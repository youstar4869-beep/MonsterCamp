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

    // mixed: 조합 시너지 설정 (어려움·불가능 팀이 노림, null이면 안 노림)
    public static EnemyTeamData GetTeam(RunConfig config, ShopConfig shop, MixedSynergyConfig mixed, DifficultyData difficulty,
        int floor, int runSeed)
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
        Generate(_generated, config, shop, mixed, difficulty, floor, runSeed);
        return _generated;
    }

    private static void Generate(EnemyTeamData team, RunConfig config, ShopConfig shop, MixedSynergyConfig mixed,
        DifficultyData difficulty, int floor, int runSeed)
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
        // TODO 24: 조합 크리처(어려움 25층·불가능 15층부터)는 여기서 조합식으로 고른다
        int mixedIndex = -1;
        if (difficulty.EnemyMixedSynergy && mixed != null && mixed.Count > 0 && random.NextDouble() < config.MixedTeamChance)
        {
            mixedIndex = random.Next(mixed.Count);
            name = mixed.Get(mixedIndex).Name + " 무리";
        }

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
            CreatureData creature = PickCreature(shop.CreaturePool, type, RollTier(tierPercents, random), random);
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
        // 앞줄(0~1)부터 체력 높은 순
        System.Array.Sort(members, (a, b) => b.Creature.GetMaxHealth(b.Level).CompareTo(a.Creature.GetMaxHealth(a.Level)));

        team.SetGenerated(name, isBoss, members);
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
