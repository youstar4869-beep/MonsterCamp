using System;

// 전투에 들어가는 크리처 1마리의 설정 (UnityEngine을 쓰지 않음: 화면 없는 시뮬레이션용)
public struct BattleUnitSetup
{
    public string Name;
    public CreatureType Type;
    public int Tier;
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
}

// 전투 중 크리처 1마리의 상태 (BattleSimulation이 바꾼다)
public class BattleSimUnit
{
    public BattleUnitSetup Setup { get; }
    public bool IsPlayer { get; }
    public int SlotIndex => Setup.SlotIndex;
    public bool IsFrontRow => SlotIndex < BattleSimulation.FrontRowCount;
    public CreatureType Type => Setup.Type;
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

    // ---- 스킬 상태 ----
    internal int AttackCount;
    internal float SkillTimer;
    internal bool FirstHitIgnored;
    internal bool LowHealthTriggered;
    internal bool Revived;
    internal int KillStacks;
    internal float HasteTimer;

    // ---- 상태이상 ----
    internal float StunTimer;
    internal float RootTimer;
    internal float BurnTimer;
    internal float BurnDamage;
    internal float BurnTickTimer;
    internal float SlowAmount;
    internal float SlowTimer;
    internal float WeakenAmount;
    internal float WeakenTimer;

    public BattleSimUnit(BattleUnitSetup setup, bool isPlayer)
    {
        Setup = setup;
        IsPlayer = isPlayer;
        BattleBonus bonus = setup.Bonus;

        // 이전 BattleUnit.ApplyBonus와 같은 순서·같은 float 계산 (스킬이 없으면 결과가 같아야 한다)
        MaxHealth = Round(setup.MaxHealth * (1f + bonus.HealthBonus));
        Health = MaxHealth;
        Attack = Round(setup.Attack * (1f + bonus.AttackBonus));

        float speedBonus = bonus.SpeedBonus + (setup.Skill == SkillType.AttackSpeedUp ? setup.SkillValue1 : 0f);
        float interval = 1f / setup.AttackSpeed;
        interval /= 1f + speedBonus;
        BaseInterval = interval;
        Cooldown = interval;

        float skillReduction = setup.Skill == SkillType.DamageReduction || setup.Skill == SkillType.DamageReductionDeathStun
            ? setup.SkillValue1 : 0f;
        DamageReduction = skillReduction > 0f
            ? 1f - (1f - bonus.DamageReduction) * (1f - skillReduction)
            : bonus.DamageReduction;
        ThornsPercent = bonus.ThornsPercent + (setup.Skill == SkillType.Thorns ? setup.SkillValue1 : 0f);

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

        SkillTimer = setup.SkillEvery;
    }

    // 지금 공격력: 체력이 낮아져 오른 공격력, 약화(공격력 감소) 반영
    public float CurrentAttack
    {
        get
        {
            float attack = Attack;
            if (LowHealthTriggered && Skill == SkillType.LowHealthAttackUp)
            {
                attack *= 1f + Setup.SkillValue2;
            }
            if (WeakenAmount > 0f)
            {
                attack *= 1f - WeakenAmount;
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
                speed *= 1f + KillStacks * Setup.SkillValue1;
            }
            if (HasteTimer > 0f)
            {
                speed *= Setup.SkillValue1;
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
