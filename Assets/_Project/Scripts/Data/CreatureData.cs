using UnityEngine;

// 크리처 1종의 기본 정보. 수치는 Lv1 기준.
[CreateAssetMenu(fileName = "NewCreature", menuName = "MonsterCamp/Creature Data")]
public class CreatureData : ScriptableObject
{
    public const int MaxLevel = 3;

    [SerializeField] private string _displayName;
    [SerializeField] private CreatureType _type;
    [SerializeField, Range(1, 3)] private int _tier = 1;

    [Header("능력치 (Lv1)")]
    [SerializeField, Min(1)] private int _maxHealth = 100;
    [SerializeField, Min(0)] private int _attack = 10;
    [Tooltip("초당 공격 횟수")]
    [SerializeField, Min(0.1f)] private float _attackSpeed = 1f;

    [Header("스킬")]
    [SerializeField, TextArea] private string _skillDescription;

    [Header("레벨별 배율 (체력·공격력, Lv1~Lv3)")]
    [SerializeField] private float[] _levelMultipliers = { 1f, 1.8f, 3f };

    [Header("그림 (임시: 색깔 네모)")]
    [SerializeField] private Sprite _sprite;

    public string DisplayName => _displayName;
    public CreatureType Type => _type;
    public int Tier => _tier;
    public int MaxHealth => _maxHealth;
    public int Attack => _attack;
    public float AttackSpeed => _attackSpeed;
    public string SkillDescription => _skillDescription;
    public Sprite Sprite => _sprite;

    public int GetMaxHealth(int level) => Mathf.RoundToInt(_maxHealth * GetLevelMultiplier(level));
    public int GetAttack(int level) => Mathf.RoundToInt(_attack * GetLevelMultiplier(level));

    public float GetLevelMultiplier(int level)
    {
        int index = Mathf.Clamp(level, 1, MaxLevel) - 1;
        return _levelMultipliers[index];
    }

    private void OnValidate()
    {
        if (_levelMultipliers == null || _levelMultipliers.Length != MaxLevel)
        {
            System.Array.Resize(ref _levelMultipliers, MaxLevel);
        }
    }
}
