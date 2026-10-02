using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 상점·배치 화면: 골드, 상점 칸(크리처·아이템), 새로고침, 대기석, 판매, 팀 배치(끌어서 옮기기), 합성,
// 아이템 보관함, 아이템 장착(보관함 → 크리처로 끌기)
public class ShopScreen : MonoBehaviour
{
    private const int MergeCount = 3;

    [SerializeField] private ShopConfig _config;
    [SerializeField] private SynergyConfig _synergy;
    [SerializeField] private RunManager _run;
    [Tooltip("지금 켜진 타입 시너지 (예: 불 2/4)")]
    [SerializeField] private Text _synergyText;
    [Tooltip("상단 표시에 있는 골드 글자")]
    [SerializeField] private Text _goldText;
    [SerializeField] private Text _messageText;
    [SerializeField] private Text _refreshLabel;
    [SerializeField] private CreatureSlotView[] _shopSlots;
    [SerializeField] private ItemSlotView _shopItemSlot;
    [SerializeField] private CreatureSlotView[] _benchSlots;
    [Tooltip("0~1 앞줄, 2~4 뒷줄")]
    [SerializeField] private CreatureSlotView[] _teamSlots;
    [SerializeField] private ItemSlotView[] _itemSlots;
    [Tooltip("끄는 동안 손가락을 따라다니는 그림")]
    [SerializeField] private Image _dragIcon;
    [Tooltip("아이템을 끌 때 그림 위에 보이는 글자")]
    [SerializeField] private Text _dragLabel;

    private CreatureData[] _offers;
    private ItemData _itemOffer;
    // 칸 위치를 유지하기 위해 빈 칸은 null
    private CreatureInstance[] _bench;
    private CreatureInstance[] _team;
    private ItemData[] _items;
    private int _gold;
    private SlotDragHandler _dragSource;

    // 합성 후보를 모을 때 재사용 (팀 칸 먼저, 그다음 대기석)
    private readonly List<(SlotArea area, int index)> _mergeCandidates = new List<(SlotArea, int)>();
    private readonly List<ItemData> _returnedItems = new List<ItemData>();
    // 시너지 표시용 (팀 칸만)
    private readonly List<CreatureData> _teamCreatures = new List<CreatureData>();
    private readonly int[] _typeCounts = new int[TypeSynergy.TypeCount];
    private readonly System.Text.StringBuilder _synergyBuilder = new System.Text.StringBuilder();

    // 새 런 시작: 대기석·팀·아이템 보관함 비우기
    public void ResetRun()
    {
        _offers = new CreatureData[_shopSlots.Length];
        _bench = new CreatureInstance[_benchSlots.Length];
        _team = new CreatureInstance[_teamSlots.Length];
        _items = new ItemData[_itemSlots.Length];
    }

    // 라운드 시작: 골드를 다시 채우고 상점을 새로 뽑는다. 대기석·팀·보관함은 그대로
    // bonusGold: 트레이너 능력, 행운 동전 등으로 더 받는 골드
    public void StartRound(int bonusGold)
    {
        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
        _gold = _config.RoundGold + bonusGold;
        RollOffers();
        ShowMessage("");
        UpdateView();
    }

    // ---------- 저장 / 불러오기 ----------

    // 지금 상점 단계 상태를 저장 데이터에 쓴다 (골드, 팀, 대기석, 보관함, 상점 목록)
    public void WriteSave(RunSaveData data)
    {
        data.gold = _gold;
        data.team = ToSave(_team);
        data.bench = ToSave(_bench);
        data.items = new string[_items.Length];
        for (int i = 0; i < _items.Length; i++)
        {
            data.items[i] = IdOf(_items[i]);
        }
        data.shopCreatures = new string[_offers.Length];
        for (int i = 0; i < _offers.Length; i++)
        {
            data.shopCreatures[i] = IdOf(_offers[i]);
        }
        data.shopItem = IdOf(_itemOffer);
    }

    // 저장 데이터로 상점 단계를 되살린다. 찾을 수 없는 크리처·아이템은 빈 칸으로 둔다
    public void LoadSave(RunSaveData data)
    {
        ResetRun();
        FromSave(data.team, _team);
        FromSave(data.bench, _bench);
        for (int i = 0; i < _items.Length && data.items != null && i < data.items.Length; i++)
        {
            _items[i] = _config.FindItem(data.items[i]);
        }
        for (int i = 0; i < _offers.Length && data.shopCreatures != null && i < data.shopCreatures.Length; i++)
        {
            _offers[i] = _config.FindCreature(data.shopCreatures[i]);
        }
        _itemOffer = _config.FindItem(data.shopItem);
        _gold = Mathf.Max(0, data.gold);

        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
        ShowMessage("");
        UpdateView();
    }

    private static CreatureSaveData[] ToSave(CreatureInstance[] slots)
    {
        var result = new CreatureSaveData[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            CreatureInstance creature = slots[i];
            result[i] = new CreatureSaveData
            {
                creature = IdOf(creature?.Data),
                level = creature?.Level ?? 0,
                item = IdOf(creature?.Item)
            };
        }
        return result;
    }

    private void FromSave(CreatureSaveData[] saved, CreatureInstance[] slots)
    {
        for (int i = 0; i < slots.Length && saved != null && i < saved.Length; i++)
        {
            CreatureData data = _config.FindCreature(saved[i]?.creature);
            if (data == null)
            {
                continue;
            }
            int level = Mathf.Clamp(saved[i].level, 1, CreatureData.MaxLevel);
            slots[i] = new CreatureInstance(data, level) { Item = _config.FindItem(saved[i].item) };
        }
    }

    private static string IdOf(Object asset)
    {
        return asset != null ? asset.name : "";
    }

    // 크리처 칸과 아이템 칸을 같이 새로 뽑는다
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

        // 같은 Lv1이 이미 2마리 있으면 사자마자 합쳐지므로 대기석 자리가 필요 없다
        CollectSame(creature, 1);
        bool mergesNow = _mergeCandidates.Count >= MergeCount - 1;

        int benchIndex = System.Array.IndexOf(_bench, null);
        if (benchIndex < 0 && !mergesNow)
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
        _offers[index] = null;
        ShowMessage("");

        if (mergesNow)
        {
            // 산 크리처를 대기석에 두지 않고 가진 2마리와 바로 합친다
            MergeCandidates(MergeCount - 1);
        }
        else
        {
            _bench[benchIndex] = new CreatureInstance(creature);
            UpdateView();
        }
        MergeAll();
    }

    // 상점 아이템 칸 버튼: 아이템 보관함 빈 칸으로
    public void BuyItem()
    {
        if (_itemOffer == null)
        {
            return;
        }

        int itemIndex = System.Array.IndexOf(_items, null);
        if (itemIndex < 0)
        {
            ShowMessage("아이템 보관함이 꽉 찼어요");
            return;
        }
        if (_gold < _itemOffer.Price)
        {
            ShowMessage("골드가 부족해요");
            return;
        }

        _gold -= _itemOffer.Price;
        _items[itemIndex] = _itemOffer;
        _itemOffer = null;
        ShowMessage("");
        UpdateView();
    }

    public void SellAt(int index)
    {
        // 끌어서 같은 칸에 다시 놓은 경우에도 클릭이 들어오므로 무시
        CreatureInstance creature = _bench[index];
        if (_dragSource != null || creature == null)
        {
            return;
        }

        // 장착한 아이템은 보관함으로. 보관함이 꽉 차면 팔지 않는다
        if (creature.Item != null)
        {
            if (!TryStoreItem(creature.Item))
            {
                ShowMessage("아이템 보관함이 꽉 차서 팔 수 없어요");
                return;
            }
            creature.Item = null;
        }

        _gold += _config.GetSellPrice(creature);
        _bench[index] = null;
        ShowMessage("");
        UpdateView();
    }

    // "전투 시작" 버튼
    public void StartBattle()
    {
        if (_dragSource != null)
        {
            return;
        }
        if (System.Array.TrueForAll(_team, creature => creature == null))
        {
            ShowMessage("팀 칸에 크리처를 놓아 주세요");
            return;
        }

        ShowMessage("");
        _run.BeginBattle(_team);
    }

    // 끌기 시작. 빈 칸이거나 이미 다른 칸을 끌고 있으면 false
    public bool BeginDrag(SlotDragHandler source)
    {
        if (_dragSource != null)
        {
            return false;
        }

        if (source.Area == SlotArea.Items)
        {
            ItemData item = _items[source.Index];
            if (item == null)
            {
                return false;
            }
            _dragIcon.sprite = null;
            _dragIcon.color = item.IconColor;
            _dragLabel.text = item.IconLabel;
        }
        else
        {
            CreatureInstance creature = GetSlots(source.Area)[source.Index];
            if (creature == null)
            {
                return false;
            }
            _dragIcon.sprite = creature.Data.Sprite;
            _dragIcon.color = Color.white;
            _dragLabel.text = "";
        }

        _dragSource = source;
        _dragIcon.gameObject.SetActive(true);
        return true;
    }

    public void MoveDrag(Vector2 screenPosition)
    {
        _dragIcon.transform.position = screenPosition;
    }

    public void DropOn(SlotDragHandler target)
    {
        if (_dragSource == null)
        {
            return;
        }

        if (_dragSource.Area == SlotArea.Items)
        {
            DropItem(_dragSource.Index, target);
        }
        else if (target.Area != SlotArea.Items)
        {
            // 크리처: 놓은 칸이 비어 있으면 옮기고, 크리처가 있으면 자리를 바꾼다
            CreatureInstance[] from = GetSlots(_dragSource.Area);
            CreatureInstance[] to = GetSlots(target.Area);
            (from[_dragSource.Index], to[target.Index]) = (to[target.Index], from[_dragSource.Index]);
            ShowMessage("");
        }
        // 크리처를 보관함에 놓으면 아무 일도 없다

        UpdateView();
    }

    public void EndDrag()
    {
        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
    }

    // 보관함의 아이템을 크리처에 놓으면 장착, 보관함 칸에 놓으면 자리 바꾸기
    private void DropItem(int itemIndex, SlotDragHandler target)
    {
        if (target.Area == SlotArea.Items)
        {
            (_items[itemIndex], _items[target.Index]) = (_items[target.Index], _items[itemIndex]);
            ShowMessage("");
            return;
        }

        CreatureInstance creature = GetSlots(target.Area)[target.Index];
        if (creature == null)
        {
            return;
        }

        // 원래 끼고 있던 아이템은 방금 비운 보관함 칸으로 돌아간다
        // (아이템을 보관함에서 꺼내 오므로 자리는 항상 있다)
        ItemData previous = creature.Item;
        creature.Item = _items[itemIndex];
        _items[itemIndex] = previous;
        ShowMessage("");
    }

    private bool TryStoreItem(ItemData item)
    {
        int index = System.Array.IndexOf(_items, null);
        if (index < 0)
        {
            return false;
        }
        _items[index] = item;
        return true;
    }

    // 같은 크리처·같은 레벨이 3마리 모이면 1마리로 합치고 레벨 +1.
    // Lv2가 새로 3마리가 될 수 있으므로 더 합칠 게 없을 때까지 반복한다.
    private void MergeAll()
    {
        while (TryMergeOnce())
        {
        }
    }

    private bool TryMergeOnce()
    {
        foreach (CreatureInstance owned in EnumerateOwned())
        {
            if (owned.Level >= CreatureData.MaxLevel)
            {
                continue;
            }

            CollectSame(owned.Data, owned.Level);
            if (_mergeCandidates.Count < MergeCount)
            {
                continue;
            }

            MergeCandidates(MergeCount);
            return true;
        }
        return false;
    }

    // _mergeCandidates 앞에서 count마리를 1마리로 합친다.
    // 첫 번째 후보를 남긴다. 팀 칸부터 모았으므로 팀 칸에 있던 자리가 우선
    private void MergeCandidates(int count)
    {
        (SlotArea keepArea, int keepIndex) = _mergeCandidates[0];
        CreatureInstance kept = GetSlots(keepArea)[keepIndex];

        // 장착 아이템은 보관함으로 (사라지는 크리처 것 먼저, 남는 크리처 것은 마지막)
        _returnedItems.Clear();
        for (int i = 1; i < count; i++)
        {
            (SlotArea area, int index) = _mergeCandidates[i];
            CreatureInstance merged = GetSlots(area)[index];
            if (merged.Item != null)
            {
                _returnedItems.Add(merged.Item);
            }
            GetSlots(area)[index] = null;
        }
        if (kept.Item != null)
        {
            _returnedItems.Add(kept.Item);
        }

        kept.Level++;
        kept.Item = null;
        int lost = 0;
        foreach (ItemData item in _returnedItems)
        {
            if (TryStoreItem(item))
            {
                continue;
            }
            // 보관함이 꽉 차면 합성된 크리처에 그대로 남긴다 (1개까지)
            if (kept.Item == null)
            {
                kept.Item = item;
            }
            else
            {
                lost++;
            }
        }
        if (lost > 0)
        {
            ShowMessage($"아이템 보관함이 꽉 차서 아이템 {lost}개가 사라졌어요");
        }

        UpdateView();
        GetSlotViews(keepArea)[keepIndex].Flash();
    }

    private void CollectSame(CreatureData data, int level)
    {
        _mergeCandidates.Clear();
        AddSame(SlotArea.Team, data, level);
        AddSame(SlotArea.Bench, data, level);
    }

    private void AddSame(SlotArea area, CreatureData data, int level)
    {
        CreatureInstance[] slots = GetSlots(area);
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].Data == data && slots[i].Level == level)
            {
                _mergeCandidates.Add((area, i));
            }
        }
    }

    private IEnumerable<CreatureInstance> EnumerateOwned()
    {
        foreach (CreatureInstance creature in _team)
        {
            if (creature != null)
            {
                yield return creature;
            }
        }
        foreach (CreatureInstance creature in _bench)
        {
            if (creature != null)
            {
                yield return creature;
            }
        }
    }

    private CreatureInstance[] GetSlots(SlotArea area)
    {
        return area == SlotArea.Team ? _team : _bench;
    }

    private CreatureSlotView[] GetSlotViews(SlotArea area)
    {
        return area == SlotArea.Team ? _teamSlots : _benchSlots;
    }

    private void RollOffers()
    {
        for (int i = 0; i < _offers.Length; i++)
        {
            _offers[i] = _config.GetRandomCreature();
        }
        _itemOffer = _config.GetRandomItem();
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
                _shopSlots[i].Show(_offers[i], 0, null, $"{_config.GetPrice(_offers[i])}G");
            }
            else
            {
                _shopSlots[i].Clear();
            }
        }

        if (_itemOffer != null)
        {
            _shopItemSlot.Show(_itemOffer, $"{_itemOffer.Price}G");
        }
        else
        {
            _shopItemSlot.Clear();
        }

        for (int i = 0; i < _benchSlots.Length; i++)
        {
            CreatureInstance creature = _bench[i];
            if (creature != null)
            {
                _benchSlots[i].Show(creature.Data, creature.Level, creature.Item, $"판매 +{_config.GetSellPrice(creature)}G");
            }
            else
            {
                _benchSlots[i].Clear();
            }
        }

        for (int i = 0; i < _teamSlots.Length; i++)
        {
            CreatureInstance creature = _team[i];
            if (creature != null)
            {
                _teamSlots[i].Show(creature.Data, creature.Level, creature.Item, "");
            }
            else
            {
                _teamSlots[i].Clear();
            }
        }

        for (int i = 0; i < _itemSlots.Length; i++)
        {
            if (_items[i] != null)
            {
                _itemSlots[i].Show(_items[i], "");
            }
            else
            {
                _itemSlots[i].Clear();
            }
        }

        UpdateSynergyText();
    }

    // 팀 칸 크리처만 세서 켜진 시너지를 "불 2/4 · 물 3/4" 형태로 표시
    private void UpdateSynergyText()
    {
        _teamCreatures.Clear();
        foreach (CreatureInstance creature in _team)
        {
            _teamCreatures.Add(creature?.Data);
        }
        TypeSynergy.CountUnique(_teamCreatures, _typeCounts);

        _synergyBuilder.Clear();
        for (int type = 0; type < _typeCounts.Length; type++)
        {
            int count = _typeCounts[type];
            if (_synergy.GetTier(count) == 0)
            {
                continue;
            }
            if (_synergyBuilder.Length > 0)
            {
                _synergyBuilder.Append(" · ");
            }
            _synergyBuilder.Append(TypeSynergy.GetTypeName((CreatureType)type))
                .Append(' ').Append(count).Append('/').Append(_synergy.GetNextThreshold(count));
        }

        _synergyText.text = _synergyBuilder.Length > 0 ? "시너지  " + _synergyBuilder : "";
    }
}
