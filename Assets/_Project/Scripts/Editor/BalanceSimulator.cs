using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// 밸런스 시뮬레이터: 화면 없이 한 판 전체(난이도의 1층~마지막 층, 목숨 3)를 봇으로 여러 번 돌리고 통계를 낸다.
// 실제 에셋(ShopConfig, SynergyConfig, RunConfig, DifficultyData, TypeChart, 트레이너, 아이템, 적 팀)을 그대로 쓰고,
// 전투는 BattleSetupFactory + BattleSimulation을 그대로 부른다.
// [게임과 다른 점] 튜토리얼 라운드는 없음, 해금은 둘 중 하나(모두 열림 / 처음 계정처럼 MetaConfig의 잠금 그대로),
// 전투 시간 제한 MaxBattleSeconds (게임에는 제한이 없음, 넘으면 패배로 셈), 전투 프레임 1/60초 고정
public class BalanceSimulator
{
    private const string DataFolder = "Assets/_Project/Data";
    public const float BattleStep = 1f / 60f;
    public const float MaxBattleSeconds = 180f;
    // 1:1 대결: 한 쌍(A, B)마다 A가 내 팀일 때 / 적 팀일 때 각각 이만큼
    private const int DuelRepeats = 10;
    // 등급 가치 시험 횟수
    private const int TierValueTrials = 400;

    public class CreatureStat
    {
        public int RunsBought;
        public int Offered;
        public int Bought;
        public int TeamBattles;
        public int TeamWins;
    }

    public class BotStats
    {
        public BalanceBotKind Bot;
        public int Games;
        public int Clears;
        public long ReachedSum;
        public int Timeouts;
        // 마지막 팀에서 시너지 1단계 이상 / 2단계가 켜진 판 수
        public int SynergyTier1Games;
        public int SynergyTier2Games;
        // 마지막 팀에서 조합 시너지가 하나라도 켜진 판 수
        public int MixedActiveGames;
        // 조합을 한 번이라도 한 판 수 / 만든 조합 수 (조합 해금일 때)
        public int FusionGames;
        public int FusionsMade;
        // 히든 조합을 한 번이라도 한 판 수 / 만든 히든 수 / 그 판들의 도달 층 합 (히든 해금일 때)
        public int HiddenGames;
        public int HiddenMade;
        public long HiddenReachedSum;
        // [히든 조합식] = {만든 판 수, 그 판들의 도달 층 합}
        public readonly Dictionary<FusionRecipe, long[]> HiddenRecipes = new Dictionary<FusionRecipe, long[]>();
        // [조합 시너지] = {판 수, 클리어}. 시너지 봇이 조합 시너지를 노린 판
        public int[,] MixedGoals = new int[0, 2];
        // 앞쪽 몇 판의 라운드별 기록 (봇이 무엇을 사서 어떻게 졌는지 사람이 읽어 보는 용도)
        public readonly StringBuilder SampleLog = new StringBuilder();
        public int[] RoundBattles;
        public int[] RoundWins;
        public readonly Dictionary<CreatureData, CreatureStat> Creatures = new Dictionary<CreatureData, CreatureStat>();
        public readonly Dictionary<TrainerData, int[]> Trainers = new Dictionary<TrainerData, int[]>();
        // [타입] = {판 수, 클리어}. 시너지 봇은 고른 타입, 나머지 봇은 마지막 팀에서 가장 많은 타입
        public readonly int[,] Types = new int[TypeSynergy.TypeCount, 2];

        public float ClearRate => Games > 0 ? (float)Clears / Games : 0f;
        public float AverageReached => Games > 0 ? (float)ReachedSum / Games : 0f;
        public float RoundWinRate(int round) => RoundBattles[round] > 0 ? (float)RoundWins[round] / RoundBattles[round] : 0f;
    }

    public struct DuelResult
    {
        public CreatureData A;
        public CreatureData B;
        public float WinRate;
        public float NeutralWinRate;
        // 우위 점수 (상성 제외): 이기면 0.5 + 0.5 × 남은 체력 비율, 지면 0.5 - 0.5 × 상대 남은 체력 비율. 0.5 = 대등
        public float NeutralScore;
    }

    public struct TierValueResult
    {
        public string Name;
        public int Gold;
        public float WinRate;
    }

    private readonly ShopConfig _shop;
    private readonly SynergyConfig _synergy;
    private readonly RunConfig _runConfig;
    private readonly DifficultyData _difficulty;
    private readonly TrainerData[] _trainers;
    private readonly System.Func<CreatureData, bool> _canAppear;
    // 조합 해금 계정 (보통 클리어 후)
    private readonly bool _fusionUnlocked;
    // 히든 조합 해금 계정 (어려움 클리어 후, 조합 해금일 때만 의미 있음)
    private readonly bool _hiddenUnlocked;
    private readonly CreatureData[] _creatures;
    private readonly float[,] _typeMultipliers;
    private readonly TypeChart _typeChart;
    private readonly float[,] _neutralMultipliers;

    private readonly List<BattleSetupFactory.Member> _players = new List<BattleSetupFactory.Member>();
    private readonly List<BattleSetupFactory.Member> _enemies = new List<BattleSetupFactory.Member>();
    private readonly int[] _typeCounts = new int[TypeSynergy.TypeCount];
    // 기록할 판이면 null이 아니다
    private StringBuilder _gameLog;

    public TrainerData[] Trainers => _trainers;

    // lockedLikeNewAccount: true면 MetaConfig에서 잠긴 트레이너·크리처를 빼고 돌린다 (처음 시작한 계정)
    // difficulty: Data/Difficulty 안 에셋 이름 (Normal, Hard, Impossible)
    // fusionUnlocked: 봇이 조합을 쓴다 (보통 클리어 후 계정)
    // hiddenUnlocked: 랜덤 외 봇이 히든 조합도 쓴다 (어려움 클리어 후 계정)
    public BalanceSimulator(bool lockedLikeNewAccount = false, string difficulty = "Normal", bool fusionUnlocked = false,
        bool hiddenUnlocked = false)
    {
        _fusionUnlocked = fusionUnlocked;
        _hiddenUnlocked = fusionUnlocked && hiddenUnlocked;
        _shop = Load<ShopConfig>("ShopConfig");
        _synergy = Load<SynergyConfig>("SynergyConfig");
        _runConfig = Load<RunConfig>("RunConfig");
        _difficulty = Load<DifficultyData>("Difficulty/" + difficulty);
        _typeChart = Load<TypeChart>("TypeChart");
        _typeMultipliers = BattleSetupFactory.CreateTypeMultipliers(_typeChart);
        _trainers = LoadAll<TrainerData>(DataFolder + "/Trainers");
        _canAppear = _ => true;
        if (lockedLikeNewAccount)
        {
            var meta = Load<MetaConfig>("MetaConfig");
            _trainers = _trainers.Where(trainer => meta.GetCost(trainer) == 0).ToArray();
            _canAppear = creature => meta.GetCost(creature) == 0;
        }
        _creatures = LoadAll<CreatureData>(DataFolder + "/Creatures")
            .OrderBy(c => c.Tier).ThenBy(c => c.Type).ThenBy(c => c.name).ToArray();

        BalanceBots.SynergyThresholds = new int[_synergy.TierCount];
        for (int tier = 1; tier <= _synergy.TierCount; tier++)
        {
            BalanceBots.SynergyThresholds[tier - 1] = _synergy.GetThreshold(tier);
        }

        int typeCount = TypeSynergy.TypeCount;
        _neutralMultipliers = new float[typeCount, typeCount];
        for (int a = 0; a < typeCount; a++)
        {
            for (int d = 0; d < typeCount; d++)
            {
                _neutralMultipliers[a, d] = 1f;
            }
        }
    }

    private static T Load<T>(string name) where T : Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>($"{DataFolder}/{name}.asset");
        if (asset == null)
        {
            throw new System.InvalidOperationException($"에셋을 찾을 수 없습니다: {DataFolder}/{name}.asset");
        }
        return asset;
    }

    private static T[] LoadAll<T>(string folder) where T : Object
    {
        return AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.name)
            .ToArray();
    }

    // ---------- 한 판 ----------

    // games판을 돌린다. 판 i: 시드 seed+i, 트레이너 i % 트레이너 수,
    // 시너지 봇 목표 (i / 트레이너 수) % (5 + 조합 시너지 수): 0~4는 그 타입, 그 뒤는 조합 시너지
    // progress가 true를 돌려주면 멈춘다. 앞쪽 logGames판은 라운드별 기록을 SampleLog에 남긴다
    public BotStats RunGames(BalanceBotKind bot, int games, int seed, System.Func<float, bool> progress = null,
        int logGames = 0)
    {
        var stats = new BotStats
        {
            Bot = bot,
            RoundBattles = new int[_difficulty.FloorCount + 1],
            RoundWins = new int[_difficulty.FloorCount + 1],
        };
        MixedSynergyConfig mixed = _synergy.Mixed;
        int mixedCount = mixed != null ? mixed.Count : 0;
        stats.MixedGoals = new int[mixedCount, 2];
        foreach (CreatureData creature in _creatures)
        {
            stats.Creatures[creature] = new CreatureStat();
        }
        foreach (TrainerData trainer in _trainers)
        {
            stats.Trainers[trainer] = new int[2];
        }

        Random.State savedState = Random.state;
        try
        {
            for (int i = 0; i < games; i++)
            {
                if (progress != null && i % 20 == 0 && progress((float)i / games))
                {
                    break;
                }
                TrainerData trainer = _trainers[i % _trainers.Length];
                int goal = i / _trainers.Length % (TypeSynergy.TypeCount + mixedCount);
                var focus = (CreatureType)(goal % TypeSynergy.TypeCount);
                int mixedGoal = goal >= TypeSynergy.TypeCount ? goal - TypeSynergy.TypeCount : -1;
                _gameLog = i < logGames ? stats.SampleLog : null;
                _gameLog?.AppendLine($"== {BotName(bot)} 봇 {i + 1}번째 판 (시드 {seed + i}, 트레이너 {trainer.DisplayName}" +
                                     (bot != BalanceBotKind.Synergy ? ")"
                                         : mixedGoal >= 0 ? $", 노린 조합 {mixed.Get(mixedGoal).Name})"
                                         : $", 고른 타입 {TypeSynergy.GetTypeName(focus)})"));
                PlayGame(bot, seed + i, trainer, focus, mixedGoal, stats);
            }
        }
        finally
        {
            Random.state = savedState;
            _gameLog = null;
        }
        return stats;
    }

    // RunManager.StartRun / OnBattleEnd / StartRound와 같은 흐름
    // [게임과 따로 구현] RunManager는 화면 전환·저장과 묶인 MonoBehaviour라서 층·목숨 규칙을 여기로 옮겨 왔다
    private void PlayGame(BalanceBotKind bot, int gameSeed, TrainerData trainer, CreatureType focus, int mixedGoal,
        BotStats stats)
    {
        // 상점 확률은 ShopConfig가 UnityEngine.Random을 쓰므로 판마다 시드를 정한다
        Random.InitState(gameSeed);
        var random = new System.Random(gameSeed);
        var run = new BalanceRun(_shop, _difficulty, _canAppear) { FusionUnlocked = _fusionUnlocked, HiddenUnlocked = _hiddenUnlocked };
        CreatureType[] mixedTypes = mixedGoal >= 0 ? _synergy.Mixed.Get(mixedGoal).Types : null;

        int round = 1;
        int lives = _difficulty.Lives;
        int pendingGold = 0;
        bool cleared = false;
        run.StartRound(round, trainer.RoundGoldBonus + pendingGold);

        while (true)
        {
            int startGold = run.Gold;
            BalanceBots.PlayShop(bot, run, focus, mixedTypes, random);
            EnsureTeam(run);

            // 팀 칸 크리처의 행운 동전만 센다
            int winGold = 0;
            foreach (CreatureInstance creature in run.Team)
            {
                if (creature?.Item != null)
                {
                    winGold += creature.Item.WinGold;
                }
            }

            _players.Clear();
            for (int i = 0; i < run.Team.Length; i++)
            {
                CreatureInstance creature = run.Team[i];
                if (creature != null)
                {
                    _players.Add(new BattleSetupFactory.Member
                        { Creature = creature.Data, Level = creature.Level, Item = creature.Item, SlotIndex = i });
                }
            }
            bool isBoss = _difficulty.IsBossFloor(round);
            EnemyTeamData enemy = EnemyTeamGenerator.GetTeam(_runConfig, _shop, _synergy.Mixed, _difficulty, round, gameSeed,
                _typeChart, run.Team);
            SetEnemyTeam(enemy);

            // 시간 초과(0.5)는 패배로 센다
            bool timeout = false;
            float margin = 0f;
            bool won = _players.Count > 0
                       && Fight(_players, _enemies, trainer, random.Next(), _typeMultipliers, out timeout, out margin,
                           _difficulty.GetEnemyStatMultiplier(round), true, _difficulty.EnemyMixedSynergy,
                           BossAbility.Combine(_difficulty.GetBossAbilities(round))) > 0.75f;
            if (_gameLog != null)
            {
                _gameLog.AppendLine($"  {round}층 골드 {startGold}→{run.Gold} | 팀 {DescribeTeam(run.Team)} | " +
                                    $"적 {enemy.DisplayName}{(isBoss ? "(보스)" : "")} | " +
                                    $"{(won ? "승" : timeout ? "시간 초과" : "패")} (이긴 쪽 남은 체력 {margin * 100f:0}%)" +
                                    $" | 목숨 {(won ? lives : lives - 1)}");
            }
            stats.Timeouts += timeout ? 1 : 0;
            stats.RoundBattles[round]++;
            stats.RoundWins[round] += won ? 1 : 0;
            foreach (CreatureData creature in _players.Select(p => p.Creature).Distinct())
            {
                if (stats.Creatures.TryGetValue(creature, out CreatureStat stat))
                {
                    stat.TeamBattles++;
                    stat.TeamWins += won ? 1 : 0;
                }
            }

            bool lastRound = round >= _difficulty.FloorCount;
            pendingGold = won ? winGold : 0;
            if (won)
            {
                if (lastRound)
                {
                    cleared = true;
                    break;
                }
                round++;
            }
            else
            {
                lives--;
                if (lives <= 0)
                {
                    break;
                }
                // 지더라도 다음 층으로. 마지막 층에서 지면 마지막 층을 다시
                if (!lastRound)
                {
                    round++;
                }
            }
            run.StartRound(round, trainer.RoundGoldBonus + pendingGold);
        }

        _gameLog?.AppendLine(cleared ? "  → 클리어" : $"  → {round}층에서 게임 오버");
        stats.Games++;
        stats.Clears += cleared ? 1 : 0;
        stats.ReachedSum += round;
        stats.FusionGames += run.FusionsMade > 0 ? 1 : 0;
        stats.FusionsMade += run.FusionsMade;
        stats.HiddenGames += run.HiddenMade > 0 ? 1 : 0;
        stats.HiddenMade += run.HiddenMade;
        stats.HiddenReachedSum += run.HiddenMade > 0 ? round : 0;
        foreach (FusionRecipe recipe in run.HiddenRecipesMade)
        {
            if (!stats.HiddenRecipes.TryGetValue(recipe, out long[] made))
            {
                stats.HiddenRecipes[recipe] = made = new long[2];
            }
            made[0]++;
            made[1] += round;
        }
        stats.Trainers[trainer][0]++;
        stats.Trainers[trainer][1] += cleared ? 1 : 0;

        int dominant = DominantType(run);
        int type = bot == BalanceBotKind.Synergy ? (int)focus : dominant;
        int bestTier = _synergy.GetTier(_typeCounts.Max());
        stats.SynergyTier1Games += bestTier >= 1 ? 1 : 0;
        stats.SynergyTier2Games += bestTier >= 2 ? 1 : 0;
        if (bot == BalanceBotKind.Synergy && mixedGoal >= 0)
        {
            stats.MixedGoals[mixedGoal, 0]++;
            stats.MixedGoals[mixedGoal, 1] += cleared ? 1 : 0;
        }
        else
        {
            stats.Types[type, 0]++;
            stats.Types[type, 1] += cleared ? 1 : 0;
        }
        MixedSynergyConfig mixed = _synergy.Mixed;
        for (int i = 0; mixed != null && i < mixed.Count; i++)
        {
            if (mixed.IsActive(i, _typeCounts))
            {
                stats.MixedActiveGames++;
                break;
            }
        }

        foreach (KeyValuePair<CreatureData, int> pair in run.Bought)
        {
            if (stats.Creatures.TryGetValue(pair.Key, out CreatureStat stat))
            {
                stat.RunsBought++;
                stat.Bought += pair.Value;
            }
        }
        foreach (KeyValuePair<CreatureData, int> pair in run.Offered)
        {
            if (stats.Creatures.TryGetValue(pair.Key, out CreatureStat stat))
            {
                stat.Offered += pair.Value;
            }
        }
    }

    // 상점의 "전투 시작"은 팀 칸이 비어 있으면 안 되므로 대기석 첫 크리처를 팀으로
    private static void EnsureTeam(BalanceRun run)
    {
        if (System.Array.Exists(run.Team, c => c != null))
        {
            return;
        }
        int bench = System.Array.FindIndex(run.Bench, c => c != null);
        if (bench >= 0)
        {
            run.Team[0] = run.Bench[bench];
            run.Bench[bench] = null;
        }
    }

    // 예: "앞[용암갈기 Lv2(날카로운 발톱), 돌멩군 Lv1] 뒤[...]"
    private static string DescribeTeam(CreatureInstance[] team)
    {
        string Slot(int i) => team[i] == null ? "-"
            : $"{team[i].Data.DisplayName} Lv{team[i].Level}{(team[i].Item != null ? $"({team[i].Item.DisplayName})" : "")}";
        var front = Enumerable.Range(0, BattleSimulation.FrontRowCount).Select(Slot);
        var back = Enumerable.Range(BattleSimulation.FrontRowCount, team.Length - BattleSimulation.FrontRowCount).Select(Slot);
        return $"앞[{string.Join(", ", front)}] 뒤[{string.Join(", ", back)}]";
    }

    private int DominantType(BalanceRun run)
    {
        TypeSynergy.CountUnique(run.Team.Select(c => c?.Data), _typeCounts);
        int best = 0;
        for (int i = 1; i < _typeCounts.Length; i++)
        {
            if (_typeCounts[i] > _typeCounts[best])
            {
                best = i;
            }
        }
        return best;
    }

    private void SetEnemyTeam(EnemyTeamData team)
    {
        _enemies.Clear();
        for (int i = 0; i < team.SlotCount; i++)
        {
            EnemyTeamData.Member member = team.GetMember(i);
            if (member.Creature != null)
            {
                _enemies.Add(new BattleSetupFactory.Member
                    { Creature = member.Creature, Level = Mathf.Max(1, member.Level), Item = member.Item, SlotIndex = i });
            }
        }
    }

    // BattleScreen.StartBattle과 같은 준비 (시너지는 양쪽, 트레이너·아이템은 내 팀만) 후 끝까지 돌린다.
    // 내 팀 승리 1, 패배 0, 시간 초과 0.5
    private float Fight(List<BattleSetupFactory.Member> players, List<BattleSetupFactory.Member> enemies,
        TrainerData trainer, int seed, float[,] multipliers, out bool timeout)
    {
        return Fight(players, enemies, trainer, seed, multipliers, out timeout, out _);
    }

    // margin: 이긴 쪽 남은 체력 비율 (0~1, 시간 초과면 0). enemyMultiplier: 층마다 적 능력치 증가.
    // playerMixed/enemyMixed: 조합 시너지 적용 (1:1 대결·등급 가치는 둘 다 없음). boss: 보스 특수 능력 (불가능 보스 층)
    private float Fight(List<BattleSetupFactory.Member> players, List<BattleSetupFactory.Member> enemies,
        TrainerData trainer, int seed, float[,] multipliers, out bool timeout, out float margin, float enemyMultiplier = 1f,
        bool playerMixed = false, bool enemyMixed = false, BossEffects boss = default)
    {
        List<BattleUnitSetup> playerSetups = BattleSetupFactory.CreateTeam(players, _synergy, trainer, playerMixed);
        List<BattleUnitSetup> enemySetups = BattleSetupFactory.CreateTeam(enemies, _synergy, null, enemyMixed);
        BattleSetupFactory.ScaleStats(enemySetups, enemyMultiplier);
        var simulation = new BattleSimulation(playerSetups, enemySetups, multipliers, seed, boss);
        simulation.Start();
        while (!simulation.IsOver && simulation.ElapsedTime < MaxBattleSeconds)
        {
            simulation.Tick(BattleStep);
        }
        timeout = !simulation.IsOver;
        margin = timeout ? 0f : HealthRatio(simulation.PlayerWon ? simulation.Players : simulation.Enemies);
        return timeout ? 0.5f : simulation.PlayerWon ? 1f : 0f;
    }

    private static float HealthRatio(IReadOnlyList<BattleSimUnit> units)
    {
        float health = 0f;
        float max = 0f;
        foreach (BattleSimUnit unit in units)
        {
            health += System.Math.Max(0, unit.Health);
            max += unit.MaxHealth;
        }
        return max > 0f ? health / max : 0f;
    }

    // ---------- 1:1 대결표 (같은 등급끼리, Lv1, 시너지 규칙 그대로) ----------

    public List<DuelResult> RunDuels(int seed)
    {
        var results = new List<DuelResult>();
        foreach (IGrouping<int, CreatureData> tier in _creatures.GroupBy(c => c.Tier))
        {
            CreatureData[] group = tier.ToArray();
            foreach (CreatureData a in group)
            {
                foreach (CreatureData b in group)
                {
                    if (a == b)
                    {
                        continue;
                    }
                    results.Add(new DuelResult
                    {
                        A = a,
                        B = b,
                        WinRate = Duel(a, b, seed, _typeMultipliers, out _),
                        NeutralWinRate = Duel(a, b, seed, _neutralMultipliers, out float score),
                        NeutralScore = score,
                    });
                }
            }
        }
        return results;
    }

    // A가 내 팀일 때와 적 팀일 때를 반반 (먼저 계산되는 쪽이 유리한 것을 없앤다)
    private float Duel(CreatureData a, CreatureData b, int seed, float[,] multipliers, out float marginScore)
    {
        float wins = 0f;
        marginScore = 0f;
        for (int i = 0; i < DuelRepeats; i++)
        {
            SetSingle(_players, a, 1);
            SetSingle(_enemies, b, 1);
            float result = Fight(_players, _enemies, null, seed + i, multipliers, out _, out float margin);
            wins += result;
            marginScore += MarginScore(result, margin);
            SetSingle(_players, b, 1);
            SetSingle(_enemies, a, 1);
            result = 1f - Fight(_players, _enemies, null, seed + 1000 + i, multipliers, out _, out margin);
            wins += result;
            marginScore += MarginScore(result, margin);
        }
        marginScore /= DuelRepeats * 2;
        return wins / (DuelRepeats * 2);
    }

    // result: A 기준 1 승, 0 패, 0.5 시간 초과
    private static float MarginScore(float result, float margin)
    {
        return result > 0.75f ? 0.5f + 0.5f * margin : result < 0.25f ? 0.5f - 0.5f * margin : 0.5f;
    }

    private static void SetSingle(List<BattleSetupFactory.Member> members, CreatureData creature, int level)
    {
        members.Clear();
        members.Add(new BattleSetupFactory.Member { Creature = creature, Level = level, SlotIndex = 0 });
    }

    // ---------- 등급 가치: 같은 골드로 산 팀끼리 ----------

    // 등급 N 크리처 Lv1 1마리 대 1골드 Lv1 N마리 (서로 다른 크리처), 5골드 1마리 대 1골드 Lv2 팀
    public List<TierValueResult> RunTierValue(int seed)
    {
        var random = new System.Random(seed);
        CreatureData[] oneGold = _creatures.Where(c => c.Tier == 1).ToArray();
        var results = new List<TierValueResult>();

        for (int tier = 2; tier <= CreatureData.MaxTier; tier++)
        {
            CreatureData[] pool = _creatures.Where(c => c.Tier == tier).ToArray();
            int count = tier;
            results.Add(new TierValueResult
            {
                Name = $"{tier}골드 Lv1 1마리 vs 1골드 Lv1 {count}마리",
                Gold = tier,
                WinRate = TierTrial(random, seed, pool, 1, oneGold, Enumerable.Repeat(1, count).ToArray()),
            });
        }

        results.Add(new TierValueResult
        {
            Name = "3골드 Lv1 1마리 vs 1골드 Lv2 1마리 (같은 골드·같은 칸 수)",
            Gold = 3,
            WinRate = TierTrial(random, seed, _creatures.Where(c => c.Tier == 3).ToArray(), 1, oneGold, new[] { 2 }),
        });

        CreatureData[] fiveGold = _creatures.Where(c => c.Tier == CreatureData.MaxTier).ToArray();
        results.Add(new TierValueResult
        {
            Name = "5골드 Lv1 1마리 vs 1골드 Lv2 1마리 + Lv1 2마리",
            Gold = 5,
            WinRate = TierTrial(random, seed, fiveGold, 1, oneGold, new[] { 2, 1, 1 }),
        });
        results.Add(new TierValueResult
        {
            Name = "5골드 Lv2 1마리 vs 1골드 Lv2 5마리",
            Gold = 15,
            WinRate = TierTrial(random, seed, fiveGold, 2, oneGold, new[] { 2, 2, 2, 2, 2 }),
        });
        return results;
    }

    // 큰 크리처 1마리(앞줄 첫 칸)의 승률. 무리는 앞줄부터 칸 순서로
    private float TierTrial(System.Random random, int seed, CreatureData[] bigPool, int bigLevel,
        CreatureData[] smallPool, int[] smallLevels)
    {
        float score = 0f;
        for (int trial = 0; trial < TierValueTrials; trial++)
        {
            CreatureData big = bigPool[random.Next(bigPool.Length)];
            CreatureData[] small = smallPool.OrderBy(_ => random.Next()).Take(smallLevels.Length).ToArray();
            bool bigIsPlayer = trial % 2 == 0;
            List<BattleSetupFactory.Member> bigSide = bigIsPlayer ? _players : _enemies;
            List<BattleSetupFactory.Member> smallSide = bigIsPlayer ? _enemies : _players;

            SetSingle(bigSide, big, bigLevel);
            smallSide.Clear();
            for (int i = 0; i < small.Length; i++)
            {
                smallSide.Add(new BattleSetupFactory.Member { Creature = small[i], Level = smallLevels[i], SlotIndex = i });
            }
            float playerScore = Fight(_players, _enemies, null, seed + trial, _typeMultipliers, out _);
            score += bigIsPlayer ? playerScore : 1f - playerScore;
        }
        return score / TierValueTrials;
    }

    // ---------- 결과 정리 ----------

    public string BuildSummary(IList<BotStats> bots, IList<DuelResult> duels, IList<TierValueResult> tierValue)
    {
        var text = new StringBuilder();
        foreach (BotStats stats in bots)
        {
            text.AppendLine($"[{BotName(stats.Bot)} 봇] {stats.Games}판  클리어 {Pct(stats.ClearRate)}  " +
                            $"평균 도달 층 {stats.AverageReached:0.00}" +
                            $"  마지막 팀 시너지 1단계+ {Pct(Rate(stats.SynergyTier1Games, stats.Games))} · 2단계 {Pct(Rate(stats.SynergyTier2Games, stats.Games))}" +
                            (stats.Timeouts > 0 ? $"  시간 초과 전투 {stats.Timeouts}" : "") +
                            (_fusionUnlocked ? $"  조합한 판 {Pct(Rate(stats.FusionGames, stats.Games))} (판당 {(float)stats.FusionsMade / Mathf.Max(1, stats.Games):0.0}번)" : ""));
            if (_hiddenUnlocked)
            {
                int without = stats.Games - stats.HiddenGames;
                text.Append($"  히든 조합한 판 {Pct(Rate(stats.HiddenGames, stats.Games))} (판당 {(float)stats.HiddenMade / Mathf.Max(1, stats.Games):0.00}번)" +
                            $"  평균 층 히든 만든 판 {(stats.HiddenGames > 0 ? (float)stats.HiddenReachedSum / stats.HiddenGames : 0f):0.0}" +
                            $" / 안 만든 판 {(without > 0 ? (float)(stats.ReachedSum - stats.HiddenReachedSum) / without : 0f):0.0}");
                text.Append("  히든별(만든 판, 평균 층):");
                foreach (KeyValuePair<FusionRecipe, long[]> pair in stats.HiddenRecipes.OrderBy(p => p.Key.name))
                {
                    text.Append($" {pair.Key.Result.DisplayName} {pair.Value[0]}판 {(float)pair.Value[1] / pair.Value[0]:0.0}");
                }
                text.AppendLine();
            }
            text.Append("  층 승률:");
            for (int round = 1; round < stats.RoundBattles.Length; round++)
            {
                text.Append($" {round}:{Pct(stats.RoundWinRate(round))}");
            }
            text.AppendLine();
            text.Append("  트레이너 클리어:");
            foreach (KeyValuePair<TrainerData, int[]> pair in stats.Trainers)
            {
                text.Append($" {pair.Key.DisplayName} {Pct(Rate(pair.Value[1], pair.Value[0]))}");
            }
            text.AppendLine();
            text.Append(stats.Bot == BalanceBotKind.Synergy ? "  고른 타입 클리어:" : "  주 타입 클리어:");
            for (int type = 0; type < TypeSynergy.TypeCount; type++)
            {
                text.Append($" {TypeSynergy.GetTypeName((CreatureType)type)} {Pct(Rate(stats.Types[type, 1], stats.Types[type, 0]))}" +
                            $"({stats.Types[type, 0]})");
            }
            text.AppendLine();
            MixedSynergyConfig mixed = _synergy.Mixed;
            if (mixed != null)
            {
                text.Append($"  마지막 팀 조합 시너지 {Pct(Rate(stats.MixedActiveGames, stats.Games))}");
                if (stats.Bot == BalanceBotKind.Synergy)
                {
                    text.Append("  노린 조합 클리어:");
                    for (int i = 0; i < stats.MixedGoals.GetLength(0); i++)
                    {
                        text.Append($" {mixed.Get(i).Name} {Pct(Rate(stats.MixedGoals[i, 1], stats.MixedGoals[i, 0]))}({stats.MixedGoals[i, 0]})");
                    }
                }
                text.AppendLine();
            }
        }

        if (duels != null && duels.Count > 0)
        {
            text.AppendLine("[1:1 대결] 같은 등급 평균 — 승률(상성 포함) / 승률(상성 제외) / 우위 점수(상성 제외, 40~60 밖은 *)");
            foreach (IGrouping<int, DuelResult> tier in duels.GroupBy(d => d.A.Tier))
            {
                text.Append($"  {tier.Key}골드:");
                foreach (IGrouping<CreatureData, DuelResult> creature in tier.GroupBy(d => d.A))
                {
                    float withType = creature.Average(d => d.WinRate);
                    float neutral = creature.Average(d => d.NeutralWinRate);
                    float score = creature.Average(d => d.NeutralScore);
                    bool outside = score < 0.4f || score > 0.6f;
                    text.Append($" {creature.Key.DisplayName} {Pct(withType)}/{Pct(neutral)}/{score * 100f:0}{(outside ? "*" : "")}");
                }
                text.AppendLine();
            }
        }

        if (tierValue != null && tierValue.Count > 0)
        {
            text.AppendLine("[등급 가치] 같은 골드끼리, 앞쪽 크리처 승률");
            foreach (TierValueResult result in tierValue)
            {
                text.AppendLine($"  {result.Name}: {Pct(result.WinRate)}");
            }
        }
        return text.ToString();
    }

    public void WriteCsv(string folder, IList<BotStats> bots, IList<DuelResult> duels, IList<TierValueResult> tierValue,
        string summary)
    {
        Directory.CreateDirectory(folder);
        var encoding = new UTF8Encoding(true);

        if (bots.Count > 0)
        {
            int roundCount = bots[0].RoundBattles.Length - 1;
            var csv = new StringBuilder("봇,판 수,클리어율,평균 도달 층,시간 초과 전투,시너지 1단계+ 판 비율,시너지 2단계 판 비율");
            for (int round = 1; round <= roundCount; round++)
            {
                csv.Append($",{round}층 승률");
            }
            csv.AppendLine();
            foreach (BotStats stats in bots)
            {
                csv.Append($"{BotName(stats.Bot)},{stats.Games},{F(stats.ClearRate)},{stats.AverageReached:0.00},{stats.Timeouts},{F(Rate(stats.SynergyTier1Games, stats.Games))},{F(Rate(stats.SynergyTier2Games, stats.Games))}");
                for (int round = 1; round <= roundCount; round++)
                {
                    csv.Append($",{F(stats.RoundWinRate(round))}");
                }
                csv.AppendLine();
            }
            File.WriteAllText(Path.Combine(folder, "bots.csv"), csv.ToString(), encoding);

            csv = new StringBuilder("봇,층,전투 수,승리,승률\n");
            foreach (BotStats stats in bots)
            {
                for (int round = 1; round <= roundCount; round++)
                {
                    csv.AppendLine($"{BotName(stats.Bot)},{round},{stats.RoundBattles[round]},{stats.RoundWins[round]},{F(stats.RoundWinRate(round))}");
                }
            }
            File.WriteAllText(Path.Combine(folder, "rounds.csv"), csv.ToString(), encoding);

            csv = new StringBuilder("봇,크리처,이름,등급,타입,산 판 비율,상점에 나온 수,산 수,나온 것 중 산 비율,팀에 있던 전투 수,그때 승률\n");
            foreach (BotStats stats in bots)
            {
                foreach (KeyValuePair<CreatureData, CreatureStat> pair in stats.Creatures)
                {
                    CreatureData c = pair.Key;
                    CreatureStat s = pair.Value;
                    csv.AppendLine($"{BotName(stats.Bot)},{c.name},{c.DisplayName},{c.Tier},{TypeSynergy.GetTypeName(c.Type)}," +
                                   $"{F(Rate(s.RunsBought, stats.Games))},{s.Offered},{s.Bought},{F(Rate(s.Bought, s.Offered))}," +
                                   $"{s.TeamBattles},{F(Rate(s.TeamWins, s.TeamBattles))}");
                }
            }
            File.WriteAllText(Path.Combine(folder, "creatures.csv"), csv.ToString(), encoding);

            csv = new StringBuilder("봇,기준,타입,판 수,클리어율\n");
            foreach (BotStats stats in bots)
            {
                string basis = stats.Bot == BalanceBotKind.Synergy ? "고른 타입" : "마지막 팀 주 타입";
                for (int type = 0; type < TypeSynergy.TypeCount; type++)
                {
                    csv.AppendLine($"{BotName(stats.Bot)},{basis},{TypeSynergy.GetTypeName((CreatureType)type)}," +
                                   $"{stats.Types[type, 0]},{F(Rate(stats.Types[type, 1], stats.Types[type, 0]))}");
                }
            }
            File.WriteAllText(Path.Combine(folder, "types.csv"), csv.ToString(), encoding);

            csv = new StringBuilder("봇,트레이너,판 수,클리어율\n");
            foreach (BotStats stats in bots)
            {
                foreach (KeyValuePair<TrainerData, int[]> pair in stats.Trainers)
                {
                    csv.AppendLine($"{BotName(stats.Bot)},{pair.Key.DisplayName},{pair.Value[0]},{F(Rate(pair.Value[1], pair.Value[0]))}");
                }
            }
            File.WriteAllText(Path.Combine(folder, "trainers.csv"), csv.ToString(), encoding);
        }

        if (duels != null && duels.Count > 0)
        {
            var csv = new StringBuilder("등급,A,A 이름,A 타입,B,B 이름,B 타입,A 승률(상성 포함),A 승률(상성 제외),A 우위 점수(상성 제외)\n");
            foreach (DuelResult d in duels)
            {
                csv.AppendLine($"{d.A.Tier},{d.A.name},{d.A.DisplayName},{TypeSynergy.GetTypeName(d.A.Type)}," +
                               $"{d.B.name},{d.B.DisplayName},{TypeSynergy.GetTypeName(d.B.Type)},{F(d.WinRate)},{F(d.NeutralWinRate)},{F(d.NeutralScore)}");
            }
            File.WriteAllText(Path.Combine(folder, "duels.csv"), csv.ToString(), encoding);

            csv = new StringBuilder("등급,크리처,이름,타입,평균 승률(상성 포함),평균 승률(상성 제외),평균 우위 점수(상성 제외)\n");
            foreach (IGrouping<CreatureData, DuelResult> group in duels.GroupBy(d => d.A))
            {
                CreatureData c = group.Key;
                csv.AppendLine($"{c.Tier},{c.name},{c.DisplayName},{TypeSynergy.GetTypeName(c.Type)}," +
                               $"{F(group.Average(d => d.WinRate))},{F(group.Average(d => d.NeutralWinRate))},{F(group.Average(d => d.NeutralScore))}");
            }
            File.WriteAllText(Path.Combine(folder, "duel_summary.csv"), csv.ToString(), encoding);
        }

        if (tierValue != null && tierValue.Count > 0)
        {
            var csv = new StringBuilder("시험,골드,앞쪽 승률\n");
            foreach (TierValueResult result in tierValue)
            {
                csv.AppendLine($"{result.Name},{result.Gold},{F(result.WinRate)}");
            }
            File.WriteAllText(Path.Combine(folder, "tier_value.csv"), csv.ToString(), encoding);
        }

        File.WriteAllText(Path.Combine(folder, "summary.txt"), summary, encoding);
        string samples = string.Concat(bots.Select(stats => stats.SampleLog.ToString()));
        if (samples.Length > 0)
        {
            File.WriteAllText(Path.Combine(folder, "sample_games.txt"), samples, encoding);
        }
    }

    public static string BotName(BalanceBotKind bot)
    {
        switch (bot)
        {
            case BalanceBotKind.Random: return "랜덤";
            case BalanceBotKind.Basic: return "기본";
            case BalanceBotKind.Synergy: return "시너지";
            case BalanceBotKind.HighTier: return "고등급";
            default: return "저축";
        }
    }

    private static float Rate(int part, int total) => total > 0 ? (float)part / total : 0f;
    private static string Pct(float rate) => $"{rate * 100f:0.0}%";
    private static string F(float rate) => rate.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
}
