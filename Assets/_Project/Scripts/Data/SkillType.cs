// 크리처 스킬 종류 (UnityEngine을 쓰지 않음: 전투 시뮬레이션에서 공용).
// 숫자 칸: Every(N번째 공격 또는 N초), Chance(확률 0~1), Value1(주 수치), Value2(보조 수치: 지속 시간 등)
// 비율은 0~1 (0.3 = 30%). 공격력 비율은 공격하는 크리처의 공격력 기준
public enum SkillType
{
    None,

    // ---- N번째 공격마다 ----
    // Every번째 공격마다 피해 ×Value1
    EveryNthAttackDamage,
    // Every번째 공격마다 적 전체에 공격력 ×Value1 피해
    EveryNthAttackHitAll,
    // Every번째 공격마다 체력 비율이 가장 낮은 아군 회복 (그 아군 최대 체력 ×Value1)
    EveryNthAttackHealLowest,
    // Every번째 공격마다 뒷줄 적 1마리에게 공격력 ×Value1 피해 (뒷줄이 없으면 앞줄)
    EveryNthAttackHitBackRow,
    // Every번째 공격마다 적 전체에 공격력 ×Value1 피해 + 아군 전체 회복 (각자 최대 체력 ×Value2)
    EveryNthAttackHitAllHealTeam,

    // ---- 공격할 때 ----
    // 첫 공격 피해 ×Value1
    FirstAttackDamage,
    // 공격한 적의 공격 속도 Value1 감소 (전투 끝까지, 중첩 없음)
    OnHitSlow,
    // 공격 시 Chance 확률로 대상을 Value2초 묶기 (공격 못 함)
    OnHitChanceRoot,
    // 공격 시 Chance 확률로 한 번 더 공격
    OnHitChanceExtraAttack,
    // 공격한 적에게 Value2초 동안 화상: 1초마다 공격력 ×Value1 피해 (다시 걸면 시간만 새로)
    OnHitBurn,
    // 공격 시 Chance 확률로 적 공격력 Value1 감소, Value2초 (중첩 없음, 센 쪽)
    OnHitChanceWeaken,
    // 공격 시 Chance 확률로 옆 적(같은 줄, 가장 가까운 칸)에게 공격력 ×Value1 피해
    OnHitChanceSplash,
    // 한 번에 적 Value1마리를 동시에 공격 (앞줄부터)
    MultiTarget,

    // ---- 받을 때 ----
    // 받는 피해 Value1 감소
    DamageReduction,
    // 처음 받는 공격 피해 무시
    IgnoreFirstHit,
    // 받은 공격 피해의 Value1만큼 공격자에게 반사
    Thorns,

    // ---- 상시 ----
    // 공격 속도 +Value1
    AttackSpeedUp,

    // ---- N초마다 ----
    // Every초마다 자신 회복 (최대 체력 ×Value1)
    PeriodicSelfHeal,
    // Every초마다 적 전체 공격 속도 Value1 감소, Value2초 (중첩 없음, 센 쪽)
    PeriodicSlowAllEnemies,

    // ---- 전투 시작 시 ----
    // 아군 전체 최대 체력 +Value1
    BattleStartTeamMaxHealth,
    // 아군 전체 최대 체력 +Value1, Every초마다 아군 전체 회복 (각자 최대 체력 ×Value2)
    TeamHealthAndRegen,
    // 전투 시작 후 Value2초 동안 공격 속도 ×Value1
    BattleStartHaste,
    // 앞줄 적 1마리 Value1초 기절 (앞줄이 없으면 맨 앞 적)
    BattleStartStunFront,
    // 뒷줄 적 1마리 Value1초 묶기 (뒷줄이 없으면 맨 앞 적)
    BattleStartRootBack,
    // 아군 전체에 최대 체력 ×Value1 보호막
    BattleStartTeamShield,

    // ---- 체력이 처음 Value1 이하가 될 때 (한 번) ----
    // 공격력 +Value2
    LowHealthAttackUp,
    // 최대 체력 ×Value2 보호막
    LowHealthShield,

    // ---- 쓰러질 때 ----
    // 아군 전체 회복 (각자 최대 체력 ×Value1)
    OnDeathHealTeam,
    // 처음 쓰러지면 최대 체력 ×Value1로 한 번 부활
    Revive,
    // 받는 피해 Value1 감소 + 쓰러지면 적 전체 Value2초 기절
    DamageReductionDeathStun,

    // ---- 적을 쓰러뜨렸을 때 ----
    // 공격 속도 +Value1 (누적)
    OnKillAttackSpeed,
}
