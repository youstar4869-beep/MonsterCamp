using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 상점·배치 화면: 골드, 상점 칸(크리처·아이템), 새로고침, 대기석, 판매, 팀 배치(끌어서 옮기기), 합성,
// 아이템 보관함, 아이템 장착(보관함 → 크리처로 끌기), 조합(보통 클리어 후)
public class ShopScreen : MonoBehaviour
{
    private const int MergeCount = 3;
    // 층 시작 때 들어온 골드를 보여 주는 시간 (초)
    private const float IncomeShowSeconds = 2.5f;
    private const string GoldHelp = "남은 골드는 다음 층으로 넘어가요. {0}골드마다 이자 1골드(최대 {1})";

    [SerializeField] private ShopConfig _config;
    [SerializeField] private SynergyConfig _synergy;
    [Tooltip("칸을 누르면 뜨는 크리처 정보 창 (구매·판매 버튼)")]
    [SerializeField] private CreatureInfoPopup _infoPopup;
    [Tooltip("\"시너지\" 버튼으로 여는 시너지 표")]
    [SerializeField] private SynergyPanel _synergyPanel;
    [SerializeField] private RunManager _run;
    [SerializeField] private MetaProgress _meta;
    [Tooltip("지금 켜진 타입 시너지 (예: 불 2/4)")]
    [SerializeField] private Text _synergyText;
    [Tooltip("상단 정보바 (골드 표시, 골드를 누르면 골드 규칙 안내)")]
    [SerializeField] private HudView _hud;
    [Tooltip("층 시작 때 골드 아래에 잠깐 보이는 \"+10 +이자 2\"")]
    [SerializeField] private Text _goldIncomeText;
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

    [Header("조합 (보통 클리어 후 해금, 그 전에는 안 보임)")]
    [Tooltip("시너지 줄 옆 \"조합 N\" 버튼 (만들 수 있는 조합이 없으면 흐리게)")]
    [SerializeField] private Button _fusionButton;
    [SerializeField] private Text _fusionLabel;
    [SerializeField] private FusionPanel _fusionPanel;
    [Tooltip("조합 버튼 왼쪽 \"조합식\" 버튼과 목록 창")]
    [SerializeField] private Button _fusionBookButton;
    [SerializeField] private FusionBookPanel _fusionBookPanel;
    [Tooltip("\"새 조합식 발견!\" 알림")]
    [SerializeField] private TipPopup _tip;

    private CreatureData[] _offers;
    private ItemData _itemOffer;
    // 칸 위치를 유지하기 위해 빈 칸은 null
    private CreatureInstance[] _bench;
    private CreatureInstance[] _team;
    private ItemData[] _items;
    // 층이 끝나도 남아서 다음 층으로 넘어간다 (새 런은 0)
    private int _gold;
    private Coroutine _incomeRoutine;
    // 지금 층과 난이도 (상점 등급 확률, 이자 최대, 새로고침 비용)
    private int _floor = 1;
    private DifficultyData _difficulty;
    private SlotDragHandler _dragSource;

    // 합성 후보를 모을 때 재사용 (팀 칸 먼저, 그다음 대기석)
    private readonly List<(SlotArea area, int index)> _mergeCandidates = new List<(SlotArea, int)>();
    private readonly List<ItemData> _returnedItems = new List<ItemData>();
    // 시너지 표시용 (팀 칸만)
    private readonly List<CreatureData> _teamCreatures = new List<CreatureData>();
    private readonly int[] _typeCounts = new int[TypeSynergy.TypeCount];
    private readonly System.Text.StringBuilder _synergyBuilder = new System.Text.StringBuilder();
    // 지금 만들 수 있는 조합식과 목록 글
    private readonly List<FusionRecipe> _fusionOptions = new List<FusionRecipe>();
    private readonly List<string> _fusionLabels = new List<string>();
    // 조합 버튼이 안 보일 때 시너지 줄 오른쪽 끝 (씬에 놓인 값)
    private float _synergyTextRight = -1f;

    // 새 런 시작: 대기석·팀·아이템 보관함 비우기
    public void ResetRun(DifficultyData difficulty)
    {
        _offers = new CreatureData[_shopSlots.Length];
        _bench = new CreatureInstance[_benchSlots.Length];
        _team = new CreatureInstance[_teamSlots.Length];
        _items = new ItemData[_itemSlots.Length];
        _gold = 0;
        _difficulty = difficulty;
        HideIncome();
    }

    // 층 시작: 골드를 더하고 상점을 새로 뽑는다. 대기석·팀·보관함은 그대로
    // 새 골드 = 남은 골드 + 층 골드 + bonusGold + 이자(남은 골드 기준), 최대치까지
    // floor: 지금 층 (등급 확률), bonusGold: 트레이너 능력, 행운 동전 등으로 더 받는 골드
    // presetOffers: 튜토리얼처럼 상점 목록을 정해 둘 때 (null이면 랜덤, 아이템 칸은 비움)
    // carryGold: false면 남은 골드·이자 없이 층 골드만 (튜토리얼 층)
    public void StartRound(int floor, int bonusGold, CreatureData[] presetOffers, bool carryGold)
    {
        _floor = floor;
        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
        ClosePopups();
        int leftover = carryGold ? _gold : 0;
        int interest = _config.GetInterest(leftover, _difficulty.MaxInterest);
        int total = leftover + _config.RoundGold + bonusGold + interest;
        _gold = Mathf.Min(_config.MaxGold, total);
        ShowIncome(_config.RoundGold + bonusGold, interest, total > _config.MaxGold);
        if (presetOffers != null)
        {
            for (int i = 0; i < _offers.Length; i++)
            {
                _offers[i] = i < presetOffers.Length ? presetOffers[i] : null;
            }
            _itemOffer = null;
        }
        else
        {
            RollOffers();
        }
        ShowMessage("");
        UpdateView();
    }

    // ---------- 튜토리얼용 (비출 위치, 진행 확인) ----------

    public ShopConfig Config => _config;
    public RectTransform ShopSlotRect(int index) => (RectTransform)_shopSlots[index].transform;
    public RectTransform BenchSlotRect(int index) => (RectTransform)_benchSlots[index].transform;
    public RectTransform TeamSlotRect(int index) => (RectTransform)_teamSlots[index].transform;
    public RectTransform ShopRowRect => (RectTransform)_shopSlots[0].transform.parent;
    public RectTransform BenchRowRect => (RectTransform)_benchSlots[0].transform.parent;
    public RectTransform SynergyTextRect => _synergyText.rectTransform;

    public int FindOfferIndex(CreatureData creature) => System.Array.IndexOf(_offers, creature);
    public int FindFirstBenchIndex() => System.Array.FindIndex(_bench, creature => creature != null);
    public bool HasTeamCreature => System.Array.Exists(_team, creature => creature != null);
    public bool HasOwnedCreature => HasTeamCreature || FindFirstBenchIndex() >= 0;
    public bool HasMergedCreature
    {
        get
        {
            foreach (CreatureInstance creature in EnumerateOwned())
            {
                if (creature.Level >= 2)
                {
                    return true;
                }
            }
            return false;
        }
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
    public void LoadSave(RunSaveData data, DifficultyData difficulty)
    {
        ResetRun(difficulty);
        _floor = Mathf.Max(1, data.floor);
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
        // 저장된 골드가 그대로 다음 층의 남은 골드가 된다
        _gold = Mathf.Clamp(data.gold, 0, _config.MaxGold);

        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
        ClosePopups();
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
        if (_gold < _difficulty.RefreshCost)
        {
            ShowMessage("골드가 부족해요");
            return;
        }

        _gold -= _difficulty.RefreshCost;
        RollOffers();
        ShowMessage("");
        UpdateView();
    }

    // ---------- 정보 창 (칸을 누르면 열림, 구매·판매는 창의 버튼으로) ----------

    // 상점 칸: 정보 + "구매 NG" (골드가 모자라면 흐리게)
    public void ShowShopInfo(int index)
    {
        CreatureData creature = _offers[index];
        if (_dragSource != null || creature == null)
        {
            return;
        }

        int price = _config.GetPrice(creature);
        _infoPopup.Show(creature, 1, null, BuildSynergyLine(creature.Type));
        _infoPopup.SetAction($"구매 {price}G", _gold >= price, () => OnBuyClicked(index));
    }

    public void ShowBenchInfo(int index) => ShowOwnedInfo(SlotArea.Bench, index);
    public void ShowTeamInfo(int index) => ShowOwnedInfo(SlotArea.Team, index);

    // 대기석·팀 칸: 정보 + "판매 +NG" → 한 번 더 "정말 팔까요? +NG"
    private void ShowOwnedInfo(SlotArea area, int index)
    {
        CreatureInstance creature = GetSlots(area)[index];
        // 끌어서 같은 칸에 다시 놓은 경우에도 클릭이 들어오므로 무시 (끌기 중에는 창을 열지 않는다)
        if (_dragSource != null || creature == null)
        {
            return;
        }

        int price = _config.GetSellPrice(creature);
        _infoPopup.Show(creature.Data, creature.Level, creature.Item, BuildSynergyLine(creature.Data.Type));
        _infoPopup.SetAction($"판매 +{price}G", true, () => OnSellClicked(area, index),
            $"정말 팔까요? +{price}G", "팔기");
    }

    // "시너지" 버튼·시너지 줄: 5개 타입의 시너지 표
    public void OpenSynergyPanel()
    {
        if (_dragSource != null)
        {
            return;
        }
        CountTeamTypes();
        _synergyPanel.Show(_typeCounts, _synergy);
    }

    public void ClosePopups()
    {
        _infoPopup.Close();
        _synergyPanel.Close();
        _fusionPanel.Close();
        _fusionBookPanel.Close();
    }

    // ---------- 조합 ----------

    // 조합식 버튼: 런 중 언제든 열고 닫는다
    public void OpenFusionBook()
    {
        if (!_meta.FusionUnlocked)
        {
            return;
        }
        _fusionBookPanel.Show(_meta, GetFusionHighlight);
    }

    // 조합식 창 줄 강조: 2 지금 만들 수 있음, 1 재료 1마리 이상 있음, 0 없음
    private int GetFusionHighlight(FusionRecipe recipe)
    {
        if (Fusion.TryPlan(recipe, _team, _bench, out _))
        {
            return 2;
        }
        return Fusion.Has(recipe.MaterialA, _team, _bench) || Fusion.Has(recipe.MaterialB, _team, _bench) ? 1 : 0;
    }

    // "조합" 버튼: 지금 만들 수 있는 조합 목록 → 고르면 확인 창
    public void OpenFusionPanel()
    {
        if (_dragSource != null || !_meta.FusionUnlocked)
        {
            return;
        }
        Fusion.CollectAvailable(_config.FusionRecipes, _team, _bench, _fusionOptions);
        if (_fusionOptions.Count == 0)
        {
            return;
        }

        _fusionLabels.Clear();
        foreach (FusionRecipe recipe in _fusionOptions)
        {
            Fusion.TryPlan(recipe, _team, _bench, out Fusion.Plan plan);
            _fusionLabels.Add($"{recipe.Result.DisplayName} {CreatureInstance.ToStars(plan.Level)}  " +
                              $"({recipe.MaterialA.DisplayName} + {recipe.MaterialB.DisplayName})");
        }
        _fusionPanel.ShowList(_fusionLabels, OnFusionPicked);
    }

    private void OnFusionPicked(int index)
    {
        if (index < 0 || index >= _fusionOptions.Count
            || !Fusion.TryPlan(_fusionOptions[index], _team, _bench, out Fusion.Plan plan))
        {
            _fusionPanel.Close();
            return;
        }

        FusionRecipe recipe = _fusionOptions[index];
        CreatureInstance a = Fusion.GetA(plan, _team, _bench);
        CreatureInstance b = Fusion.GetB(plan, _team, _bench);
        string message = $"{a.Data.DisplayName} {CreatureInstance.ToStars(a.Level)} + {b.Data.DisplayName} {CreatureInstance.ToStars(b.Level)}" +
                         $"\n→ {recipe.Result.DisplayName} {CreatureInstance.ToStars(plan.Level)}";
        if (a.Level != b.Level)
        {
            message += "\n(결과 레벨은 두 재료 중 낮은 레벨)";
        }
        _fusionPanel.ShowConfirm(message, () => MakeFusion(recipe));
    }

    private void MakeFusion(FusionRecipe recipe)
    {
        if (!Fusion.TryPlan(recipe, _team, _bench, out Fusion.Plan plan))
        {
            return;
        }

        Fusion.Make(recipe, plan, _team, _bench, _items, out bool inTeam, out int index, out int refund);
        _gold += refund;
        _meta.SetMade(recipe);
        ShowMessage(refund > 0 ? $"아이템 보관함이 꽉 차서 아이템을 {refund}골드로 바꿨어요" : "");
        UpdateView();
        GetSlotViews(inTeam ? SlotArea.Team : SlotArea.Bench)[index].Flash();
        AudioManager.Play(Sfx.Merge);
    }

    // 조합 버튼 표시 + 새로 만들 수 있게 된 조합식 발견 기록·알림 (UpdateView에서)
    private void UpdateFusion()
    {
        bool unlocked = _meta.FusionUnlocked;
        _fusionButton.gameObject.SetActive(unlocked);
        _fusionBookButton.gameObject.SetActive(unlocked);
        // 조합 버튼이 보이면 시너지 줄을 버튼 왼쪽까지 줄인다
        RectTransform synergyRect = _synergyText.rectTransform;
        if (_synergyTextRight < 0f)
        {
            _synergyTextRight = synergyRect.anchorMax.x;
        }
        float right = unlocked ? ((RectTransform)_fusionBookButton.transform).anchorMin.x - 0.01f : _synergyTextRight;
        synergyRect.anchorMax = new Vector2(right, synergyRect.anchorMax.y);
        if (!unlocked)
        {
            return;
        }

        Fusion.CollectAvailable(_config.FusionRecipes, _team, _bench, _fusionOptions);
        _fusionButton.interactable = _fusionOptions.Count > 0;
        _fusionLabel.text = _fusionOptions.Count > 0 ? $"조합 {_fusionOptions.Count}" : "조합";

        _synergyBuilder.Clear();
        foreach (FusionRecipe recipe in _fusionOptions)
        {
            if (_meta.IsDiscovered(recipe))
            {
                continue;
            }
            _meta.SetDiscovered(recipe);
            _synergyBuilder.Append(_synergyBuilder.Length > 0 ? "\n" : "")
                .Append(recipe.MaterialA.DisplayName).Append(" + ").Append(recipe.MaterialB.DisplayName)
                .Append(" → ").Append(recipe.Result.DisplayName);
        }
        if (_synergyBuilder.Length > 0)
        {
            _tip.Show("새 조합식 발견!", _synergyBuilder + "\n\"조합\" 버튼으로 만들 수 있어요");
        }
    }

    private void OnBuyClicked(int index)
    {
        if (TryBuy(index))
        {
            _infoPopup.Close();
        }
        else
        {
            _infoPopup.ShowMessage(_messageText.text);
        }
    }

    private void OnSellClicked(SlotArea area, int index)
    {
        if (TrySell(area, index))
        {
            _infoPopup.Close();
        }
        else
        {
            _infoPopup.ShowMessage(_messageText.text);
        }
    }

    private bool TryBuy(int index)
    {
        CreatureData creature = _offers[index];
        if (creature == null)
        {
            return false;
        }

        // 같은 Lv1이 이미 2마리 있으면 사자마자 합쳐지므로 대기석 자리가 필요 없다
        CollectSame(creature, 1);
        bool mergesNow = _mergeCandidates.Count >= MergeCount - 1;

        int benchIndex = System.Array.IndexOf(_bench, null);
        if (benchIndex < 0 && !mergesNow)
        {
            ShowMessage("대기석이 꽉 찼어요");
            return false;
        }

        int price = _config.GetPrice(creature);
        if (_gold < price)
        {
            ShowMessage("골드가 부족해요");
            return false;
        }

        _gold -= price;
        _offers[index] = null;
        AudioManager.Play(Sfx.Buy);
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
        return true;
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
        AudioManager.Play(Sfx.Buy);
        _items[itemIndex] = _itemOffer;
        _itemOffer = null;
        ShowMessage("");
        UpdateView();
    }

    // 대기석·팀 칸 크리처 판매 (정보 창에서 확인까지 누른 뒤)
    private bool TrySell(SlotArea area, int index)
    {
        CreatureInstance creature = GetSlots(area)[index];
        if (creature == null)
        {
            return false;
        }

        // 장착한 아이템은 보관함으로. 보관함이 꽉 차면 팔지 않는다
        if (creature.Item != null)
        {
            if (!TryStoreItem(creature.Item))
            {
                ShowMessage("아이템 보관함이 꽉 차서 팔 수 없어요");
                return false;
            }
            creature.Item = null;
        }

        _gold += _config.GetSellPrice(creature);
        AudioManager.Play(Sfx.Sell);
        GetSlots(area)[index] = null;
        ShowMessage("");
        UpdateView();
        return true;
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
        HideIncome();
        _run.BeginBattle(_team);
    }

    // 상단 골드 글자를 누르면: 골드 규칙 안내 (상점 화면일 때만)
    public void ShowGoldHelp()
    {
        if (!gameObject.activeInHierarchy || _dragSource != null)
        {
            return;
        }
        ShowMessage(string.Format(GoldHelp, _config.InterestStep, _difficulty.MaxInterest));
    }

    // 골드 아래에 "+10 +이자 2"를 잠깐 보여 준다
    private void ShowIncome(int income, int interest, bool capped)
    {
        if (_goldIncomeText == null)
        {
            return;
        }
        HideIncome();
        string text = interest > 0 ? $"+{income} +이자 {interest}" : $"+{income}";
        _goldIncomeText.text = capped ? $"{text} (최대 {_config.MaxGold})" : text;
        _goldIncomeText.gameObject.SetActive(true);
        if (gameObject.activeInHierarchy)
        {
            _incomeRoutine = StartCoroutine(HideIncomeLater());
        }
    }

    private System.Collections.IEnumerator HideIncomeLater()
    {
        yield return new WaitForSeconds(IncomeShowSeconds);
        _incomeRoutine = null;
        HideIncome();
    }

    private void HideIncome()
    {
        if (_incomeRoutine != null)
        {
            StopCoroutine(_incomeRoutine);
            _incomeRoutine = null;
        }
        if (_goldIncomeText != null)
        {
            _goldIncomeText.gameObject.SetActive(false);
        }
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
            _dragIcon.sprite = creature.Data.GetSprite(creature.Level);
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
            // 조합 크리처는 같은 것 3마리 합성에서 빠진다
            if (owned.Level >= CreatureData.MaxLevel || owned.Data.FusionOnly)
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
        AudioManager.Play(Sfx.Merge);
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
        // 잠긴 크리처는 내 상점에 나오지 않는다
        for (int i = 0; i < _offers.Length; i++)
        {
            _offers[i] = _config.GetRandomCreature(_floor, _meta.IsUnlocked);
        }
        _itemOffer = _config.GetRandomItem();
    }

    private void ShowMessage(string message)
    {
        _messageText.text = message;
    }

    private void UpdateView()
    {
        _hud.SetGold(_gold);
        _refreshLabel.text = $"새로고침 ({_difficulty.RefreshCost}G)";

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
        UpdateFusion();
    }

    // 팀 칸 크리처만 세서 _typeCounts에 (전투와 같은 TypeSynergy.CountUnique)
    private void CountTeamTypes()
    {
        _teamCreatures.Clear();
        foreach (CreatureInstance creature in _team)
        {
            _teamCreatures.Add(creature?.Data);
        }
        TypeSynergy.CountUnique(_teamCreatures, _typeCounts);
    }

    // 정보 창의 시너지 한 줄. 예: "불 시너지 2/4 · 1단계 켜짐: 공격력 +15%"
    private string BuildSynergyLine(CreatureType type)
    {
        CountTeamTypes();
        int count = _typeCounts[(int)type];
        int tier = _synergy.GetTier(count);
        string head = $"{TypeSynergy.GetTypeName(type)} 시너지 {count}/{_synergy.GetNextThreshold(count)}";
        return tier > 0
            ? $"{head} · {tier}단계 켜짐: {_synergy.GetEffectText(type, tier)}"
            : $"{head} · {_synergy.GetThreshold(1)}마리부터: {_synergy.GetEffectText(type, 1)}";
    }

    // 팀 칸 크리처만 세서 켜진 시너지를 "불 2/4 · 물 3/4 · 온천" 형태로 표시
    private void UpdateSynergyText()
    {
        CountTeamTypes();

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
        // 켜진 조합 시너지는 이름만
        MixedSynergyConfig mixed = _synergy.Mixed;
        for (int i = 0; mixed != null && i < mixed.Count; i++)
        {
            if (mixed.IsActive(i, _typeCounts))
            {
                if (_synergyBuilder.Length > 0)
                {
                    _synergyBuilder.Append(" · ");
                }
                _synergyBuilder.Append(mixed.Get(i).Name);
            }
        }

        _synergyText.text = _synergyBuilder.Length > 0 ? "시너지  " + _synergyBuilder : "";
    }
}
