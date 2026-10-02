using UnityEngine;
using UnityEngine.UI;

// 전투 중 크리처 한 마리 표시: 레벨 그림(통통 튀는 움직임), 이름, 레벨 별, 체력바(+보호막), 장착 아이템,
// 기절·묶기 중에는 흐리게
public class CreatureUnitView : MonoBehaviour
{
    private const float DisabledAlpha = 0.4f;

    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;
    [Tooltip("체력 비율만큼 가로 길이를 줄인다 (anchorMax.x)")]
    [SerializeField] private RectTransform _healthFill;
    [Tooltip("보호막: 체력 칸 바로 뒤에 흰 칸 (anchorMin.x ~ anchorMax.x)")]
    [SerializeField] private RectTransform _shieldFill;
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
    private bool _disabled;
    private readonly Vector3[] _corners = new Vector3[4];

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
        SetHealth(1f, 0f);
        SetDisabled(false);
        _bouncePhase = Random.value;
        gameObject.SetActive(true);
    }

    // health, shield: 최대 체력 대비 비율. 보호막은 체력 칸 뒤에 이어서 (넘치면 끝까지)
    public void SetHealth(float health, float shield)
    {
        health = Mathf.Clamp01(health);
        Vector2 healthMax = _healthFill.anchorMax;
        healthMax.x = health;
        _healthFill.anchorMax = healthMax;

        bool hasShield = shield > 0f;
        _shieldFill.gameObject.SetActive(hasShield);
        if (hasShield)
        {
            float start = Mathf.Min(health, 1f - Mathf.Min(1f, shield));
            _shieldFill.anchorMin = new Vector2(start, _shieldFill.anchorMin.y);
            _shieldFill.anchorMax = new Vector2(Mathf.Min(1f, start + shield), _shieldFill.anchorMax.y);
        }
    }

    // 기절·묶기 중에는 크리처를 살짝 흐리게
    public void SetDisabled(bool disabled)
    {
        if (_disabled == disabled)
        {
            return;
        }
        _disabled = disabled;
        _icon.color = new Color(1f, 1f, 1f, disabled ? DisabledAlpha : 1f);
    }

    // 스킬 글이 뜰 위치 (크리처 그림 위쪽 가운데)
    public Vector3 SkillTextPosition
    {
        get
        {
            _iconRect.GetWorldCorners(_corners);
            return (_corners[1] + _corners[2]) * 0.5f;
        }
    }

    // 전투 화면에 보이는 동안만 불린다 (쓰러지면 꺼짐). 기절·묶기 중에는 튀지 않는다
    private void Update()
    {
        float bounce = _disabled ? 0f : Mathf.Abs(Mathf.Sin((Time.time * _bouncesPerSecond + _bouncePhase) * Mathf.PI));
        _iconRect.anchoredPosition = _iconBasePosition + new Vector2(0f, bounce * _bounceHeight);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
