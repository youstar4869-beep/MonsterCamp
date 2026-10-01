using UnityEngine;
using UnityEngine.UI;

// 전투 중 크리처 한 마리 표시: 그림, 이름, 체력바
public class CreatureUnitView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [Tooltip("체력 비율만큼 가로 길이를 줄인다 (anchorMax.x)")]
    [SerializeField] private RectTransform _healthFill;

    public void Show(CreatureData creature)
    {
        _icon.sprite = creature.Sprite;
        _nameText.text = creature.DisplayName;
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
