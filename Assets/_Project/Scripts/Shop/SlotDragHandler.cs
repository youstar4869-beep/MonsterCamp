using UnityEngine;
using UnityEngine.EventSystems;

public enum SlotArea
{
    Bench,
    Team,
    // 아이템 보관함
    Items
}

// 대기석·팀·아이템 보관함 칸에 붙여서 손가락으로 끌어 옮기기
[RequireComponent(typeof(CanvasGroup))]
public class SlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    private const float FadedAlpha = 0.4f;

    [SerializeField] private ShopScreen _shop;
    [SerializeField] private SlotArea _area;
    [SerializeField] private int _index;

    private CanvasGroup _canvasGroup;
    private bool _dragging;

    public SlotArea Area => _area;
    public int Index => _index;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _dragging = _shop.BeginDrag(this);
        if (!_dragging)
        {
            // 빈 칸이거나 다른 손가락이 이미 끌고 있음
            eventData.pointerDrag = null;
            return;
        }

        // 끌고 있는 동안 원래 칸을 흐리게
        _canvasGroup.alpha = FadedAlpha;
        _shop.MoveDrag(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_dragging)
        {
            _shop.MoveDrag(eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        _canvasGroup.alpha = 1f;
        _shop.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        _shop.DropOn(this);
    }
}
