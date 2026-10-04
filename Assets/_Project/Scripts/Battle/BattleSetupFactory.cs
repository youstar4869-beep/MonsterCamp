using System.Collections.Generic;

// 게임 데이터(CreatureData, 시너지, 트레이너, 아이템, 상성표)를 BattleSimulation 입력으로 바꾼다
public static class BattleSetupFactory
{
    // 팀 칸 하나
    public struct Member
    {
        public CreatureData Creature;
        public int Level;
        // 내 팀만 (적은 null)
        public ItemData Item;
        public int SlotIndex;
    }

    private static readonly List<CreatureData> TeamCreatures = new List<CreatureData>();
    private static readonly int[] TypeCounts = new int[TypeSynergy.TypeCount];

    // 크리처 1마리 (보너스 없이)
    public static BattleUnitSetup Create(CreatureData creature, int level, int slotIndex)
    {
        return new BattleUnitSetup
        {
            Name = creature.DisplayName,
            Type = creature.Type,
            Tier = creature.Tier,
            SlotIndex = slotIndex,
            MaxHealth = creature.GetMaxHealth(level),
            Attack = creature.GetAttack(level),
            AttackSpeed = creature.AttackSpeed,
            Skill = creature.SkillType,
            SkillEvery = creature.SkillEvery,
            SkillChance = creature.SkillChance,
            SkillValue1 = creature.SkillValue1,
            SkillValue2 = creature.SkillValue2,
            Skill2 = creature.Skill2Type,
            Skill2Every = creature.Skill2Every,
            Skill2Chance = creature.Skill2Chance,
            Skill2Value1 = creature.Skill2Value1,
            Skill2Value2 = creature.Skill2Value2,
        };
    }

    // 팀 전체: 타입 시너지(팀 안 마릿수) + 조합 시너지 + 트레이너 능력 + 아이템을 더한 보너스를 붙인다.
    // members는 칸 순서여야 한다 (공격 대상 순서). mixedSynergy가 false면 조합 시너지 없음 (보통 난이도 적 팀)
    public static List<BattleUnitSetup> CreateTeam(IReadOnlyList<Member> members, SynergyConfig synergy, TrainerData trainer,
        bool mixedSynergy)
    {
        MixedSynergyConfig mixed = mixedSynergy ? synergy.Mixed : null;
        TeamCreatures.Clear();
        foreach (Member member in members)
        {
            TeamCreatures.Add(member.Creature);
        }
        TypeSynergy.CountUnique(TeamCreatures, TypeCounts);

        var setups = new List<BattleUnitSetup>(members.Count);
        foreach (Member member in members)
        {
            CreatureType type = member.Creature.Type;
            BattleBonus bonus = synergy.GetBonus(type, synergy.GetTier(TypeCounts[(int)type]));
            if (member.Creature.HasSecondType && member.Creature.SecondType != type)
            {
                CreatureType second = member.Creature.SecondType;
                synergy.AddBonus(second, synergy.GetTier(TypeCounts[(int)second]), ref bonus);
            }
            if (mixed != null)
            {
                mixed.AddBonus(TypeCounts, ref bonus);
            }
            if (trainer != null)
            {
                trainer.AddBattleBonus(type, ref bonus);
            }
            if (member.Item != null)
            {
                member.Item.AddBattleBonus(ref bonus);
            }

            BattleUnitSetup setup = Create(member.Creature, member.Level, member.SlotIndex);
            setup.Bonus = bonus;
            setups.Add(setup);
        }
        return setups;
    }

    // 층마다 적 능력치 증가: 체력·공격력에 배율을 곱한다 (1이면 그대로)
    public static void ScaleStats(List<BattleUnitSetup> team, float multiplier)
    {
        if (multiplier == 1f)
        {
            return;
        }
        for (int i = 0; i < team.Count; i++)
        {
            BattleUnitSetup setup = team[i];
            setup.MaxHealth = (int)System.Math.Round(setup.MaxHealth * multiplier);
            setup.Attack = (int)System.Math.Round(setup.Attack * multiplier);
            team[i] = setup;
        }
    }

    // [공격 타입, 방어 타입] 상성 배율표
    public static float[,] CreateTypeMultipliers(TypeChart chart)
    {
        int count = TypeSynergy.TypeCount;
        var multipliers = new float[count, count];
        for (int attacker = 0; attacker < count; attacker++)
        {
            for (int defender = 0; defender < count; defender++)
            {
                multipliers[attacker, defender] = chart.GetMultiplier((CreatureType)attacker, (CreatureType)defender);
            }
        }
        return multipliers;
    }
}
