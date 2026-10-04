using UnityEngine;
using UnityEngine.UI;

// 전투 하늘의 구름 한 장을 아주 느리게 옆으로 흘린다. 그림을 옮기지 않고 RawImage의 UV만 밀어서 좌우가 이어지게 반복
// (그림 Wrap Mode U = Repeat). 전투 화면이 꺼져 있으면 Update도 돌지 않는다
[RequireComponent(typeof(RawImage))]
public class CloudScroller : MonoBehaviour
{
    [Tooltip("1초에 그림 폭의 몇 배만큼 흐를지 (0.01이면 100초에 한 바퀴)")]
    [SerializeField] private float _speed = 0.01f;

    private RawImage _image;

    private void Awake()
    {
        _image = GetComponent<RawImage>();
    }

    private void Update()
    {
        Rect uv = _image.uvRect;
        uv.x = Mathf.Repeat(uv.x + _speed * Time.deltaTime, 1f);
        _image.uvRect = uv;
    }
}
