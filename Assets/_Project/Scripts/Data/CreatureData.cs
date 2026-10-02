using UnityEngine;

// 크리처 1종의 기본 정보. 수치는 Lv1 기준.
[CreateAssetMenu(fileName = "NewCreature", menuName = "MonsterCamp/Creature Data")]
public class CreatureData : ScriptableObject
{
    public const int MaxLevel = 3;
    // 등급 = 구매 가격 (1~5골드)
    public const int MaxTier = 5;

    [SerializeField] private string _displayName;
    [SerializeField] private CreatureType _type;
    [Tooltip("등급 = 구매 가격 (1~5골드)")]
    [SerializeField, Range(1, MaxTier)] private int _tier = 1;

    [Header("능력치 (Lv1)")]
    [SerializeField, Min(1)] private int _maxHealth = 100;
    [SerializeField, Min(0)] private int _attack = 10;
    [Tooltip("초당 공격 횟수")]
    [SerializeField, Min(0.1f)] private float _attackSpeed = 1f;

    [Header("스킬")]
    [SerializeField, TextArea] private string _skillDescription;
    [Tooltip("스킬 종류 (칸의 뜻은 SkillType 설명 참고)")]
    [SerializeField] private SkillType _skillType;
    [Tooltip("N번째 공격마다 또는 N초마다")]
    [SerializeField, Min(0f)] private float _skillEvery;
    [Tooltip("확률 (0~1)")]
    [SerializeField, Range(0f, 1f)] private float _skillChance;
    [Tooltip("주 수치 (비율 0.3 = 30%, 배율, 마릿수, 초 등)")]
    [SerializeField] private float _skillValue1;
    [Tooltip("보조 수치 (지속 시간 등)")]
    [SerializeField] private float _skillValue2;

    [Header("레벨별 배율 (체력·공격력, Lv1~Lv3)")]
    [SerializeField] private float[] _levelMultipliers = { 1f, 1.8f, 3f };

    [Header("그림")]
    [Tooltip("기본 그림 (Lv1). 레벨이 없는 곳(도감, 상점)에서 사용")]
    [SerializeField] private Sprite _sprite;
    [Tooltip("레벨별 그림 Lv1~Lv3. 비어 있으면 기본 그림을 쓴다")]
    [SerializeField] private Sprite[] _levelSprites = new Sprite[MaxLevel];

    public string DisplayName => _displayName;
    public CreatureType Type => _type;
    public int Tier => _tier;
    public int MaxHealth => _maxHealth;
    public int Attack => _attack;
    public float AttackSpeed => _attackSpeed;
    public string SkillDescription => _skillDescription;
    public SkillType SkillType => _skillType;
    public float SkillEvery => _skillEvery;
    public float SkillChance => _skillChance;
    public float SkillValue1 => _skillValue1;
    public float SkillValue2 => _skillValue2;
    public Sprite Sprite => _sprite;

    public int GetMaxHealth(int level) => Mathf.RoundToInt(_maxHealth * GetLevelMultiplier(level));
    public int GetAttack(int level) => Mathf.RoundToInt(_attack * GetLevelMultiplier(level));

    public float GetLevelMultiplier(int level)
    {
        int index = Mathf.Clamp(level, 1, MaxLevel) - 1;
        return _levelMultipliers[index];
    }

    // 레벨에 맞는 그림 (대기석·팀·전투·드래그). 그 레벨 그림이 없으면 기본 그림
    public Sprite GetSprite(int level)
    {
        int index = Mathf.Clamp(level, 1, MaxLevel) - 1;
        if (_levelSprites != null && index < _levelSprites.Length && _levelSprites[index] != null)
        {
            return _levelSprites[index];
        }
        return _sprite;
    }

    private void OnValidate()
    {
        if (_levelMultipliers == null || _levelMultipliers.Length != MaxLevel)
        {
            System.Array.Resize(ref _levelMultipliers, MaxLevel);
        }
        if (_levelSprites == null || _levelSprites.Length != MaxLevel)
        {
            System.Array.Resize(ref _levelSprites, MaxLevel);
        }
    }
}
