using UnityEngine;

// 노치·둥근 모서리 폰에서 UI가 가려지지 않게, 이 RectTransform을 화면의 안전 영역(Screen.safeArea)에 맞춘다.
// 모든 화면을 이 안에 넣는다. Update 없이 켜질 때와 크기가 바뀔 때(회전 등)만 다시 계산한다
[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    private RectTransform _rect;
    private Rect _appliedArea;
    private Vector2 _appliedScreen;

    private void OnEnable()
    {
        ApplyScreenSafeArea();
    }

    // 캔버스 크기가 바뀌면(화면 회전, 해상도 변경) 불린다
    private void OnRectTransformDimensionsChange()
    {
        if (isActiveAndEnabled)
        {
            ApplyScreenSafeArea();
        }
    }

    private void ApplyScreenSafeArea()
    {
        Apply(Screen.safeArea, new Vector2(Screen.width, Screen.height));
    }

    // safeArea: 화면 픽셀 기준 안전 영역, screenSize: 화면 크기 (같은 값이면 다시 계산하지 않는다)
    public void Apply(Rect safeArea, Vector2 screenSize)
    {
        if (screenSize.x <= 0f || screenSize.y <= 0f || (safeArea == _appliedArea && screenSize == _appliedScreen))
        {
            return;
        }
        _appliedArea = safeArea;
        _appliedScreen = screenSize;

        _rect ??= (RectTransform)transform;
        _rect.anchorMin = new Vector2(safeArea.xMin / screenSize.x, safeArea.yMin / screenSize.y);
        _rect.anchorMax = new Vector2(safeArea.xMax / screenSize.x, safeArea.yMax / screenSize.y);
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
    }
}
