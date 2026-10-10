using UnityEngine;
using UnityEngine.UI;

// 상단 정보바: 층(깃발), 목숨(하트), 골드(동전), 이번 판에서 받은 별.
// 골드가 바뀌면 숫자가 잠깐 커졌다 돌아온다 (그동안만 Update가 켜진다. 씬에는 꺼진 채로 저장)
public class HudView : MonoBehaviour
{
    [Tooltip("층 (예: 12/30층)")]
    [SerializeField] private Text _roundText;
    [Tooltip("보스 층일 때만 보이는 표시")]
    [SerializeField] private GameObject _bossTag;
    [Tooltip("가장 많은 목숨만큼 (왼쪽부터 채운다). 하트 영역 안에서 이번 판 최대 목숨 수로 나눠 놓는다")]
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

    // 예: "12/30층"
    public void SetFloor(int floor, int floorCount, bool isBoss)
    {
        _roundText.text = $"{floor}/{floorCount}층";
        _bossTag.SetActive(isBoss);
    }

    // 하트 maxLives개를 하트 영역에 같은 너비로 나눠 놓는다 (많으면 작아짐), 남은 목숨은 꽉 찬 하트·잃은 목숨은 빈 하트
    public void SetLives(int lives, int maxLives)
    {
        int count = Mathf.Clamp(maxLives, 1, _hearts.Length);
        for (int i = 0; i < _hearts.Length; i++)
        {
            bool shown = i < count;
            _hearts[i].gameObject.SetActive(shown);
            if (!shown)
            {
                continue;
            }
            RectTransform rect = _hearts[i].rectTransform;
            rect.anchorMin = new Vector2((float)i / count, rect.anchorMin.y);
            rect.anchorMax = new Vector2((float)(i + 1) / count, rect.anchorMax.y);
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
