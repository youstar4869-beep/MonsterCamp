// 보스 특수 능력을 합친 전투 입력 (적 팀 전체에 적용, 모두 0이면 없음). BossAbility.Combine으로 만든다
public struct BossEffects
{
    // 전투 시작 이 초 동안 적 전체 피해 무시
    public float InvulnerableSeconds;
    // 이 초가 지나면 적 전체 공격력 EnrageAttackMultiplier배 (배율 0이면 없음)
    public float EnrageAfterSeconds;
    public float EnrageAttackMultiplier;
    // RegenInterval초마다 적 전체 최대 체력 비율 회복
    public float RegenPercent;
    public float RegenInterval;
    // 가시 갑옷처럼 받은 피해의 비율을 공격자에게
    public float ThornsPercent;
    // 공격 속도 +비율 (시너지 공격 속도 보너스에 더함)
    public float AttackSpeedBonus;
}
