using UnityEngine;
using UnityEngine.UI;

// 화면이 바뀔 때 짧은 페이드: 화면은 바로 바뀌고, 그 위를 덮은 어두운 판이 잠깐 사이에 투명해진다.
// 진행 중에만 켜져 있다 (끝나면 꺼져서 그리지도, Update도 하지 않는다). 누르는 것은 막지 않는다
public class ScreenFader : MonoBehaviour
{
    [SerializeField] private Image _image;
    [Tooltip("처음 진하기 (0~1)")]
    [SerializeField] private float _startAlpha = 0.7f;
    [SerializeField] private float _duration = 0.25f;

    private float _time;

    public void Play()
    {
        _time = 0f;
        SetAlpha(_startAlpha);
        gameObject.SetActive(true);
    }

    private void Update()
    {
        _time += Time.unscaledDeltaTime;
        float t = _time / _duration;
        if (t >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }
        SetAlpha(_startAlpha * (1f - t) * (1f - t));
    }

    private void SetAlpha(float alpha)
    {
        Color color = _image.color;
        color.a = alpha;
        _image.color = color;
    }
}
