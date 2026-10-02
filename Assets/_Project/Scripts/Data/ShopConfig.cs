using UnityEngine;
using UnityEngine.Serialization;

// 상점 수치와 상점에 나오는 크리처 목록, 라운드별 등급 확률
[CreateAssetMenu(fileName = "ShopConfig", menuName = "MonsterCamp/Shop Config")]
public class ShopConfig : ScriptableObject
{
    // 라운드 구간 하나의 등급 확률
    [System.Serializable]
    public struct TierOdds
    {
        [Tooltip("이 라운드부터 다음 줄의 라운드 전까지 적용")]
        public int FromRound;
        [Tooltip("1~5골드가 나올 확률(%). 합이 100이 아니어도 비율로 계산")]
        public int[] Percents;
    }

    [Tooltip("라운드가 시작될 때 골드를 이 값으로 다시 채운다")]
    [FormerlySerializedAs("_startGold")]
    [SerializeField, Min(0)] private int _roundGold = 10;
    [SerializeField, Min(0)] private int _refreshCost = 1;
    [Tooltip("등급 1~5 구매 가격")]
    [SerializeField] private int[] _tierPrices = { 1, 2, 3, 4, 5 };
    [Tooltip("판매 가격 = 구매 가격 × 이 배수 (Lv1~3, 합친 마리 수)")]
    [FormerlySerializedAs("_levelSellPrices")]
    [SerializeField] private int[] _levelSellMultipliers = { 1, 3, 9 };
    [Tooltip("라운드 구간별 등급 확률 (FromRound 오름차순)")]
    [SerializeField] private TierOdds[] _tierOdds =
    {
        new TierOdds { FromRound = 1, Percents = new[] { 75, 25, 0, 0, 0 } },
        new TierOdds { FromRound = 3, Percents = new[] { 50, 35, 15, 0, 0 } },
        new TierOdds { FromRound = 5, Percents = new[] { 30, 35, 25, 10, 0 } },
        new TierOdds { FromRound = 7, Percents = new[] { 20, 30, 30, 15, 5 } },
        new TierOdds { FromRound = 9, Percents = new[] { 15, 25, 30, 20, 10 } },
    };
    [SerializeField] private CreatureData[] _creaturePool;
    [Tooltip("상점 아이템 칸에 나오는 아이템 목록 (가격은 각 아이템 에셋)")]
    [SerializeField] private ItemData[] _itemPool;

    public int RoundGold => _roundGold;
    public int RefreshCost => _refreshCost;

    public int GetPrice(CreatureData creature)
    {
        int index = Mathf.Clamp(creature.Tier, 1, _tierPrices.Length) - 1;
        return _tierPrices[index];
    }

    // 구매 가격 × 합친 마리 수 (Lv1 ×1, Lv2 ×3, Lv3 ×9)
    public int GetSellPrice(CreatureInstance creature)
    {
        int index = Mathf.Clamp(creature.Level, 1, _levelSellMultipliers.Length) - 1;
        return GetPrice(creature.Data) * _levelSellMultipliers[index];
    }

    // 상점 칸 하나: 라운드 확률로 등급을 고른 뒤, 그 등급에서 canAppear(잠긴 것 제외)인 크리처 중 하나.
    // 그 등급에 후보가 없으면 아래 등급으로 내려간다. 끝까지 없으면 null
    public CreatureData GetRandomCreature(int round, System.Func<CreatureData, bool> canAppear)
    {
        for (int tier = RollTier(round); tier >= 1; tier--)
        {
            _candidates.Clear();
            foreach (CreatureData creature in _creaturePool)
            {
                if (creature != null && creature.Tier == tier && canAppear(creature))
                {
                    _candidates.Add(creature);
                }
            }
            if (_candidates.Count > 0)
            {
                return _candidates[Random.Range(0, _candidates.Count)];
            }
        }
        return null;
    }

    // 라운드에 맞는 확률표로 등급(1~5)을 하나 고른다
    private int RollTier(int round)
    {
        if (_tierOdds == null || _tierOdds.Length == 0)
        {
            return 1;
        }

        int[] percents = _tierOdds[0].Percents;
        foreach (TierOdds odds in _tierOdds)
        {
            if (round >= odds.FromRound)
            {
                percents = odds.Percents;
            }
        }

        int total = 0;
        foreach (int percent in percents)
        {
            total += Mathf.Max(0, percent);
        }
        if (total <= 0)
        {
            return 1;
        }

        int roll = Random.Range(0, total);
        for (int i = 0; i < percents.Length; i++)
        {
            roll -= Mathf.Max(0, percents[i]);
            if (roll < 0)
            {
                return i + 1;
            }
        }
        return 1;
    }

    private readonly System.Collections.Generic.List<CreatureData> _candidates =
        new System.Collections.Generic.List<CreatureData>();

    public ItemData GetRandomItem()
    {
        return _itemPool.Length > 0 ? _itemPool[Random.Range(0, _itemPool.Length)] : null;
    }

    // 저장 파일의 에셋 이름으로 찾기. 없으면 null
    public CreatureData FindCreature(string id)
    {
        return FindByName(_creaturePool, id);
    }

    public ItemData FindItem(string id)
    {
        return FindByName(_itemPool, id);
    }

    private static T FindByName<T>(T[] assets, string id) where T : Object
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        foreach (T asset in assets)
        {
            if (asset != null && asset.name == id)
            {
                return asset;
            }
        }
        return null;
    }
}
