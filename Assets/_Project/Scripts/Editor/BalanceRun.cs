using System.Collections.Generic;
using UnityEngine;

// 밸런스 시뮬레이터용: 화면 없이 한 판의 상점 단계 상태(골드, 상점, 대기석, 팀, 보관함)를 들고 있다.
// 상점 확률·가격·판매 가격·이자는 ShopConfig 함수를 그대로 부른다.
// [게임과 따로 구현] 구매·판매·합성·아이템 장착은 ShopScreen(MonoBehaviour, UI와 묶여 있음) 안에 있어서
// 같은 규칙을 여기로 옮겨 왔다. ShopScreen의 TryBuy / TrySell / BuyItem / MergeAll / MergeCandidates를 고치면 여기도 같이 고칠 것
public class BalanceRun
{
    // [게임과 따로 구현] MainSceneBuilder의 칸 수와 같게 (상점 5, 대기석 6, 팀 5, 보관함 3)
    public const int ShopSlotCount = 5;
    public const int BenchSlotCount = 6;
    public const int TeamSlotCount = 5;
    public const int ItemSlotCount = 3;
    private const int MergeCount = 3;

    private readonly ShopConfig _config;
    // 이자 최대, 새로고침 비용, 층 수 (상점 등급 확률)
    private readonly DifficultyData _difficulty;
    // 상점에 나올 수 있는 크리처 (잠긴 크리처 제외용)
    private readonly System.Func<CreatureData, bool> _canAppear;

    public readonly CreatureData[] Offers = new CreatureData[ShopSlotCount];
    public ItemData ItemOffer;
    public readonly CreatureInstance[] Bench = new CreatureInstance[BenchSlotCount];
    public readonly CreatureInstance[] Team = new CreatureInstance[TeamSlotCount];
    public readonly ItemData[] Items = new ItemData[ItemSlotCount];
    public int Gold;
    public int Round = 1;
    // 조합 해금 계정 (보통 클리어 후). 조합 규칙은 ShopScreen과 같은 Fusion을 그대로 부른다
    public bool FusionUnlocked;
    // 통계용: 이번 판에 만든 조합 수
    public int FusionsMade;

    // 통계용: 이번 판에 상점에 나온 크리처 / 산 크리처 (마리 수)
    public readonly Dictionary<CreatureData, int> Offered = new Dictionary<CreatureData, int>();
    public readonly Dictionary<CreatureData, int> Bought = new Dictionary<CreatureData, int>();

    private readonly List<(bool team, int index)> _mergeCandidates = new List<(bool, int)>();
    private readonly List<ItemData> _returnedItems = new List<ItemData>();

    public BalanceRun(ShopConfig config, DifficultyData difficulty, System.Func<CreatureData, bool> canAppear)
    {
        _config = config;
        _difficulty = difficulty;
        _canAppear = canAppear;
    }

    public ShopConfig Config => _config;
    public int RefreshCost => _difficulty.RefreshCost;

    // ShopScreen.StartRound와 같은 골드 계산: 남은 골드 + 층 골드 + bonusGold + 이자, 최대치까지. round = 층
    // (튜토리얼은 시뮬레이션하지 않는다: 항상 남은 골드·이자를 넘긴다)
    public void StartRound(int round, int bonusGold)
    {
        Round = round;
        int leftover = Gold;
        int interest = _config.GetInterest(leftover, _difficulty.MaxInterest);
        Gold = Mathf.Min(_config.MaxGold, leftover + _config.RoundGold + bonusGold + interest);
        RollOffers();
    }

    public bool Refresh()
    {
        if (Gold < _difficulty.RefreshCost)
        {
            return false;
        }
        Gold -= _difficulty.RefreshCost;
        RollOffers();
        return true;
    }

    // ShopScreen.RollOffers와 같음 (잠긴 크리처는 canAppear가 false)
    private void RollOffers()
    {
        for (int i = 0; i < Offers.Length; i++)
        {
            Offers[i] = _config.GetRandomCreature(Round, _canAppear);
            if (Offers[i] != null)
            {
                Offered.TryGetValue(Offers[i], out int count);
                Offered[Offers[i]] = count + 1;
            }
        }
        ItemOffer = _config.GetRandomItem();
    }

    // 같은 Lv1이 이미 2마리 있으면 사자마자 합쳐진다 (대기석 자리가 필요 없음)
    public bool MergesOnBuy(CreatureData creature) => CountSame(creature, 1) >= MergeCount - 1;

    // ShopScreen.TryBuy와 같은 규칙
    public bool TryBuy(int index)
    {
        CreatureData creature = Offers[index];
        if (creature == null)
        {
            return false;
        }

        CollectSame(creature, 1);
        bool mergesNow = _mergeCandidates.Count >= MergeCount - 1;
        int benchIndex = System.Array.IndexOf(Bench, null);
        if (benchIndex < 0 && !mergesNow)
        {
            return false;
        }

        int price = _config.GetPrice(creature);
        if (Gold < price)
        {
            return false;
        }

        Gold -= price;
        Offers[index] = null;
        Bought.TryGetValue(creature, out int bought);
        Bought[creature] = bought + 1;

        if (mergesNow)
        {
            MergeCandidates(MergeCount - 1);
        }
        else
        {
            Bench[benchIndex] = new CreatureInstance(creature);
        }
        MergeAll();
        return true;
    }

    // ShopScreen.BuyItem과 같은 규칙
    public bool BuyItem()
    {
        if (ItemOffer == null)
        {
            return false;
        }
        int itemIndex = System.Array.IndexOf(Items, null);
        if (itemIndex < 0 || Gold < ItemOffer.Price)
        {
            return false;
        }
        Gold -= ItemOffer.Price;
        Items[itemIndex] = ItemOffer;
        ItemOffer = null;
        return true;
    }

    // ShopScreen.TrySell과 같은 규칙 (장착 아이템은 보관함으로, 보관함이 꽉 차면 못 판다)
    public bool TrySell(bool team, int index)
    {
        CreatureInstance[] slots = team ? Team : Bench;
        CreatureInstance creature = slots[index];
        if (creature == null)
        {
            return false;
        }
        if (creature.Item != null)
        {
            if (!TryStoreItem(creature.Item))
            {
                return false;
            }
            creature.Item = null;
        }
        Gold += _config.GetSellPrice(creature);
        slots[index] = null;
        return true;
    }

    // ShopScreen.DropItem과 같음: 보관함 아이템을 크리처에 끼우고, 끼고 있던 아이템은 보관함 그 칸으로
    public void Equip(int itemIndex, CreatureInstance creature)
    {
        ItemData previous = creature.Item;
        creature.Item = Items[itemIndex];
        Items[itemIndex] = previous;
    }

    // 끌어서 옮기기와 같은 결과: 가진 크리처를 정해 준 자리로 다시 놓는다 (팀 5칸 + 대기석 나머지)
    public void Place(IList<CreatureInstance> team, IList<CreatureInstance> bench)
    {
        System.Array.Clear(Team, 0, Team.Length);
        System.Array.Clear(Bench, 0, Bench.Length);
        for (int i = 0; i < team.Count && i < Team.Length; i++)
        {
            Team[i] = team[i];
        }
        for (int i = 0; i < bench.Count && i < Bench.Length; i++)
        {
            Bench[i] = bench[i];
        }
    }

    // ShopScreen.MakeFusion과 같은 규칙 (재료 두 마리 → 결과, 보관함이 꽉 차면 아이템 가격만큼 골드)
    public bool TryFuse(FusionRecipe recipe)
    {
        if (!FusionUnlocked || !Fusion.TryPlan(recipe, Team, Bench, out Fusion.Plan plan))
        {
            return false;
        }
        Fusion.Make(recipe, plan, Team, Bench, Items, out _, out _, out int refund);
        Gold += refund;
        FusionsMade++;
        return true;
    }

    public void CollectOwned(List<CreatureInstance> result)
    {
        result.Clear();
        foreach (CreatureInstance creature in Team)
        {
            if (creature != null)
            {
                result.Add(creature);
            }
        }
        foreach (CreatureInstance creature in Bench)
        {
            if (creature != null)
            {
                result.Add(creature);
            }
        }
    }

    // 레벨 상관없이 가진 마리 수
    public int CountOwned(CreatureData data)
    {
        int count = 0;
        foreach (CreatureInstance creature in EnumerateOwned())
        {
            count += creature.Data == data ? 1 : 0;
        }
        return count;
    }

    public int CountSame(CreatureData data, int level)
    {
        CollectSame(data, level);
        return _mergeCandidates.Count;
    }

    private bool TryStoreItem(ItemData item)
    {
        int index = System.Array.IndexOf(Items, null);
        if (index < 0)
        {
            return false;
        }
        Items[index] = item;
        return true;
    }

    // ---------- 합성 (ShopScreen.MergeAll / TryMergeOnce / MergeCandidates와 같은 규칙) ----------

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

    private void MergeCandidates(int count)
    {
        (bool keepTeam, int keepIndex) = _mergeCandidates[0];
        CreatureInstance kept = (keepTeam ? Team : Bench)[keepIndex];

        _returnedItems.Clear();
        for (int i = 1; i < count; i++)
        {
            (bool team, int index) = _mergeCandidates[i];
            CreatureInstance[] slots = team ? Team : Bench;
            if (slots[index].Item != null)
            {
                _returnedItems.Add(slots[index].Item);
            }
            slots[index] = null;
        }
        if (kept.Item != null)
        {
            _returnedItems.Add(kept.Item);
        }

        kept.Level++;
        kept.Item = null;
        foreach (ItemData item in _returnedItems)
        {
            if (!TryStoreItem(item) && kept.Item == null)
            {
                kept.Item = item;
            }
        }
    }

    // 팀 칸 먼저, 그다음 대기석 (첫 번째 후보가 남는다)
    private void CollectSame(CreatureData data, int level)
    {
        _mergeCandidates.Clear();
        for (int i = 0; i < Team.Length; i++)
        {
            if (Team[i] != null && Team[i].Data == data && Team[i].Level == level)
            {
                _mergeCandidates.Add((true, i));
            }
        }
        for (int i = 0; i < Bench.Length; i++)
        {
            if (Bench[i] != null && Bench[i].Data == data && Bench[i].Level == level)
            {
                _mergeCandidates.Add((false, i));
            }
        }
    }

    private IEnumerable<CreatureInstance> EnumerateOwned()
    {
        foreach (CreatureInstance creature in Team)
        {
            if (creature != null)
            {
                yield return creature;
            }
        }
        foreach (CreatureInstance creature in Bench)
        {
            if (creature != null)
            {
                yield return creature;
            }
        }
    }
}
