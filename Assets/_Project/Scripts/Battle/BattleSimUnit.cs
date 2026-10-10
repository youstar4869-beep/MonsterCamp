using System;

// 전투에 들어가는 크리처 1마리의 설정 (UnityEngine을 쓰지 않음: 화면 없는 시뮬레이션용)
public struct BattleUnitSetup
{
    public string Name;
    public CreatureType Type;
    public int Tier;
    // 별 (1~3)
    public int Level;
    // 0~1 앞줄, 2~4 뒷줄
    public int SlotIndex;
    // 레벨 배율을 적용한 값 (보너스 적용 전)
    public int MaxHealth;
    public int Attack;
    // 초당 공격 횟수
    public float AttackSpeed;
    // 시너지 + 트레이너 + 아이템
    public BattleBonus Bonus;

    public SkillType Skill;
    public float SkillEvery;
    public float SkillChance;
    public float SkillValue1;
    public float SkillValue2;

    // 두 번째 스킬 (조합 크리처). None이면 없음
    public SkillType Skill2;
    public float Skill2Every;
    public float Skill2Chance;
    public float Skill2Value1;
    public float Skill2Value2;
}

// 스킬 한 칸 (칸의 뜻은 SkillType 설명)
public struct SkillSlot
{
    public SkillType Type;
    public float Every;
    public float Chance;
    public float Value1;
    public float Value2;
}

// 전투 중 크리처 1마리의 상태 (BattleSimulation이 바꾼다)
public class BattleSimUnit
{
    public BattleUnitSetup Setup { get; }
    public bool IsPlayer { get; }
    public int SlotIndex => Setup.SlotIndex;
    public bool IsFrontRow => SlotIndex < BattleSimulation.FrontRowCount;
    public CreatureType Type => Setup.Type;
    // 첫 스킬 (두 스킬 모두 보려면 HasSkill)
    public SkillType Skill => Setup.Skill;

    public int MaxHealth { get; internal set; }
    public int Health { get; internal set; }
    public int Shield { get; internal set; }
    // 보너스까지 적용한 기본 공격력
    public int Attack { get; }
    // 보너스까지 적용한 기본 공격 간격 (초)
    public float BaseInterval { get; }
    public float Cooldown { get; internal set; }

    public bool IsAlive => Health > 0;
    // 기절·묶기 중에는 공격하지 못한다
    public bool IsStunned => StunTimer > 0f;
    public bool IsRooted => RootTimer > 0f;
    public bool IsDisabled => IsStunned || IsRooted;

    // ---- 보너스 ----
    internal float DamageReduction;
    internal float ThornsPercent;
    internal float RegenPercent;
    internal float RegenInterval;
    internal float RegenTimer;
    internal float EmergencyHealThreshold;
    internal float EmergencyHealPercent;
    internal bool EmergencyHealUsed;
    // 조합 시너지 (온천 회복, 뇌우 기절, 용광로 피해 배율, 무지개 시작 가속)
    internal float TeamRegenPercent;
    internal float TeamRegenInterval;
    internal float TeamRegenTimer;
    internal float StunChance;
    internal float StunSeconds;
    internal float CritChance;
    internal float CritMultiplier;
    internal float SynergyHasteTimer;
    // 보스 광폭화 공격력 배율 (1이면 없음)
    internal float BossAttackMultiplier = 1f;

    // ---- 스킬 ----
    // None이 아닌 스킬 칸 (첫 스킬, 두 번째 스킬 순). 스킬이 하나면 1칸
    internal readonly SkillSlot[] Skills;
    // N초마다 스킬 타이머 (Skills와 같은 순서)
    internal readonly float[] SkillTimers;

    // ---- 스킬 상태 ----
    internal int AttackCount;
    // 무시한 공격 수 (처음 받는 공격 무시)
    internal int IgnoredHits;
    internal bool LowHealthTriggered;
    internal bool Revived;
    internal int KillStacks;
    // 처치 누적 공격력 (히든 홍염제왕)
    internal int KillAttackStacks;
    // 아군 부활을 썼으면 true (히든 녹림여제)
    internal bool AllyReviveUsed;
    internal float HasteTimer;

    // ---- 상태이상 ----
    internal float StunTimer;
    internal float RootTimer;
    internal float BurnTimer;
    internal float BurnDamage;
    internal float BurnTickTimer;
    // 화상으로 쓰러지면 처치로 셀 크리처 (처치 누적 스킬이 있는 공격자만, 없으면 null)
    internal BattleSimUnit BurnSource;
    internal float SlowAmount;
    internal float SlowTimer;
    internal float WeakenAmount;
    internal float WeakenTimer;

    public BattleSimUnit(BattleUnitSetup setup, bool isPlayer)
    {
        Setup = setup;
        IsPlayer = isPlayer;
        BattleBonus bonus = setup.Bonus;

        int skillCount = (setup.Skill != SkillType.None ? 1 : 0) + (setup.Skill2 != SkillType.None ? 1 : 0);
        Skills = new SkillSlot[skillCount];
        SkillTimers = new float[skillCount];
        int slot = 0;
        if (setup.Skill != SkillType.None)
        {
            Skills[slot++] = new SkillSlot { Type = setup.Skill, Every = setup.SkillEvery, Chance = setup.SkillChance,
                Value1 = setup.SkillValue1, Value2 = setup.SkillValue2 };
        }
        if (setup.Skill2 != SkillType.None)
        {
            Skills[slot] = new SkillSlot { Type = setup.Skill2, Every = setup.Skill2Every, Chance = setup.Skill2Chance,
                Value1 = setup.Skill2Value1, Value2 = setup.Skill2Value2 };
        }
        for (int i = 0; i < skillCount; i++)
        {
            SkillTimers[i] = Skills[i].Every;
        }

        // 이전 BattleUnit.ApplyBonus와 같은 순서·같은 float 계산 (스킬이 없으면 결과가 같아야 한다)
        MaxHealth = Round(setup.MaxHealth * (1f + bonus.HealthBonus));
        Health = MaxHealth;
        Attack = Round(setup.Attack * (1f + bonus.AttackBonus));

        float speedBonus = bonus.SpeedBonus + (HasSkill(SkillType.AttackSpeedUp) ? GetSkill(SkillType.AttackSpeedUp).Value1 : 0f);
        float interval = 1f / setup.AttackSpeed;
        interval /= 1f + speedBonus;
        BaseInterval = interval;
        Cooldown = interval;

        float skillReduction = HasSkill(SkillType.DamageReduction) ? GetSkill(SkillType.DamageReduction).Value1
            : HasSkill(SkillType.DamageReductionDeathStun) ? GetSkill(SkillType.DamageReductionDeathStun).Value1
            : HasSkill(SkillType.DamageReductionThorns) ? GetSkill(SkillType.DamageReductionThorns).Value1
            : 0f;
        DamageReduction = skillReduction > 0f
            ? 1f - (1f - bonus.DamageReduction) * (1f - skillReduction)
            : bonus.DamageReduction;
        ThornsPercent = bonus.ThornsPercent + (HasSkill(SkillType.Thorns) ? GetSkill(SkillType.Thorns).Value1 : 0f);
        if (HasSkill(SkillType.DamageReductionThorns))
        {
            ThornsPercent += GetSkill(SkillType.DamageReductionThorns).Value2;
        }

        RegenPercent = bonus.RegenPercent;
        RegenInterval = bonus.RegenInterval;
        RegenTimer = RegenInterval;
        EmergencyHealThreshold = bonus.EmergencyHealThreshold;
        EmergencyHealPercent = bonus.EmergencyHealPercent;
        TeamRegenPercent = bonus.TeamRegenPercent;
        TeamRegenInterval = bonus.TeamRegenInterval;
        TeamRegenTimer = TeamRegenInterval;
        StunChance = bonus.StunChance;
        StunSeconds = bonus.StunSeconds;
        CritChance = bonus.CritChance;
        CritMultiplier = bonus.CritMultiplier;
    }

    public bool HasSkill(SkillType type)
    {
        foreach (SkillSlot skill in Skills)
        {
            if (skill.Type == type)
            {
                return true;
            }
        }
        return false;
    }

    // 그 종류의 스킬 칸 (없으면 빈 칸). 같은 종류가 두 칸이면 첫 칸
    internal SkillSlot GetSkill(SkillType type)
    {
        foreach (SkillSlot skill in Skills)
        {
            if (skill.Type == type)
            {
                return skill;
            }
        }
        return default;
    }

    // 지금 공격력: 체력이 낮아져 오른 공격력, 약화(공격력 감소), 보스 광폭화 반영
    public float CurrentAttack
    {
        get
        {
            float attack = Attack;
            if (LowHealthTriggered && HasSkill(SkillType.LowHealthAttackUp))
            {
                attack *= 1f + GetSkill(SkillType.LowHealthAttackUp).Value2;
            }
            if (KillAttackStacks > 0)
            {
                attack *= 1f + KillAttackStacks * GetSkill(SkillType.OnKillAttackUp).Value1;
            }
            if (WeakenAmount > 0f)
            {
                attack *= 1f - WeakenAmount;
            }
            if (BossAttackMultiplier != 1f)
            {
                attack *= BossAttackMultiplier;
            }
            return attack;
        }
    }

    // 지금 공격 간격: 처치 누적 가속, 시작 가속(스킬·무지개), 둔화(공격 속도 감소) 반영
    public float CurrentInterval
    {
        get
        {
            float speed = 1f;
            if (KillStacks > 0)
            {
                speed *= 1f + KillStacks * GetSkill(SkillType.OnKillAttackSpeed).Value1;
            }
            if (HasteTimer > 0f)
            {
                speed *= GetSkill(SkillType.BattleStartHaste).Value1;
            }
            if (SynergyHasteTimer > 0f)
            {
                speed *= 1f + Setup.Bonus.StartHasteBonus;
            }
            if (SlowAmount > 0f)
            {
                speed *= 1f - SlowAmount;
            }
            return speed == 1f ? BaseInterval : BaseInterval / speed;
        }
    }

    // Mathf.RoundToInt와 같은 반올림 (가운데 값은 짝수 쪽)
    internal static int Round(float value) => (int)Math.Round(value);
}
