using UnityEngine;
using UnityEngine.UI;

// 전투 중 크리처 한 마리 표시: 레벨 그림(통통 튀는 움직임), 이름, 레벨 별, 체력바, 장착 아이템
public class CreatureUnitView : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;
    [Tooltip("체력 비율만큼 가로 길이를 줄인다 (anchorMax.x)")]
    [SerializeField] private RectTransform _healthFill;
    [SerializeField] private ItemIconView _itemIcon;

    [Header("통통 튀는 움직임")]
    [Tooltip("가장 높이 뛸 때 높이 (1080x1920 기준 픽셀)")]
    [SerializeField] private float _bounceHeight = 8f;
    [Tooltip("1초에 튀는 횟수")]
    [SerializeField] private float _bouncesPerSecond = 2f;

    private RectTransform _iconRect;
    private Vector2 _iconBasePosition;
    // 크리처마다 튀는 박자를 다르게
    private float _bouncePhase;

    private void Awake()
    {
        _iconRect = _icon.rectTransform;
        _iconBasePosition = _iconRect.anchoredPosition;
    }

    public void Show(CreatureData creature, int level, ItemData item)
    {
        _icon.sprite = creature.GetSprite(level);
        _nameText.text = creature.DisplayName;
        _levelText.text = CreatureInstance.ToStars(level);
        _itemIcon.Show(item);
        SetHealth(1f);
        _bouncePhase = Random.value;
        gameObject.SetActive(true);
    }

    // 전투 화면에 보이는 동안만 불린다 (쓰러지면 꺼짐)
    private void Update()
    {
        float bounce = Mathf.Abs(Mathf.Sin((Time.time * _bouncesPerSecond + _bouncePhase) * Mathf.PI));
        _iconRect.anchoredPosition = _iconBasePosition + new Vector2(0f, bounce * _bounceHeight);
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
