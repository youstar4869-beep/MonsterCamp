// 전투 시작 시 크리처 한 마리에 붙는 보너스 (타입·조합 시너지 + 트레이너 능력 + 아이템을 합친 값)
public struct BattleBonus
{
    public float AttackBonus;
    public float HealthBonus;
    public float SpeedBonus;
    public float DamageReduction;
    public float RegenPercent;
    public float RegenInterval;
    // 체력이 처음 이 비율 아래로 떨어지면 최대 체력의 EmergencyHealPercent만큼 한 번 회복 (0이면 없음)
    public float EmergencyHealThreshold;
    public float EmergencyHealPercent;
    // 받은 피해의 이 비율만큼 공격자에게 되돌림
    public float ThornsPercent;

    // ---- 조합 시너지 (모두 0이면 없음) ----
    // 온천: TeamRegenInterval초마다 최대 체력의 비율 회복 (풀 시너지 회복과 따로 돈다)
    public float TeamRegenPercent;
    public float TeamRegenInterval;
    // 뇌우: 공격 시 확률로 대상 기절
    public float StunChance;
    public float StunSeconds;
    // 수정숲: 전투 시작 시 최대 체력 비율 보호막
    public float StartShieldPercent;
    // 용광로: 공격 시 확률로 피해 배율
    public float CritChance;
    public float CritMultiplier;
    // 증기화산: 전투 시작 시 적 전체 최대 체력 비율 피해 (팀에서 가장 큰 값 한 번)
    public float StartEnemyDamagePercent;
    // 무지개: 전투 시작 StartHasteSeconds초 동안 공격 속도 +비율
    public float StartHasteBonus;
    public float StartHasteSeconds;
}
