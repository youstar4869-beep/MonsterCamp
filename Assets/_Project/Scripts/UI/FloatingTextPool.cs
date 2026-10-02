using UnityEngine;
using UnityEngine.UI;

// 크리처 위에 잠깐 떠올랐다 사라지는 짧은 글 (스킬 발동: "기절", "화상" 등).
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
    private int _next;

    private void Awake()
    {
        _startTimes = new float[_texts.Length];
        _startPositions = new Vector3[_texts.Length];
        _colors = new Color[_texts.Length];
    }

    // worldPosition: 글이 시작할 위치 (캔버스 월드 좌표)
    public void Show(string message, Vector3 worldPosition, Color color)
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
        _next = (index + 1) % _texts.Length;

        Text text = _texts[index];
        text.text = message;
        text.color = color;
        _colors[index] = color;
        _startTimes[index] = Time.time;
        // 같은 칸에 여러 글이 겹치지 않도록 옆으로 조금 흩뜨린다
        _startPositions[index] = worldPosition + new Vector3(Random.Range(-25f, 25f), 0f, 0f) * text.canvas.scaleFactor;
        text.transform.position = _startPositions[index];
        text.gameObject.SetActive(true);
    }

    public void HideAll()
    {
        foreach (Text text in _texts)
        {
            text.gameObject.SetActive(false);
        }
    }

    // 떠 있는 글이 있을 때만 움직인다
    private void Update()
    {
        for (int i = 0; i < _texts.Length; i++)
        {
            Text text = _texts[i];
            if (!text.gameObject.activeSelf)
            {
                continue;
            }

            float t = (Time.time - _startTimes[i]) / _duration;
            if (t >= 1f)
            {
                text.gameObject.SetActive(false);
                continue;
            }
            text.transform.position = _startPositions[i] + Vector3.up * (t * _riseHeight * text.canvas.scaleFactor);
            Color color = _colors[i];
            color.a = 1f - t * t;
            text.color = color;
        }
    }
}
