using UnityEngine;
using UnityEngine.Serialization;

// 상점 수치와 상점에 나오는 크리처 목록
[CreateAssetMenu(fileName = "ShopConfig", menuName = "MonsterCamp/Shop Config")]
public class ShopConfig : ScriptableObject
{
    [Tooltip("라운드가 시작될 때 골드를 이 값으로 다시 채운다")]
    [FormerlySerializedAs("_startGold")]
    [SerializeField, Min(0)] private int _roundGold = 10;
    [SerializeField, Min(0)] private int _refreshCost = 1;
    [Tooltip("티어 1~3 구매 가격")]
    [SerializeField] private int[] _tierPrices = { 1, 2, 3 };
    [Tooltip("Lv1~3 판매 가격")]
    [SerializeField] private int[] _levelSellPrices = { 1, 3, 9 };
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

    public int GetSellPrice(CreatureInstance creature)
    {
        int index = Mathf.Clamp(creature.Level, 1, _levelSellPrices.Length) - 1;
        return _levelSellPrices[index];
    }

    // canAppear가 true인 크리처 중에서 고른다 (잠긴 크리처 제외용). 하나도 없으면 null
    public CreatureData GetRandomCreature(System.Func<CreatureData, bool> canAppear)
    {
        _candidates.Clear();
        foreach (CreatureData creature in _creaturePool)
        {
            if (creature != null && canAppear(creature))
            {
                _candidates.Add(creature);
            }
        }
        return _candidates.Count > 0 ? _candidates[Random.Range(0, _candidates.Count)] : null;
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
