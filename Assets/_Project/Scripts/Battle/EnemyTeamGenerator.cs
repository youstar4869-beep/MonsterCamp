using System.Collections.Generic;
using UnityEngine;

// 층의 적 팀: 1~10층은 RunConfig에 정해 둔 팀, 11층부터는 자동 생성.
// 같은 런 시드 + 같은 층이면 항상 같은 팀 (이어하기 해도 같음). 수치는 RunConfig·DifficultyData·ShopConfig 에셋
public static class EnemyTeamGenerator
{
    // 자동 생성 팀은 인스턴스 하나를 다시 쓴다 (전투 시작 때 바로 읽고 끝)
    private static EnemyTeamData _generated;
    private static readonly List<CreatureData> Picked = new List<CreatureData>();
    private static readonly List<CreatureData> Candidates = new List<CreatureData>();
    private static readonly List<int> Unequipped = new List<int>();
    private static readonly List<ItemData> BattleItems = new List<ItemData>();
    private static readonly int[] PlayerTypeCounts = new int[TypeSynergy.TypeCount];

    // mixed: 조합 시너지 설정 (어려움·불가능 팀이 노림, null이면 안 노림).
    // chart·playerTeam: 카운터 팀이 켜진 층에서 내 팀 주 타입에 유리한 타입을 고를 때 (빈 칸은 null)
    public static EnemyTeamData GetTeam(RunConfig config, ShopConfig shop, MixedSynergyConfig mixed, DifficultyData difficulty,
        int floor, int runSeed, TypeChart chart, IReadOnlyList<CreatureInstance> playerTeam)
    {
        if (floor <= config.FixedTeamCount)
        {
            return config.GetFixedTeam(floor);
        }
        if (_generated == null)
        {
            _generated = ScriptableObject.CreateInstance<EnemyTeamData>();
            _generated.hideFlags = HideFlags.DontSave;
        }
        Generate(_generated, config, shop, mixed, difficulty, floor, runSeed, chart, playerTeam);
        return _generated;
    }

    private static void Generate(EnemyTeamData team, RunConfig config, ShopConfig shop, MixedSynergyConfig mixed,
        DifficultyData difficulty, int floor, int runSeed, TypeChart chart, IReadOnlyList<CreatureInstance> playerTeam)
    {
        var random = new System.Random(unchecked(runSeed * 486187739 + floor));
        bool isBoss = difficulty.IsBossFloor(floor);
        int[] tierPercents = isBoss
            ? config.BossTierPercents
            : shop.GetTierPercents(floor);

        // 주 타입 1~2개: 타입 시너지(2·4마리)가 켜지게 마릿수를 맞춘다. 남는 칸은 아무 타입
        int typeCount = TypeSynergy.TypeCount;
        var first = (CreatureType)random.Next(typeCount);
        bool twoTypes = random.NextDouble() < config.TwoTypeChance;
        var second = (CreatureType)(((int)first + 1 + random.Next(typeCount - 1)) % typeCount);
        int firstCount = twoTypes ? config.FirstTypeCount : config.SingleTypeCount;
        int secondCount = twoTypes ? config.SecondTypeCount : 0;
        string name = TypeSynergy.GetTypeName(first) + (twoTypes ? "·" + TypeSynergy.GetTypeName(second) : "") + " 무리";

        // 조합 시너지를 노리는 팀 (어려움·불가능): 그 조합 타입을 1마리씩, 남는 칸은 첫 타입 1마리 더(타입 시너지 1단계) 뒤 아무 타입
        int mixedIndex = -1;
        if (difficulty.EnemyMixedSynergy && mixed != null && mixed.Count > 0 && random.NextDouble() < config.MixedTeamChance)
        {
            mixedIndex = random.Next(mixed.Count);
            name = mixed.Get(mixedIndex).Name + " 무리";
        }

        // 카운터 팀 (어려움·불가능): 뒤쪽 몇 칸을 내 팀 주 타입에 유리한 타입으로. 난수를 쓰지 않으므로 꺼진 층은 그대로
        int counterCount = difficulty.GetCounterCount(floor);
        CreatureType? counter = counterCount > 0 ? FindCounterType(chart, playerTeam) : null;
        float topTierChance = difficulty.GetEnemyTopTierChance(floor);

        // 시너지는 서로 다른 크리처만 세므로 같은 크리처는 다시 고르지 않는다
        Picked.Clear();
        for (int i = 0; i < config.TeamSize; i++)
        {
            CreatureType type;
            if (mixedIndex >= 0)
            {
                CreatureType[] types = mixed.Get(mixedIndex).Types;
                type = i < types.Length ? types[i]
                    : i == types.Length ? types[0]
                    : (CreatureType)random.Next(typeCount);
            }
            else
            {
                type = i < firstCount ? first
                    : i < firstCount + secondCount ? second
                    : (CreatureType)random.Next(typeCount);
            }
            if (counter.HasValue && i >= config.TeamSize - counterCount)
            {
                type = counter.Value;
            }
            int tier = RollTier(tierPercents, random);
            // 5골드 확률 (어려움·불가능). 0이면 난수를 쓰지 않는다
            if (topTierChance > 0f && random.NextDouble() < topTierChance)
            {
                tier = CreatureData.MaxTier;
            }
            CreatureData creature = PickCreature(shop.CreaturePool, type, tier, random);
            if (creature != null)
            {
                Picked.Add(creature);
            }
        }

        var members = new EnemyTeamData.Member[Picked.Count];
        for (int i = 0; i < members.Length; i++)
        {
            int level = RollLevel(difficulty, floor, random) + (isBoss ? difficulty.BossLevelBonus : 0);
            members[i] = new EnemyTeamData.Member
                { Creature = Picked[i], Level = Mathf.Clamp(level, 1, CreatureData.MaxLevel) };
        }

        // 조합 크리처 (어려움·불가능, 층이 오를수록 많이): 한 마리씩 확률로 같은 타입 조합 크리처로 바꾼다.
        // 시작 층 전에는 난수를 쓰지 않으므로 그 전 층·보통의 적 팀은 조합이 없던 때와 같다
        float fusionChance = difficulty.GetEnemyFusionChance(floor);
        for (int i = 0; fusionChance > 0f && i < members.Length; i++)
        {
            if (random.NextDouble() >= fusionChance)
            {
                continue;
            }
            CreatureData fused = PickFusion(shop.FusionRecipes, members[i].Creature, random);
            if (fused != null)
            {
                members[i].Creature = fused;
                Picked.Add(fused);
            }
        }
        // 앞줄(0~1)부터 체력 높은 순
        System.Array.Sort(members, (a, b) => b.Creature.GetMaxHealth(b.Level).CompareTo(a.Creature.GetMaxHealth(a.Level)));
        EquipItems(members, shop.ItemPool, difficulty.GetEnemyItemCount(floor), random);

        team.SetGenerated(name, isBoss, members);
    }

    // 내 팀에서 가장 많은 타입(같으면 타입 순서 앞)에 유리한 타입. 팀이 비었거나 유리한 타입이 없으면 null
    private static CreatureType? FindCounterType(TypeChart chart, IReadOnlyList<CreatureInstance> playerTeam)
    {
        if (chart == null || playerTeam == null)
        {
            return null;
        }
        System.Array.Clear(PlayerTypeCounts, 0, PlayerTypeCounts.Length);
        int dominant = -1;
        foreach (CreatureInstance creature in playerTeam)
        {
            if (creature?.Data == null)
            {
                continue;
            }
            int type = (int)creature.Data.Type;
            PlayerTypeCounts[type]++;
            if (dominant < 0 || PlayerTypeCounts[type] > PlayerTypeCounts[dominant]
                || (PlayerTypeCounts[type] == PlayerTypeCounts[dominant] && type < dominant))
            {
                dominant = type;
            }
        }
        if (dominant < 0)
        {
            return null;
        }
        for (int t = 0; t < TypeSynergy.TypeCount; t++)
        {
            if (chart.GetMultiplier((CreatureType)t, (CreatureType)dominant) > 1f)
            {
                return (CreatureType)t;
            }
        }
        return null;
    }

    // 아이템 count개를 아직 없는 적에게 하나씩 (행운 동전처럼 전투 효과가 없는 아이템은 빼고)
    private static void EquipItems(EnemyTeamData.Member[] members, IReadOnlyList<ItemData> pool, int count,
        System.Random random)
    {
        if (count <= 0 || pool == null)
        {
            return;
        }
        BattleItems.Clear();
        foreach (ItemData item in pool)
        {
            if (item != null && item.Effect != ItemEffect.WinGold)
            {
                BattleItems.Add(item);
            }
        }
        Unequipped.Clear();
        for (int i = 0; i < members.Length; i++)
        {
            Unequipped.Add(i);
        }
        for (int n = 0; n < count && Unequipped.Count > 0 && BattleItems.Count > 0; n++)
        {
            ItemData item = BattleItems[random.Next(BattleItems.Count)];
            int pick = random.Next(Unequipped.Count);
            members[Unequipped[pick]].Item = item;
            Unequipped.RemoveAt(pick);
        }
    }

    // 확률(%)로 1~5골드 하나. 표가 없으면 1
    private static int RollTier(int[] percents, System.Random random)
    {
        if (percents == null)
        {
            return 1;
        }
        int total = 0;
        foreach (int percent in percents)
        {
            total += Mathf.Max(0, percent);
        }
        if (total <= 0)
        {
            return 1;
        }
        int roll = random.Next(total);
        for (int i = 0; i < percents.Length; i++)
        {
            roll -= Mathf.Max(0, percents[i]);
            if (roll < 0)
            {
                return i + 1;
            }
        }
        return 1;
    }

    // 그 타입·등급에서 아직 안 고른 크리처. 없으면 아래 등급, 그래도 없으면 위 등급 (같은 타입)
    private static CreatureData PickCreature(IReadOnlyList<CreatureData> pool, CreatureType type, int tier,
        System.Random random)
    {
        for (int step = 0; step < CreatureData.MaxTier * 2; step++)
        {
            int t = step < tier ? tier - step : step + 1;
            if (t < 1 || t > CreatureData.MaxTier)
            {
                continue;
            }
            Candidates.Clear();
            foreach (CreatureData creature in pool)
            {
                if (creature != null && creature.Type == type && creature.Tier == t && !Picked.Contains(creature))
                {
                    Candidates.Add(creature);
                }
            }
            if (Candidates.Count > 0)
            {
                return Candidates[random.Next(Candidates.Count)];
            }
        }
        return null;
    }

    // 바꿀 크리처와 같은 타입(교차는 두 타입 중 하나)인 조합 크리처. 등급은 바꿀 크리처 가격에 맞춰
    // (1골드 하급, 2골드 중급, 3골드 교차, 4·5골드 상급), 그 등급에 없으면 아래 등급. 없으면 null
    private static CreatureData PickFusion(IReadOnlyList<FusionRecipe> recipes, CreatureData original, System.Random random)
    {
        if (recipes == null)
        {
            return null;
        }
        int grade = Mathf.Min(original.Tier, (int)FusionGrade.Upper + 1) - 1;
        for (; grade >= 0; grade--)
        {
            Candidates.Clear();
            foreach (FusionRecipe recipe in recipes)
            {
                CreatureData result = recipe != null ? recipe.Result : null;
                if (result != null && (int)recipe.Grade == grade && !Picked.Contains(result)
                    && (result.Type == original.Type || (result.HasSecondType && result.SecondType == original.Type)))
                {
                    Candidates.Add(result);
                }
            }
            if (Candidates.Count > 0)
            {
                return Candidates[random.Next(Candidates.Count)];
            }
        }
        return null;
    }

    // ★★★ 확률을 먼저, 아니면 ★★ 확률
    private static int RollLevel(DifficultyData difficulty, int floor, System.Random random)
    {
        if (random.NextDouble() < difficulty.GetStar3Chance(floor))
        {
            return 3;
        }
        return random.NextDouble() < difficulty.GetStar2Chance(floor) ? 2 : 1;
    }
}
