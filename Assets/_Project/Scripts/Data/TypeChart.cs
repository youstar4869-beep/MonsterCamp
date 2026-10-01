using System;
using UnityEngine;

// 타입 상성표. 공격 타입이 유리하면 _advantageMultiplier,
// 반대로 상대가 유리한 관계면 _disadvantageMultiplier, 그 외는 1배.
[CreateAssetMenu(fileName = "TypeChart", menuName = "MonsterCamp/Type Chart")]
public class TypeChart : ScriptableObject
{
    [Serializable]
    public struct Advantage
    {
        public CreatureType Strong;
        public CreatureType Weak;
    }

    [SerializeField] private float _advantageMultiplier = 1.5f;
    [SerializeField] private float _disadvantageMultiplier = 0.75f;
    [SerializeField] private Advantage[] _advantages;

    public float GetMultiplier(CreatureType attacker, CreatureType defender)
    {
        foreach (Advantage advantage in _advantages)
        {
            if (advantage.Strong == attacker && advantage.Weak == defender)
            {
                return _advantageMultiplier;
            }
            if (advantage.Strong == defender && advantage.Weak == attacker)
            {
                return _disadvantageMultiplier;
            }
        }
        return 1f;
    }
}
