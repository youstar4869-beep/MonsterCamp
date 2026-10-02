using UnityEngine;

public enum TrainerAbility
{
    // 대상 타입 공격력 +Value (비율)
    TypeAttackBonus,
    // 대상 타입이 전투 중 체력이 처음 HealthThreshold 아래로 떨어지면 최대 체력의 Value만큼 한 번 회복
    TypeLowHealthHeal,
    // 라운드마다 골드 +Value
    RoundGoldBonus
}

// 트레이너 1명: 이름, 설명, 능력 종류, 수치
[CreateAssetMenu(fileName = "NewTrainer", menuName = "MonsterCamp/Trainer Data")]
public class TrainerData : ScriptableObject
{
    [SerializeField] private string _displayName;
    [Tooltip("선택 화면에 보이는 설명. 수치를 바꾸면 설명도 같이 고칠 것")]
    [SerializeField, TextArea] private string _description;
    [SerializeField] private TrainerAbility _ability;
    [Tooltip("TypeAttackBonus, TypeLowHealthHeal에서 사용")]
    [SerializeField] private CreatureType _targetType;
    [Tooltip("공격력 비율(0.2 = +20%) / 회복 비율(0.3 = 최대 체력 30%) / 골드")]
    [SerializeField] private float _value;
    [Tooltip("TypeLowHealthHeal: 이 체력 비율 아래로 떨어지면 발동 (0.5 = 50%)")]
    [SerializeField, Range(0f, 1f)] private float _healthThreshold = 0.5f;

    public string DisplayName => _displayName;
    public string Description => _description;
    public TrainerAbility Ability => _ability;
    public CreatureType TargetType => _targetType;
    public float Value => _value;
    public float HealthThreshold => _healthThreshold;

    public int RoundGoldBonus => _ability == TrainerAbility.RoundGoldBonus ? Mathf.RoundToInt(_value) : 0;

    // 전투 보너스에 트레이너 능력을 더한다 (시너지와 겹쳐서 적용)
    public void AddBattleBonus(CreatureType type, ref BattleBonus bonus)
    {
        if (type != _targetType)
        {
            return;
        }

        switch (_ability)
        {
            case TrainerAbility.TypeAttackBonus:
                bonus.AttackBonus += _value;
                break;
            case TrainerAbility.TypeLowHealthHeal:
                bonus.EmergencyHealThreshold = _healthThreshold;
                bonus.EmergencyHealPercent = _value;
                break;
        }
    }
}
