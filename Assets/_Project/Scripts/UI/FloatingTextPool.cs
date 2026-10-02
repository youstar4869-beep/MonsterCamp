using UnityEngine;
using UnityEngine.UI;

// 크리처 위에 잠깐 떠올랐다 사라지는 짧은 글 (스킬 발동: "기절", "화상" 등, 데미지·회복 숫자).
// 글 오브젝트를 미리 만들어 두고 돌려 쓴다 (전투 중 새로 만들지 않음)
public class FloatingTextPool : MonoBehaviour
{
    [Tooltip("미리 만들어 둔 글 (처음엔 숨김)")]
    [SerializeField] private Text[] _texts;
    [SerializeField] private float _duration = 0.9f;
    [Tooltip("떠오르는 높이 (1080x1920 기준 픽셀)")]
    [SerializeField] private float _riseHeight = 90f;

    private float[] _startTimes;
    private Vector3[] _startPositions;
    private Color[] _colors;
    private float[] _scales;
    // 띄운 곳 (흩뜨리기 전, 쌓기 판단용)
    private Vector3[] _origins;
    private int _next;
    // 떠 있는 글 수 (0이면 Update에서 아무것도 안 함)
    private int _activeCount;

    private void Awake()
    {
        _startTimes = new float[_texts.Length];
        _startPositions = new Vector3[_texts.Length];
        _colors = new Color[_texts.Length];
        _scales = new float[_texts.Length];
        _origins = new Vector3[_texts.Length];
    }

    // worldPosition: 글이 시작할 위치 (캔버스 월드 좌표)
    public void Show(string message, Vector3 worldPosition, Color color)
    {
        Show(message, worldPosition, color, 1f);
    }

    // scale: 글 크기 배율 (상성 유리 데미지는 크게, 불리는 작게). 처음에 조금 더 크게 튀었다가 이 크기로
    public void Show(string message, Vector3 worldPosition, Color color, float scale)
    {
        if (_startTimes == null)
        {
            Awake();
        }

        // 쉬고 있는 글을 찾고, 없으면 가장 오래된 글을 다시 쓴다
        int index = -1;
        for (int i = 0; i < _texts.Length; i++)
        {
            int candidate = (_next + i) % _texts.Length;
            if (!_texts[candidate].gameObject.activeSelf)
            {
                index = candidate;
                break;
            }
        }
        if (index < 0)
        {
            index = _next;
        }
        else
        {
            _activeCount++;
        }
        _next = (index + 1) % _texts.Length;

        Text text = _texts[index];
        text.text = message;
        text.color = color;
        _colors[index] = color;
        _scales[index] = scale;
        _startTimes[index] = Time.time;
        // 같은 칸에 여러 글이 겹치지 않도록 옆으로 조금 흩뜨리고, 방금 뜬 글이 근처에 있으면 그만큼 위로 쌓는다
        float pixel = text.canvas.rootCanvas.transform.lossyScale.x;
        int stacked = CountRecentNear(index, worldPosition, pixel);
        _origins[index] = worldPosition;
        _startPositions[index] = worldPosition
                                 + new Vector3(Random.Range(-45f, 45f), Random.Range(-10f, 10f) + stacked * StackHeight, 0f) * pixel;
        text.transform.position = _startPositions[index];
        text.transform.localScale = Vector3.one * (scale * PopScale);
        text.gameObject.SetActive(true);
    }

    // 0.25초 안에 같은 곳(StackRange 픽셀 안)에서 뜬 글 수
    private int CountRecentNear(int except, Vector3 origin, float pixel)
    {
        int count = 0;
        float range = StackRange * pixel;
        for (int i = 0; i < _texts.Length; i++)
        {
            if (i != except && _texts[i].gameObject.activeSelf && Time.time - _startTimes[i] < StackWindow
                && (_origins[i] - origin).sqrMagnitude < range * range)
            {
                count++;
            }
        }
        return Mathf.Min(count, MaxStack);
    }

    private const float StackWindow = 0.25f;
    private const float StackRange = 60f;
    private const float StackHeight = 40f;
    private const int MaxStack = 3;

    // 처음 크기 (곧 1배로 줄어든다)
    private const float PopScale = 1.3f;
    private const float PopTime = 0.12f;

    public void HideAll()
    {
        foreach (Text text in _texts)
        {
            text.gameObject.SetActive(false);
        }
        _activeCount = 0;
    }

    // 떠 있는 글이 있을 때만 움직인다
    private void Update()
    {
        if (_activeCount == 0)
        {
            return;
        }
        for (int i = 0; i < _texts.Length; i++)
        {
            Text text = _texts[i];
            if (!text.gameObject.activeSelf)
            {
                continue;
            }

            float elapsed = Time.time - _startTimes[i];
            float t = elapsed / _duration;
            if (t >= 1f)
            {
                text.gameObject.SetActive(false);
                _activeCount--;
                continue;
            }
            float pop = elapsed < PopTime ? Mathf.Lerp(PopScale, 1f, elapsed / PopTime) : 1f;
            text.transform.localScale = Vector3.one * (_scales[i] * pop);
            text.transform.position = _startPositions[i] + Vector3.up * (t * _riseHeight * text.canvas.rootCanvas.transform.lossyScale.x);
            Color color = _colors[i];
            color.a = 1f - t * t;
            text.color = color;
        }
    }
}
