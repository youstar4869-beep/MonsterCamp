using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 상점·배치 화면: 골드, 상점 칸, 새로고침, 대기석, 판매, 팀 배치(끌어서 옮기기), 합성
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
    [SerializeField] private CreatureSlotView[] _benchSlots;
    [Tooltip("0~1 앞줄, 2~4 뒷줄")]
    [SerializeField] private CreatureSlotView[] _teamSlots;
    [Tooltip("끄는 동안 손가락을 따라다니는 그림")]
    [SerializeField] private Image _dragIcon;

    private CreatureData[] _offers;
    // 칸 위치를 유지하기 위해 빈 칸은 null
    private CreatureInstance[] _bench;
    private CreatureInstance[] _team;
    private int _gold;
    private SlotDragHandler _dragSource;

    // 합성 후보를 모을 때 재사용 (팀 칸 먼저, 그다음 대기석)
    private readonly List<(SlotArea area, int index)> _mergeCandidates = new List<(SlotArea, int)>();
    // 시너지 표시용 (팀 칸만)
    private readonly List<CreatureData> _teamCreatures = new List<CreatureData>();
    private readonly int[] _typeCounts = new int[TypeSynergy.TypeCount];
    private readonly System.Text.StringBuilder _synergyBuilder = new System.Text.StringBuilder();

    // 새 런 시작: 대기석·팀 비우기
    public void ResetRun()
    {
        _offers = new CreatureData[_shopSlots.Length];
        _bench = new CreatureInstance[_benchSlots.Length];
        _team = new CreatureInstance[_teamSlots.Length];
    }

    // 라운드 시작: 골드를 다시 채우고 상점을 새로 뽑는다. 대기석·팀은 그대로
    public void StartRound()
    {
        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
        _gold = _config.RoundGold;
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

    public void SellAt(int index)
    {
        // 끌어서 같은 칸에 다시 놓은 경우에도 클릭이 들어오므로 무시
        if (_dragSource != null || _bench[index] == null)
        {
            return;
        }

        _gold += _config.GetSellPrice(_bench[index]);
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
        CreatureInstance creature = GetSlots(source.Area)[source.Index];
        if (_dragSource != null || creature == null)
        {
            return false;
        }

        _dragSource = source;
        _dragIcon.sprite = creature.Data.Sprite;
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

        CreatureInstance[] from = GetSlots(_dragSource.Area);
        CreatureInstance[] to = GetSlots(target.Area);
        (from[_dragSource.Index], to[target.Index]) = (to[target.Index], from[_dragSource.Index]);
        ShowMessage("");
        UpdateView();
    }

    public void EndDrag()
    {
        _dragSource = null;
        _dragIcon.gameObject.SetActive(false);
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
        GetSlots(keepArea)[keepIndex].Level++;
        for (int i = 1; i < count; i++)
        {
            (SlotArea area, int index) = _mergeCandidates[i];
            GetSlots(area)[index] = null;
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
                _shopSlots[i].Show(_offers[i], 0, $"{_config.GetPrice(_offers[i])}G");
            }
            else
            {
                _shopSlots[i].Clear();
            }
        }

        for (int i = 0; i < _benchSlots.Length; i++)
        {
            CreatureInstance creature = _bench[i];
            if (creature != null)
            {
                _benchSlots[i].Show(creature.Data, creature.Level, $"판매 +{_config.GetSellPrice(creature)}G");
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
                _teamSlots[i].Show(creature.Data, creature.Level, "");
            }
            else
            {
                _teamSlots[i].Clear();
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
