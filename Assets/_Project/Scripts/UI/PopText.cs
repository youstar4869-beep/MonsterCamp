using UnityEngine;
using UnityEngine.UI;

// 큰 글자가 튀어나오는 알림 (승리, 패배, 보스 등장): 작게 시작해 조금 더 커졌다가 제 크기로.
// holdSeconds가 있으면 그만큼 보인 뒤 흐려지며 사라진다. 움직이는 동안에만 Update가 켜진다 (씬에는 꺼진 채로 저장)
public class PopText : MonoBehaviour
{
    [SerializeField] private Text _text;
    [SerializeField] private float _popDuration = 0.35f;
    [Tooltip("가장 클 때 배율")]
    [SerializeField] private float _overshoot = 1.18f;
    [SerializeField] private float _fadeDuration = 0.3f;

    private float _time;
    // 흐려지기 시작하는 시간 (음수면 계속 보임)
    private float _hideAt;
    private Color _color;

    public string Message => _text.text;

    public void Pop(string message, Color color, float holdSeconds = 0f)
    {
        _text.text = message;
        _color = color;
        _text.color = color;
        _time = 0f;
        _hideAt = holdSeconds > 0f ? _popDuration + holdSeconds : -1f;
        transform.localScale = Vector3.one * 0.3f;
        enabled = true;
    }

    public void Clear()
    {
        _text.text = "";
        transform.localScale = Vector3.one;
        enabled = false;
    }

    private void Update()
    {
        _time += Time.deltaTime;

        // 0.3배 → overshoot → 1배
        float t = Mathf.Clamp01(_time / _popDuration);
        float scale = t < 0.6f
            ? Mathf.Lerp(0.3f, _overshoot, t / 0.6f)
            : Mathf.Lerp(_overshoot, 1f, (t - 0.6f) / 0.4f);
        transform.localScale = Vector3.one * scale;

        if (_hideAt < 0f)
        {
            if (t >= 1f)
            {
                enabled = false;
            }
            return;
        }

        float fade = (_time - _hideAt) / _fadeDuration;
        if (fade >= 1f)
        {
            Clear();
            return;
        }
        Color color = _color;
        color.a = fade > 0f ? 1f - fade : 1f;
        _text.color = color;
    }
}
