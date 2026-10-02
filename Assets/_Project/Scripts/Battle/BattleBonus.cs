// 전투 시작 시 크리처 한 마리에 붙는 보너스 (타입 시너지 + 트레이너 능력을 합친 값)
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
}
