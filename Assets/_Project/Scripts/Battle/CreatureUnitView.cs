using UnityEngine;
using UnityEngine.UI;

// 전투 중 크리처 한 마리 표시: 그림, 이름, 레벨 별, 체력바, 장착 아이템
public class CreatureUnitView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;
    [Tooltip("체력 비율만큼 가로 길이를 줄인다 (anchorMax.x)")]
    [SerializeField] private RectTransform _healthFill;
    [SerializeField] private ItemIconView _itemIcon;

    public void Show(CreatureData creature, int level, ItemData item)
    {
        _icon.sprite = creature.Sprite;
        _nameText.text = creature.DisplayName;
        _levelText.text = CreatureInstance.ToStars(level);
        _itemIcon.Show(item);
        SetHealth(1f);
        gameObject.SetActive(true);
    }

    public void SetHealth(float ratio)
    {
        Vector2 anchorMax = _healthFill.anchorMax;
        anchorMax.x = Mathf.Clamp01(ratio);
        _healthFill.anchorMax = anchorMax;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
