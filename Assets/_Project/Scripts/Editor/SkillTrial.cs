using System.Collections.Generic;

// 크리처 1마리의 스킬이 실제로 발동하는지 BattleSimulation으로 확인 (UnityEngine을 쓰지 않음).
// 시험 크리처(앞줄) + 보조 아군 1마리(뒷줄, 회복 대상) 대 기본 적 2마리(앞줄 두 칸, 옆 적 확인용)를
// 두 가지로 돌린다: 약한 적(시험 크리처가 이김: 공격·처치 스킬) / 강한 적(시험 크리처가 쓰러짐: 체력·쓰러질 때 스킬)
public static class SkillTrial
{
    public struct Result
    {
        public int WeakTriggers;
        public int StrongTriggers;
        public bool WeakWon;
        public bool StrongWon;
        public int Total => WeakTriggers + StrongTriggers;
    }

    private const float MaxSeconds = 90f;
    private const float Step = 1f / 60f;
    // 등급별 능력치 배율 (CREATURE_TIERS.md 2번)
    private static readonly float[] TierScale = { 1f, 1.5f, 2.2f, 3f, 4f };

    public static Result Run(BattleUnitSetup subject, float[,] typeMultipliers, int seed)
    {
        subject.SlotIndex = 0;
        return new Result
        {
            WeakTriggers = RunOnce(subject, typeMultipliers, seed, strongEnemies: false, out bool weakWon),
            StrongTriggers = RunOnce(subject, typeMultipliers, seed + 1, strongEnemies: true, out bool strongWon),
            WeakWon = weakWon,
            StrongWon = strongWon,
        };
    }

    private static int RunOnce(BattleUnitSetup subject, float[,] typeMultipliers, int seed, bool strongEnemies, out bool won)
    {
        float scale = TierScale[System.Math.Max(1, System.Math.Min(5, subject.Tier)) - 1];
        CreatureType neutral = NeutralType(subject.Type);

        var players = new List<BattleUnitSetup>
        {
            subject,
            Dummy("보조 아군", subject.Type, 2, 120 * scale, 4 * scale),
        };
        float enemyHealth = strongEnemies ? 400 * scale : 100 * scale;
        float enemyAttack = strongEnemies ? 25 * scale : 6 * scale;
        var enemies = new List<BattleUnitSetup>
        {
            Dummy("기본 적 1", neutral, 0, enemyHealth, enemyAttack),
            Dummy("기본 적 2", neutral, 1, enemyHealth, enemyAttack),
        };

        var simulation = new BattleSimulation(players, enemies, typeMultipliers, seed);
        int triggers = 0;
        BattleSimUnit subjectUnit = simulation.Players[0];
        simulation.Event += battleEvent =>
        {
            if (battleEvent.Type == BattleEventType.Skill && battleEvent.Source == subjectUnit)
            {
                triggers++;
            }
        };
        simulation.Start();
        while (!simulation.IsOver && simulation.ElapsedTime < MaxSeconds)
        {
            simulation.Tick(Step);
        }
        won = simulation.IsOver && simulation.PlayerWon;
        return triggers;
    }

    private static BattleUnitSetup Dummy(string name, CreatureType type, int slot, float health, float attack)
    {
        return new BattleUnitSetup
        {
            Name = name,
            Type = type,
            Tier = 1,
            SlotIndex = slot,
            MaxHealth = (int)health,
            Attack = System.Math.Max(1, (int)attack),
            AttackSpeed = 1f,
            Skill = SkillType.None,
        };
    }

    // 서로 상성이 없는 타입 (배율 1)
    private static CreatureType NeutralType(CreatureType type)
    {
        switch (type)
        {
            case CreatureType.Fire: return CreatureType.Electric;
            case CreatureType.Water: return CreatureType.Rock;
            case CreatureType.Grass: return CreatureType.Electric;
            case CreatureType.Electric: return CreatureType.Fire;
            default: return CreatureType.Water;
        }
    }
}
