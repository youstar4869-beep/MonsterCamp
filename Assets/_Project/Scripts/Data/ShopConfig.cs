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

    public CreatureData GetRandomCreature()
    {
        return _creaturePool[Random.Range(0, _creaturePool.Length)];
    }
}
