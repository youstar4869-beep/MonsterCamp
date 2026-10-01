using UnityEngine;
using UnityEngine.UI;

// 상점·배치 화면: 골드, 상점 칸, 새로고침, 대기석, 판매
public class ShopScreen : MonoBehaviour
{
    [SerializeField] private ShopConfig _config;
    [SerializeField] private Text _goldText;
    [SerializeField] private Text _messageText;
    [SerializeField] private Text _refreshLabel;
    [SerializeField] private CreatureSlotView[] _shopSlots;
    [SerializeField] private CreatureSlotView[] _benchSlots;

    private CreatureData[] _offers;
    // 칸 위치를 유지하기 위해 빈 칸은 null
    private CreatureData[] _bench;
    private int _gold;

    // 트레이너 선택 → 상점으로 넘어갈 때 호출 (새 런 시작)
    public void StartRun()
    {
        _offers = new CreatureData[_shopSlots.Length];
        _bench = new CreatureData[_benchSlots.Length];
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
        if (_bench[index] == null)
        {
            return;
        }

        _bench[index] = null;
        _gold += _config.SellPrice;
        ShowMessage("");
        UpdateView();
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
    }
}
