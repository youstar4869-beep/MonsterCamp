using UnityEngine;
using UnityEngine.UI;

// 상점·배치 화면: 골드, 상점 칸, 새로고침, 대기석, 판매, 팀 배치(끌어서 옮기기)
public class ShopScreen : MonoBehaviour
{
    [SerializeField] private ShopConfig _config;
    [SerializeField] private Text _goldText;
    [SerializeField] private Text _messageText;
    [SerializeField] private Text _refreshLabel;
    [SerializeField] private CreatureSlotView[] _shopSlots;
    [SerializeField] private CreatureSlotView[] _benchSlots;
    [Tooltip("0~1 앞줄, 2~4 뒷줄")]
    [SerializeField] private CreatureSlotView[] _teamSlots;
    [Tooltip("끄는 동안 손가락을 따라다니는 그림")]
    [SerializeField] private Image _dragIcon;

    private CreatureData[] _offers;
    // 칸 위치를 유지하기 위해 빈 칸은 null
    private CreatureData[] _bench;
    private CreatureData[] _team;
    private int _gold;
    private SlotDragHandler _dragSource;

    // 트레이너 선택 → 상점으로 넘어갈 때 호출 (새 런 시작)
    public void StartRun()
    {
        _offers = new CreatureData[_shopSlots.Length];
        _bench = new CreatureData[_benchSlots.Length];
        _team = new CreatureData[_teamSlots.Length];
        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
        _gold = _config.StartGold;
        RollOffers();
        ShowMessage("");
        UpdateView();
    }

    public void Refresh()
    {
        if (_gold < _config.RefreshCost)
        {
            ShowMessage("골드가 부족해요");
            return;
        }

        _gold -= _config.RefreshCost;
        RollOffers();
        ShowMessage("");
        UpdateView();
    }

    public void BuyAt(int index)
    {
        CreatureData creature = _offers[index];
        if (creature == null)
        {
            return;
        }

        int benchIndex = System.Array.IndexOf(_bench, null);
        if (benchIndex < 0)
        {
            ShowMessage("대기석이 꽉 찼어요");
            return;
        }

        int price = _config.GetPrice(creature);
        if (_gold < price)
        {
            ShowMessage("골드가 부족해요");
            return;
        }

        _gold -= price;
        _bench[benchIndex] = creature;
        _offers[index] = null;
        ShowMessage("");
        UpdateView();
    }

    public void SellAt(int index)
    {
        // 끌어서 같은 칸에 다시 놓은 경우에도 클릭이 들어오므로 무시
        if (_dragSource != null || _bench[index] == null)
        {
            return;
        }

        _bench[index] = null;
        _gold += _config.SellPrice;
        ShowMessage("");
        UpdateView();
    }

    // 끌기 시작. 빈 칸이거나 이미 다른 칸을 끌고 있으면 false
    public bool BeginDrag(SlotDragHandler source)
    {
        if (_dragSource != null || GetSlots(source.Area)[source.Index] == null)
        {
            return false;
        }

        _dragSource = source;
        _dragIcon.sprite = GetSlots(source.Area)[source.Index].Sprite;
        _dragIcon.gameObject.SetActive(true);
        return true;
    }

    public void MoveDrag(Vector2 screenPosition)
    {
        _dragIcon.transform.position = screenPosition;
    }

    // 놓은 칸이 비어 있으면 옮기고, 크리처가 있으면 자리를 바꾼다
    public void DropOn(SlotDragHandler target)
    {
        if (_dragSource == null)
        {
            return;
        }

        CreatureData[] from = GetSlots(_dragSource.Area);
        CreatureData[] to = GetSlots(target.Area);
        (from[_dragSource.Index], to[target.Index]) = (to[target.Index], from[_dragSource.Index]);
        ShowMessage("");
        UpdateView();
    }

    public void EndDrag()
    {
        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
    }

    private CreatureData[] GetSlots(SlotArea area)
    {
        return area == SlotArea.Team ? _team : _bench;
    }

    private void RollOffers()
    {
        for (int i = 0; i < _offers.Length; i++)
        {
            _offers[i] = _config.GetRandomCreature();
        }
    }

    private void ShowMessage(string message)
    {
        _messageText.text = message;
    }

    private void UpdateView()
    {
        _goldText.text = $"골드 {_gold}";
        _refreshLabel.text = $"새로고침 ({_config.RefreshCost}G)";

        for (int i = 0; i < _shopSlots.Length; i++)
        {
            if (_offers[i] != null)
            {
                _shopSlots[i].Show(_offers[i], $"{_config.GetPrice(_offers[i])}G");
            }
            else
            {
                _shopSlots[i].Clear();
            }
        }

        for (int i = 0; i < _benchSlots.Length; i++)
        {
            if (_bench[i] != null)
            {
                _benchSlots[i].Show(_bench[i], $"판매 +{_config.SellPrice}G");
            }
            else
            {
                _benchSlots[i].Clear();
            }
        }

        for (int i = 0; i < _teamSlots.Length; i++)
        {
            if (_team[i] != null)
            {
                _teamSlots[i].Show(_team[i], "");
            }
            else
            {
                _teamSlots[i].Clear();
            }
        }
    }
}
