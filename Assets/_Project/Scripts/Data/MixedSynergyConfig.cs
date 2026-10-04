using System;
using System.Text;
using UnityEngine;

public enum MixedSynergyEffect
{
    // 일정 시간마다 아군 전체 최대 체력 비율 회복
    TeamRegen,
    // 공격 시 확률로 적 기절
    OnHitStun,
    // 전투 시작 시 아군 전체 최대 체력 비율 보호막
    StartShield,
    // 공격 시 확률로 피해 배율
    OnHitDoubleDamage,
    // 전투 시작 시 적 전체 최대 체력 비율 피해
    StartEnemyDamage,
    // 아군 전체 체력·공격력 +비율, 전투 시작 몇 초 공격 속도 +비율. 켜지면 다른 조합 시너지는 꺼짐
    Rainbow
}

// 조합 시너지: 필요한 타입이 팀 칸에 각 1마리 이상이면 켜짐 (타입 시너지와 겹쳐서 적용)
[CreateAssetMenu(fileName = "MixedSynergyConfig", menuName = "MonsterCamp/Mixed Synergy Config")]
public class MixedSynergyConfig : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string Name;
        public CreatureType[] Types;
        public MixedSynergyEffect Effect;
        [Tooltip("회복·보호막·피해: 최대 체력 비율 / 기절·피해 배율: 확률 / 무지개: 체력·공격력 +비율")]
        public float Value1;
        [Tooltip("회복: 간격(초) / 기절: 시간(초) / 피해 배율: 배율 / 무지개: 공격 속도 +비율")]
        public float Value2;
        [Tooltip("무지개: 공격 속도가 오르는 시간(초)")]
        public float Value3;
    }

    [SerializeField] private Entry[] _entries =
    {
        new Entry { Name = "온천", Types = new[] { CreatureType.Fire, CreatureType.Water, CreatureType.Grass },
            Effect = MixedSynergyEffect.TeamRegen, Value1 = 0.02f, Value2 = 2f },
        new Entry { Name = "뇌우", Types = new[] { CreatureType.Water, CreatureType.Grass, CreatureType.Electric },
            Effect = MixedSynergyEffect.OnHitStun, Value1 = 0.1f, Value2 = 1f },
        new Entry { Name = "수정숲", Types = new[] { CreatureType.Grass, CreatureType.Electric, CreatureType.Rock },
            Effect = MixedSynergyEffect.StartShield, Value1 = 0.1f },
        new Entry { Name = "용광로", Types = new[] { CreatureType.Electric, CreatureType.Rock, CreatureType.Fire },
            Effect = MixedSynergyEffect.OnHitDoubleDamage, Value1 = 0.15f, Value2 = 2f },
        new Entry { Name = "증기화산", Types = new[] { CreatureType.Rock, CreatureType.Fire, CreatureType.Water },
            Effect = MixedSynergyEffect.StartEnemyDamage, Value1 = 0.08f },
        new Entry { Name = "무지개", Types = new[] { CreatureType.Fire, CreatureType.Water, CreatureType.Grass,
                CreatureType.Electric, CreatureType.Rock },
            Effect = MixedSynergyEffect.Rainbow, Value1 = 0.15f, Value2 = 0.3f, Value3 = 3f },
    };

    private readonly StringBuilder _text = new StringBuilder();

    public int Count => _entries.Length;
    public Entry Get(int index) => _entries[index];

    // 그 조합에서 팀에 없는 타입 수. typeCounts: TypeSynergy.CountUnique 결과
    public int GetMissingCount(int index, int[] typeCounts)
    {
        int missing = 0;
        foreach (CreatureType type in _entries[index].Types)
        {
            if (typeCounts[(int)type] <= 0)
            {
                missing++;
            }
        }
        return missing;
    }

    // 타입이 다 있고, 무지개가 켜져 있으면 무지개만
    public bool IsActive(int index, int[] typeCounts)
    {
        if (GetMissingCount(index, typeCounts) > 0)
        {
            return false;
        }
        return _entries[index].Effect == MixedSynergyEffect.Rainbow || !IsRainbowActive(typeCounts);
    }

    public bool IsRainbowActive(int[] typeCounts)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (_entries[i].Effect == MixedSynergyEffect.Rainbow && GetMissingCount(i, typeCounts) == 0)
            {
                return true;
            }
        }
        return false;
    }

    // 켜진 조합 시너지를 보너스에 더한다 (안 켜졌으면 아무것도 바꾸지 않음)
    public void AddBonus(int[] typeCounts, ref BattleBonus bonus)
    {
        for (int i = 0; i < _entries.Length; i++)
        {
            if (!IsActive(i, typeCounts))
            {
                continue;
            }
            Entry entry = _entries[i];
            switch (entry.Effect)
            {
                case MixedSynergyEffect.TeamRegen:
                    bonus.TeamRegenPercent += entry.Value1;
                    bonus.TeamRegenInterval = entry.Value2;
                    break;
                case MixedSynergyEffect.OnHitStun:
                    bonus.StunChance += entry.Value1;
                    bonus.StunSeconds = Mathf.Max(bonus.StunSeconds, entry.Value2);
                    break;
                case MixedSynergyEffect.StartShield:
                    bonus.StartShieldPercent += entry.Value1;
                    break;
                case MixedSynergyEffect.OnHitDoubleDamage:
                    bonus.CritChance += entry.Value1;
                    bonus.CritMultiplier = Mathf.Max(bonus.CritMultiplier, entry.Value2);
                    break;
                case MixedSynergyEffect.StartEnemyDamage:
                    bonus.StartEnemyDamagePercent += entry.Value1;
                    break;
                case MixedSynergyEffect.Rainbow:
                    bonus.HealthBonus += entry.Value1;
                    bonus.AttackBonus += entry.Value1;
                    bonus.StartHasteBonus = Mathf.Max(bonus.StartHasteBonus, entry.Value2);
                    bonus.StartHasteSeconds = Mathf.Max(bonus.StartHasteSeconds, entry.Value3);
                    break;
            }
        }
    }

    // 화면에 보여 줄 효과 글 (수치를 바꾸면 글도 따라 바뀐다)
    public string GetEffectText(int index)
    {
        Entry e = _entries[index];
        switch (e.Effect)
        {
            case MixedSynergyEffect.TeamRegen:
                return $"{e.Value2:0.#}초마다 아군 전체 최대 체력 {Percent(e.Value1)}% 회복";
            case MixedSynergyEffect.OnHitStun:
                return $"공격 시 {Percent(e.Value1)}% 확률로 적 {e.Value2:0.#}초 기절";
            case MixedSynergyEffect.StartShield:
                return $"전투 시작 시 아군 전체 최대 체력 {Percent(e.Value1)}% 보호막";
            case MixedSynergyEffect.OnHitDoubleDamage:
                return $"공격 시 {Percent(e.Value1)}% 확률로 피해 {e.Value2:0.#}배";
            case MixedSynergyEffect.StartEnemyDamage:
                return $"전투 시작 시 적 전체 최대 체력 {Percent(e.Value1)}% 피해";
            default:
                return $"아군 전체 체력·공격력 +{Percent(e.Value1)}%, 전투 시작 {e.Value3:0.#}초 공격 속도 +{Percent(e.Value2)}%";
        }
    }

    // 예: "불+물+풀", 모든 타입이면 "5타입 모두"
    public string GetTypesText(int index)
    {
        CreatureType[] types = _entries[index].Types;
        if (types.Length >= TypeSynergy.TypeCount)
        {
            return $"{TypeSynergy.TypeCount}타입 모두";
        }
        return JoinTypes(types, null, "+");
    }

    // 팀에 없는 타입. 예: "바위" / "풀·바위"
    public string GetMissingText(int index, int[] typeCounts)
    {
        return JoinTypes(_entries[index].Types, typeCounts, "·");
    }

    // typeCounts가 있으면 없는 타입만
    private string JoinTypes(CreatureType[] types, int[] typeCounts, string separator)
    {
        _text.Clear();
        foreach (CreatureType type in types)
        {
            if (typeCounts != null && typeCounts[(int)type] > 0)
            {
                continue;
            }
            if (_text.Length > 0)
            {
                _text.Append(separator);
            }
            _text.Append(TypeSynergy.GetTypeName(type));
        }
        return _text.ToString();
    }

    private static string Percent(float ratio) => (ratio * 100f).ToString("0.#");
}
