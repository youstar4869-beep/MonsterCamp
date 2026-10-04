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

    [Tooltip("라운드가 시작될 때 남은 골드에 더해 주는 골드")]
    [FormerlySerializedAs("_startGold")]
    [SerializeField, Min(0)] private int _roundGold = 10;
    [Tooltip("이자: 라운드 시작 전 남은 골드 이 값마다 1골드")]
    [SerializeField, Min(1)] private int _interestStep = 5;
    [Tooltip("가질 수 있는 골드 최대치 (넘으면 이 값으로)")]
    [SerializeField, Min(1)] private int _maxGold = 50;
    [Tooltip("등급 1~5 구매 가격")]
    [SerializeField] private int[] _tierPrices = { 1, 2, 3, 4, 5 };
    [Tooltip("판매 가격 = 구매 가격 × 이 배수 (Lv1~3, 합친 마리 수)")]
    [FormerlySerializedAs("_levelSellPrices")]
    [SerializeField] private int[] _levelSellMultipliers = { 1, 3, 9 };
    [Tooltip("라운드(= 층) 구간별 등급 확률 (FromRound 오름차순)")]
    [SerializeField] private TierOdds[] _tierOdds =
    {
        new TierOdds { FromRound = 1, Percents = new[] { 75, 25, 0, 0, 0 } },
        new TierOdds { FromRound = 3, Percents = new[] { 50, 35, 15, 0, 0 } },
        new TierOdds { FromRound = 5, Percents = new[] { 30, 35, 25, 10, 0 } },
        new TierOdds { FromRound = 7, Percents = new[] { 20, 30, 30, 15, 5 } },
        new TierOdds { FromRound = 9, Percents = new[] { 15, 25, 30, 20, 10 } },
    };
    [Header("높은 층 (마지막 확률표에서 시작)")]
    [Tooltip("이 층부터 층마다 4·5골드 확률이 늘어남")]
    [SerializeField, Min(1)] private int _lateFromFloor = 11;
    [Tooltip("층마다 늘어나는 4골드 / 5골드 확률(%p). 늘어난 만큼 1골드부터 줄인다")]
    [SerializeField, Min(0f)] private float _lateTier4PerFloor = 0.5f;
    [SerializeField, Min(0f)] private float _lateTier5PerFloor = 0.5f;
    [Tooltip("4골드 / 5골드 확률 최대(%)")]
    [SerializeField, Range(0, 100)] private int _lateTier4Max = 30;
    [SerializeField, Range(0, 100)] private int _lateTier5Max = 20;
    [SerializeField] private CreatureData[] _creaturePool;
    [Tooltip("상점 아이템 칸에 나오는 아이템 목록 (가격은 각 아이템 에셋)")]
    [SerializeField] private ItemData[] _itemPool;

    public int RoundGold => _roundGold;
    public int InterestStep => _interestStep;
    public int MaxGold => _maxGold;
    public System.Collections.Generic.IReadOnlyList<CreatureData> CreaturePool => _creaturePool;

    // 남은 골드 5골드마다 1골드, 최대 maxInterest골드 (최대치는 난이도 에셋)
    public int GetInterest(int leftoverGold, int maxInterest)
    {
        return Mathf.Min(maxInterest, Mathf.Max(0, leftoverGold) / Mathf.Max(1, _interestStep));
    }

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

    // 상점 칸 하나: 층 확률로 등급을 고른 뒤, 그 등급에서 canAppear(잠긴 것 제외)인 크리처 중 하나.
    // 그 등급에 후보가 없으면 아래 등급으로 내려간다. 끝까지 없으면 null
    public CreatureData GetRandomCreature(int floor, System.Func<CreatureData, bool> canAppear)
    {
        for (int tier = RollTier(floor); tier >= 1; tier--)
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

    // 층에 맞는 1~5골드 확률표 (적 팀 자동 생성도 쓴다). 표가 없으면 null.
    // 1~10층은 라운드 표 그대로 (층 = 라운드), _lateFromFloor층부터는 마지막 표에 4·5골드를 층마다 더한다
    public int[] GetTierPercents(int floor)
    {
        if (_tierOdds == null || _tierOdds.Length == 0)
        {
            return null;
        }

        int[] percents = _tierOdds[0].Percents;
        foreach (TierOdds odds in _tierOdds)
        {
            if (floor >= odds.FromRound)
            {
                percents = odds.Percents;
            }
        }
        if (floor < _lateFromFloor || percents.Length < CreatureData.MaxTier)
        {
            return percents;
        }

        // 늘어난 만큼 1골드 → 2골드 → 3골드 순으로 줄인다
        int steps = floor - _lateFromFloor + 1;
        System.Array.Copy(percents, _latePercents, CreatureData.MaxTier);
        int add4 = Mathf.Max(0, Mathf.Min(_lateTier4Max, Mathf.RoundToInt(percents[3] + _lateTier4PerFloor * steps)) - percents[3]);
        int add5 = Mathf.Max(0, Mathf.Min(_lateTier5Max, Mathf.RoundToInt(percents[4] + _lateTier5PerFloor * steps)) - percents[4]);
        _latePercents[3] += add4;
        _latePercents[4] += add5;
        int remove = add4 + add5;
        for (int i = 0; i < 3 && remove > 0; i++)
        {
            int take = Mathf.Min(_latePercents[i], remove);
            _latePercents[i] -= take;
            remove -= take;
        }
        return _latePercents;
    }

    private readonly int[] _latePercents = new int[CreatureData.MaxTier];

    // 층에 맞는 확률표로 등급(1~5)을 하나 고른다
    private int RollTier(int floor)
    {
        int[] percents = GetTierPercents(floor);
        if (percents == null)
        {
            return 1;
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
