using UnityEngine;

// 상점 수치와 상점에 나오는 크리처 목록
[CreateAssetMenu(fileName = "ShopConfig", menuName = "MonsterCamp/Shop Config")]
public class ShopConfig : ScriptableObject
{
    [SerializeField, Min(0)] private int _startGold = 10;
    [SerializeField, Min(0)] private int _refreshCost = 1;
    [SerializeField, Min(0)] private int _sellPrice = 1;
    [Tooltip("티어 1~3 구매 가격")]
    [SerializeField] private int[] _tierPrices = { 1, 2, 3 };
    [SerializeField] private CreatureData[] _creaturePool;

    public int StartGold => _startGold;
    public int RefreshCost => _refreshCost;
    public int SellPrice => _sellPrice;

    public int GetPrice(CreatureData creature)
    {
        int index = Mathf.Clamp(creature.Tier, 1, _tierPrices.Length) - 1;
        return _tierPrices[index];
    }

    public CreatureData GetRandomCreature()
    {
        return _creaturePool[Random.Range(0, _creaturePool.Length)];
    }
}
