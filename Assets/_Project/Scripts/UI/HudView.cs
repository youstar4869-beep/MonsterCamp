using UnityEngine;
using UnityEngine.UI;

// 상단 정보바: 라운드(깃발), 목숨(하트), 골드(동전), 이번 판에서 받은 별.
// 골드가 바뀌면 숫자가 잠깐 커졌다 돌아온다 (그동안만 Update가 켜진다)
public class HudView : MonoBehaviour
{
    [SerializeField] private Text _roundText;
    [Tooltip("보스 라운드일 때만 보이는 표시")]
    [SerializeField] private GameObject _bossTag;
    [Tooltip("목숨 최대치만큼 (왼쪽부터 채운다)")]
    [SerializeField] private Image[] _hearts;
    [SerializeField] private Sprite _heartSprite;
    [SerializeField] private Sprite _emptyHeartSprite;
    [SerializeField] private Text _goldText;
    [SerializeField] private Text _starsText;
    [Tooltip("골드가 바뀔 때 커지는 배율")]
    [SerializeField] private float _goldPopScale = 1.35f;
    [SerializeField] private float _goldPopDuration = 0.25f;

    private int _gold = -1;
    private float _popTime;

    private void Awake()
    {
        // 골드가 바뀔 때만 켠다
        enabled = false;
    }

    public void SetRound(int round, int roundCount, bool isBoss)
    {
        _roundText.text = $"{round}/{roundCount}";
        _bossTag.SetActive(isBoss);
    }

    public void SetLives(int lives)
    {
        for (int i = 0; i < _hearts.Length; i++)
        {
            _hearts[i].sprite = i < lives ? _heartSprite : _emptyHeartSprite;
        }
    }

    // 같은 값이면 아무것도 하지 않는다 (상점 화면이 자주 부르므로)
    public void SetGold(int gold)
    {
        if (gold == _gold)
        {
            return;
        }
        bool first = _gold < 0;
        _gold = gold;
        _goldText.text = gold.ToString();
        // 처음 값은 그냥 표시. 커지는 중에 또 바뀌면 처음부터
        if (!first && gameObject.activeInHierarchy)
        {
            _popTime = 0f;
            enabled = true;
        }
    }

    public void SetStars(int stars)
    {
        _starsText.text = stars.ToString();
    }

    public RectTransform GoldRect => _goldText.rectTransform;

    private void Update()
    {
        _popTime += Time.deltaTime;
        float t = _popTime / _goldPopDuration;
        float scale = t < 1f ? Mathf.Lerp(_goldPopScale, 1f, t * t) : 1f;
        _goldText.transform.localScale = new Vector3(scale, scale, 1f);
        if (t >= 1f)
        {
            enabled = false;
        }
    }

    private void OnDisable()
    {
        _goldText.transform.localScale = Vector3.one;
    }
}
