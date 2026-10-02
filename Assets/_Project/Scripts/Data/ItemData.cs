using UnityEngine;

public enum ItemEffect
{
    // 공격력 +Value (비율)
    AttackBonus,
    // 체력 +Value (비율)
    HealthBonus,
    // 공격 속도 +Value (비율)
    SpeedBonus,
    // 1초마다 최대 체력의 Value만큼 회복
    Regen,
    // 받은 피해의 Value만큼 공격자에게 되돌림
    Thorns,
    // 전투에서 이기면 다음 라운드 골드 +Value
    WinGold
}

// 아이템 1종: 이름, 설명, 효과, 가격. 아이콘은 임시로 색 네모 + 글자
[CreateAssetMenu(fileName = "NewItem", menuName = "MonsterCamp/Item Data")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _displayName;
    [Tooltip("아이콘에 들어가는 한 글자")]
    [SerializeField] private string _iconLabel;
    [SerializeField] private Color _iconColor = Color.white;
    [Tooltip("수치를 바꾸면 설명도 같이 고칠 것")]
    [SerializeField, TextArea] private string _description;
    [SerializeField] private ItemEffect _effect;
    [Tooltip("비율(0.2 = 20%) 또는 골드")]
    [SerializeField] private float _value;
    [SerializeField, Min(0)] private int _price = 3;

    public string DisplayName => _displayName;
    public string IconLabel => _iconLabel;
    public Color IconColor => _iconColor;
    public string Description => _description;
    public ItemEffect Effect => _effect;
    public float Value => _value;
    public int Price => _price;

    public int WinGold => _effect == ItemEffect.WinGold ? Mathf.RoundToInt(_value) : 0;

    // 회복 열매 간격. 풀 시너지 회복이 이미 있으면 그 간격에 회복량만 더한다
    private const float RegenInterval = 1f;

    // 전투 보너스에 더한다 (시너지·트레이너와 겹쳐서 적용)
    public void AddBattleBonus(ref BattleBonus bonus)
    {
        switch (_effect)
        {
            case ItemEffect.AttackBonus:
                bonus.AttackBonus += _value;
                break;
            case ItemEffect.HealthBonus:
                bonus.HealthBonus += _value;
                break;
            case ItemEffect.SpeedBonus:
                bonus.SpeedBonus += _value;
                break;
            case ItemEffect.Regen:
                bonus.RegenPercent += _value;
                if (bonus.RegenInterval <= 0f)
                {
                    bonus.RegenInterval = RegenInterval;
                }
                break;
            case ItemEffect.Thorns:
                bonus.ThornsPercent += _value;
                break;
        }
    }
}
