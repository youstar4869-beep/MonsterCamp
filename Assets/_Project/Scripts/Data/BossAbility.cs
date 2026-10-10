using System.Collections.Generic;
using UnityEngine;

public enum BossAbilityType
{
    // 전투 시작 Value1초 동안 적 전체 피해 무시
    Invulnerable,
    // 전투 Value1초가 지나면 적 전체 공격력 Value2배
    Enrage,
    // Value2초마다 적 전체 최대 체력 Value1 비율 회복
    Regen,
    // 적 전체가 받은 피해의 Value1 비율을 공격자에게 되돌림
    Thorns,
    // 적 전체 공격 속도 +Value1 비율
    Haste,
    // 적이 가장 비싼 아군(골드 × 별)부터 노림
    Hunter
}

// 보스 특수 능력 하나 (불가능 난이도). 효과 수치는 Value1·Value2 (뜻은 BossAbilityType 설명)
[CreateAssetMenu(fileName = "BossAbility", menuName = "MonsterCamp/Boss Ability")]
public class BossAbility : ScriptableObject
{
    [SerializeField] private string _displayName;
    [Tooltip("전투 시작 때 '보스 능력: 이름 — 설명'으로 보여 주는 짧은 설명")]
    [SerializeField] private string _description;
    [SerializeField] private BossAbilityType _type;
    [Tooltip("철벽: 초 / 광폭화: 발동까지 초 / 재생: 최대 체력 비율 / 가시 껍질: 되돌리는 비율 / 질풍: 공격 속도 증가 비율")]
    [SerializeField, Min(0f)] private float _value1;
    [Tooltip("광폭화: 공격력 배율 / 재생: 간격(초) / 나머지는 안 씀")]
    [SerializeField, Min(0f)] private float _value2;

    public string DisplayName => _displayName;
    public string Description => _description;
    public BossAbilityType Type => _type;
    public float Value1 => _value1;
    public float Value2 => _value2;

    // 능력 목록을 전투 입력 하나로 합친다 (비었으면 효과 없음)
    public static BossEffects Combine(IReadOnlyList<BossAbility> abilities)
    {
        var effects = new BossEffects();
        if (abilities == null)
        {
            return effects;
        }
        foreach (BossAbility ability in abilities)
        {
            if (ability == null)
            {
                continue;
            }
            switch (ability._type)
            {
                case BossAbilityType.Invulnerable:
                    effects.InvulnerableSeconds = Mathf.Max(effects.InvulnerableSeconds, ability._value1);
                    break;
                case BossAbilityType.Enrage:
                    effects.EnrageAfterSeconds = ability._value1;
                    effects.EnrageAttackMultiplier = ability._value2;
                    break;
                case BossAbilityType.Regen:
                    effects.RegenPercent = ability._value1;
                    effects.RegenInterval = ability._value2;
                    break;
                case BossAbilityType.Thorns:
                    effects.ThornsPercent += ability._value1;
                    break;
                case BossAbilityType.Haste:
                    effects.AttackSpeedBonus += ability._value1;
                    break;
                case BossAbilityType.Hunter:
                    effects.Hunter = true;
                    break;
            }
        }
        return effects;
    }
}
