using UnityEngine;
using UnityEngine.EventSystems;

public enum SlotArea
{
    Bench,
    Team
}

// 대기석·팀 칸에 붙여서 손가락으로 끌어 옮기기
[RequireComponent(typeof(CreatureSlotView))]
public class SlotDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private ShopScreen _shop;
    [SerializeField] private SlotArea _area;
    [SerializeField] private int _index;

    private CreatureSlotView _view;
    private bool _dragging;

    public SlotArea Area => _area;
    public int Index => _index;

    private void Awake()
    {
        _view = GetComponent<CreatureSlotView>();
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

        _view.SetFaded(true);
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
        _view.SetFaded(false);
        _shop.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        _shop.DropOn(this);
    }
}
