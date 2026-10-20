using UnityEngine;
using UnityEngine.UI;

// 전투 중 크리처 한 마리 표시: 레벨 그림(통통 튀는 움직임), 이름, 레벨 별, 체력바(+보호막, 줄어든 만큼 뒤따라오는 흰 막대),
// 장착 아이템, 기절·묶기 중에는 흐리게.
// 연출: 공격하면 앞으로 살짝 튀어나갔다 돌아옴, 맞으면 하얗게 깜빡임, 쓰러지면 작아지며 사라짐.
// 연출은 모두 이 칸의 Update 하나에서 시간으로 계산한다 (전투 중 새로 만드는 것 없음)
public class CreatureUnitView : MonoBehaviour
{
    private const float DisabledAlpha = 0.4f;

    [SerializeField] private Image _icon;
    [SerializeField] private Text _nameText;
    [SerializeField] private Text _levelText;
    [Tooltip("체력 비율만큼 가로 길이를 줄인다 (anchorMax.x)")]
    [SerializeField] private RectTransform _healthFill;
    [Tooltip("체력이 줄면 잠깐 뒤에 천천히 따라 줄어드는 흰 막대 (체력 칸 뒤)")]
    [SerializeField] private RectTransform _healthTrail;
    [Tooltip("보호막: 체력 칸 바로 뒤에 흰 칸 (anchorMin.x ~ anchorMax.x)")]
    [SerializeField] private RectTransform _shieldFill;
    [Tooltip("맞았을 때 깜빡이는 흰 판 (칸 전체를 덮음, 평소엔 투명)")]
    [SerializeField] private Image _hitFlash;
    [SerializeField] private ItemIconView _itemIcon;
    [Tooltip("칸 아래 가는 막대: 크리처 타입 색")]
    [SerializeField] private Image _typeBar;
    [Tooltip("보스 철벽 중 반투명 흰 테두리 (평소엔 꺼짐)")]
    [SerializeField] private Outline _guardFrame;
    [SerializeField] private UITheme _theme;
    [Tooltip("내 팀이면 위(적 쪽)로, 적 팀이면 아래로 튀어나간다")]
    [SerializeField] private bool _facesUp = true;

    [Header("통통 튀는 움직임")]
    [Tooltip("가장 높이 뛸 때 높이 (1080x1920 기준 픽셀)")]
    [SerializeField] private float _bounceHeight = 8f;
    [Tooltip("1초에 튀는 횟수")]
    [SerializeField] private float _bouncesPerSecond = 2f;

    [Header("연출")]
    [Tooltip("공격할 때 앞으로 나가는 거리 (픽셀)")]
    [SerializeField] private float _lungeDistance = 28f;
    [SerializeField] private float _lungeDuration = 0.18f;
    [Tooltip("맞았을 때 흰 판의 처음 진하기")]
    [SerializeField] private float _hitFlashAlpha = 0.3f;
    [SerializeField] private float _hitFlashDuration = 0.12f;
    [SerializeField] private float _deathDuration = 0.3f;
    [Tooltip("흰 막대가 따라오기 시작하기 전 기다리는 시간 (초)")]
    [SerializeField] private float _trailDelay = 0.25f;
    [Tooltip("흰 막대가 1초에 줄어드는 비율 (체력 전체 = 1)")]
    [SerializeField] private float _trailSpeed = 0.9f;

    private RectTransform _rect;
    private RectTransform _iconRect;
    private Vector2 _iconBasePosition;
    private Vector2 _basePosition;
    // 크리처마다 튀는 박자를 다르게
    private float _bouncePhase;
    private bool _disabled;
    private readonly Vector3[] _corners = new Vector3[4];
    // 씬에 놓인 그림·체력바·별 칸 (큰 그림일 때 바꾸고, 보통 그림이면 되돌린다)
    private bool _layoutSaved;
    private Vector2 _iconMin, _iconMax, _barMin, _barMax, _levelMin, _levelMax;

    // 연출 타이머 (음수 = 진행 중 아님)
    private float _lungeTime = -1f;
    private float _flashTime = -1f;
    private float _deathTime = -1f;
    private float _health = 1f;
    private float _trail = 1f;
    private float _trailWait;

    private void Awake()
    {
        _rect = (RectTransform)transform;
        _basePosition = _rect.anchoredPosition;
        _iconRect = _icon.rectTransform;
        _iconBasePosition = _iconRect.anchoredPosition;
    }

    public void Show(CreatureData creature, int level, ItemData item)
    {
        _icon.sprite = creature.GetSprite(level);
        FitSprite(_icon.sprite);
        _nameText.text = creature.DisplayName;
        _levelText.text = CreatureInstance.ToStars(level);
        _itemIcon.Show(item);
        _typeBar.color = _theme.TypeColor(creature.Type);
        _typeBar.enabled = true;
        ResetEffects();
        SetHealth(1f, 0f);
        SetDisabled(false);
        _bouncePhase = Random.value;
        gameObject.SetActive(true);
    }

    // 32x32 기준보다 큰 그림(3성 의인화 48x48 = 1.5배)은 발 위치(그림 칸 아래쪽)는 그대로 두고 위·옆으로 크게.
    // 그림이 체력바에 닿으면 체력바를 그림 바로 위로 올리고, 별은 체력바 위에
    private void FitSprite(Sprite sprite)
    {
        RectTransform icon = _icon.rectTransform;
        var bar = (RectTransform)_healthFill.parent;
        RectTransform levelRect = _levelText.rectTransform;
        if (!_layoutSaved)
        {
            _layoutSaved = true;
            _iconMin = icon.anchorMin;
            _iconMax = icon.anchorMax;
            _barMin = bar.anchorMin;
            _barMax = bar.anchorMax;
            _levelMin = levelRect.anchorMin;
            _levelMax = levelRect.anchorMax;
        }

        float scale = sprite != null && sprite.pixelsPerUnit > 0f ? Mathf.Max(1f, sprite.rect.height / sprite.pixelsPerUnit) : 1f;
        float centerX = (_iconMin.x + _iconMax.x) * 0.5f;
        float halfWidth = (_iconMax.x - _iconMin.x) * 0.5f * scale;
        float top = _iconMin.y + (_iconMax.y - _iconMin.y) * scale;
        icon.anchorMin = new Vector2(centerX - halfWidth, _iconMin.y);
        icon.anchorMax = new Vector2(centerX + halfWidth, top);

        float lift = scale > 1f ? Mathf.Max(0f, top + 0.01f - _barMin.y) : 0f;
        bar.anchorMin = _barMin + new Vector2(0f, lift);
        bar.anchorMax = _barMax + new Vector2(0f, lift);
        if (lift > 0f)
        {
            float levelHeight = _levelMax.y - _levelMin.y;
            levelRect.anchorMin = new Vector2(_levelMin.x, bar.anchorMax.y);
            levelRect.anchorMax = new Vector2(_levelMax.x, bar.anchorMax.y + levelHeight);
        }
        else
        {
            levelRect.anchorMin = _levelMin;
            levelRect.anchorMax = _levelMax;
        }
    }

    // health, shield: 최대 체력 대비 비율. 보호막은 체력 칸 뒤에 이어서 (넘치면 끝까지)
    public void SetHealth(float health, float shield)
    {
        health = Mathf.Clamp01(health);
        if (health < _health)
        {
            // 줄었으면 흰 막대는 잠깐 그대로 있다가 따라온다
            _trailWait = _trailDelay;
        }
        _health = health;
        if (_trail < health)
        {
            // 회복은 바로 (흰 막대가 체력 칸보다 짧을 수 없다)
            _trail = health;
        }
        SetFill(_healthFill, health);
        SetFill(_healthTrail, _trail);

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

    // 보스 철벽 중 반투명 흰 테두리
    public void SetGuarded(bool guarded)
    {
        if (_guardFrame != null && _guardFrame.enabled != guarded)
        {
            _guardFrame.enabled = guarded;
        }
    }

    public void PlayAttack()
    {
        if (_deathTime < 0f)
        {
            _lungeTime = 0f;
        }
    }

    public void PlayHit()
    {
        if (_deathTime < 0f)
        {
            _flashTime = 0f;
        }
    }

    // 작아지며 사라진 뒤 꺼진다
    public void PlayDeath()
    {
        if (gameObject.activeInHierarchy)
        {
            _deathTime = 0f;
            _lungeTime = -1f;
        }
        else
        {
            Hide();
        }
    }

    // 부활: 사라지던 중이면 되돌린다
    public void PlayRevive()
    {
        ResetEffects();
        gameObject.SetActive(true);
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

    // 데미지·회복 숫자가 뜰 위치 (크리처 그림 가운데)
    public Vector3 HitTextPosition
    {
        get
        {
            _iconRect.GetWorldCorners(_corners);
            return (_corners[0] + _corners[2]) * 0.5f;
        }
    }

    // 전투 화면에 보이는 동안만 불린다 (쓰러지면 꺼짐). 기절·묶기 중에는 튀지 않는다
    private void Update()
    {
        float dt = Time.deltaTime;

        float bounce = _disabled ? 0f : Mathf.Abs(Mathf.Sin((Time.time * _bouncesPerSecond + _bouncePhase) * Mathf.PI));
        _iconRect.anchoredPosition = _iconBasePosition + new Vector2(0f, bounce * _bounceHeight);

        if (_lungeTime >= 0f)
        {
            _lungeTime += dt;
            float t = _lungeTime / _lungeDuration;
            // 0 → 1 → 0 (나갈 때 빠르고 돌아올 때 천천히)
            float offset = t < 0.35f ? t / 0.35f : Mathf.Max(0f, 1f - (t - 0.35f) / 0.65f);
            _rect.anchoredPosition = _basePosition + new Vector2(0f, (_facesUp ? 1f : -1f) * offset * _lungeDistance);
            if (t >= 1f)
            {
                _lungeTime = -1f;
                _rect.anchoredPosition = _basePosition;
            }
        }

        if (_flashTime >= 0f)
        {
            _flashTime += dt;
            float t = _flashTime / _hitFlashDuration;
            SetFlash(t >= 1f ? 0f : _hitFlashAlpha * (1f - t));
            if (t >= 1f)
            {
                _flashTime = -1f;
            }
        }

        if (_trail > _health)
        {
            if (_trailWait > 0f)
            {
                _trailWait -= dt;
            }
            else
            {
                _trail = Mathf.Max(_health, _trail - _trailSpeed * dt);
                SetFill(_healthTrail, _trail);
            }
        }

        if (_deathTime >= 0f)
        {
            _deathTime += dt;
            float t = Mathf.Clamp01(_deathTime / _deathDuration);
            float scale = 1f - t * t;
            _rect.localScale = new Vector3(scale, scale, 1f);
            if (t >= 1f)
            {
                Hide();
            }
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void ResetEffects()
    {
        _lungeTime = -1f;
        _flashTime = -1f;
        _deathTime = -1f;
        _health = 1f;
        _trail = 1f;
        _trailWait = 0f;
        if (_rect != null)
        {
            _rect.anchoredPosition = _basePosition;
            _rect.localScale = Vector3.one;
        }
        SetFlash(0f);
    }

    private void SetFlash(float alpha)
    {
        if (_hitFlash != null)
        {
            _hitFlash.color = new Color(1f, 1f, 1f, alpha);
        }
    }

    private static void SetFill(RectTransform fill, float amount)
    {
        if (fill == null)
        {
            return;
        }
        Vector2 max = fill.anchorMax;
        max.x = amount;
        fill.anchorMax = max;
    }
}
