using UnityEngine;

// 타입 시너지 수치. 각 배열은 [1단계, 2단계]
[CreateAssetMenu(fileName = "SynergyConfig", menuName = "MonsterCamp/Synergy Config")]
public class SynergyConfig : ScriptableObject
{
    [Tooltip("단계별 필요 마릿수 (팀 칸, 같은 크리처는 1마리로 셈)")]
    [SerializeField] private int[] _thresholds = { 2, 4 };

    [Header("불: 공격력 +비율")]
    [SerializeField] private float[] _fireAttackBonus = { 0.15f, 0.35f };
    [Header("물: 체력 +비율")]
    [SerializeField] private float[] _waterHealthBonus = { 0.15f, 0.35f };
    [Header("풀: 일정 시간마다 최대 체력의 비율만큼 회복")]
    [SerializeField] private float[] _grassRegenPercent = { 0.02f, 0.05f };
    [SerializeField, Min(0.1f)] private float _grassRegenInterval = 1f;
    [Header("번개: 공격 속도 +비율")]
    [SerializeField] private float[] _electricSpeedBonus = { 0.15f, 0.35f };
    [Header("바위: 받는 피해 -비율")]
    [SerializeField] private float[] _rockDamageReduction = { 0.10f, 0.25f };

    public float GrassRegenInterval => _grassRegenInterval;

    // 0 = 시너지 없음, 1 = 1단계, 2 = 2단계
    public int GetTier(int count)
    {
        int tier = 0;
        for (int i = 0; i < _thresholds.Length; i++)
        {
            if (count >= _thresholds[i])
            {
                tier = i + 1;
            }
        }
        return tier;
    }

    // 다음 단계 마릿수. 마지막 단계면 마지막 기준값
    public int GetNextThreshold(int count)
    {
        foreach (int threshold in _thresholds)
        {
            if (count < threshold)
            {
                return threshold;
            }
        }
        return _thresholds[_thresholds.Length - 1];
    }

    // 해당 타입 크리처가 받는 보너스. tier 0이면 보너스 없음
    public BattleBonus GetBonus(CreatureType type, int tier)
    {
        var bonus = new BattleBonus();
        if (tier <= 0)
        {
            return bonus;
        }

        int index = tier - 1;
        switch (type)
        {
            case CreatureType.Fire:
                bonus.AttackBonus = _fireAttackBonus[index];
                break;
            case CreatureType.Water:
                bonus.HealthBonus = _waterHealthBonus[index];
                break;
            case CreatureType.Grass:
                bonus.RegenPercent = _grassRegenPercent[index];
                bonus.RegenInterval = _grassRegenInterval;
                break;
            case CreatureType.Electric:
                bonus.SpeedBonus = _electricSpeedBonus[index];
                break;
            case CreatureType.Rock:
                bonus.DamageReduction = _rockDamageReduction[index];
                break;
        }
        return bonus;
    }
}
