using UnityEngine;

// 난이도 하나: 층 수, 목숨, 층마다 적 능력치 증가, 골드 규칙, 클리어 별, 열리는 조건
[CreateAssetMenu(fileName = "Difficulty", menuName = "MonsterCamp/Difficulty")]
public class DifficultyData : ScriptableObject
{
    [SerializeField] private string _displayName;
    [Tooltip("난이도 선택 카드의 한 줄 설명")]
    [SerializeField] private string _description;
    [SerializeField, Min(1)] private int _floorCount = 30;
    [SerializeField, Min(1)] private int _lives = 3;
    [Tooltip("층마다 적 체력·공격력 증가 (0.03 = 3%). 1층은 그대로, n층은 1 + 값 × (n - 1)배")]
    [SerializeField, Min(0f)] private float _enemyStatPerFloor = 0.03f;
    [Tooltip("이 층마다 보스 (10이면 10, 20, 30...)")]
    [SerializeField, Min(1)] private int _bossEvery = 10;
    [Tooltip("보스 층에만 적 체력·공격력에 더 곱하는 배율 (1이면 그대로)")]
    [SerializeField, Min(0.1f)] private float _bossStatMultiplier = 1f;
    [Tooltip("보스 층 자동 생성 팀 레벨 + 이 값 (최대 Lv3, 1~10층 정해 둔 팀은 그대로)")]
    [SerializeField, Min(0)] private int _bossLevelBonus = 1;
    [Tooltip("적 팀에도 조합 시너지를 적용하고, 자동 생성 팀이 조합 시너지를 노림")]
    [SerializeField] private bool _enemyMixedSynergy;
    [Tooltip("이자 최대 골드")]
    [SerializeField, Min(0)] private int _maxInterest = 3;
    [SerializeField, Min(0)] private int _refreshCost = 1;
    [Tooltip("마지막 층을 이겨 클리어하면 받는 별")]
    [SerializeField, Min(0)] private int _clearStars = 10;
    [Tooltip("이 난이도를 클리어해야 열림 (비우면 처음부터 열림)")]
    [SerializeField] private DifficultyData _requiredClear;

    [Header("적 레벨 (11층부터 자동 생성 팀)")]
    [Tooltip("이 층부터 ★★(Lv2) 적이 섞임. 0이면 없음")]
    [SerializeField, Min(0)] private int _star2FromFloor = 20;
    [Tooltip("이 층부터 ★★★(Lv3) 적이 섞임. 0이면 없음")]
    [SerializeField, Min(0)] private int _star3FromFloor;
    [Tooltip("시작 층에서 한 마리가 그 레벨일 확률")]
    [SerializeField, Range(0f, 1f)] private float _levelChanceAtStart = 0.2f;
    [Tooltip("시작 층 뒤로 층마다 더하는 확률")]
    [SerializeField, Min(0f)] private float _levelChancePerFloor = 0.03f;

    public string DisplayName => _displayName;
    public string Description => _description;
    public int FloorCount => _floorCount;
    public int Lives => _lives;
    public int MaxInterest => _maxInterest;
    public int RefreshCost => _refreshCost;
    public int ClearStars => _clearStars;
    public DifficultyData RequiredClear => _requiredClear;

    // floor는 1부터
    public bool IsBossFloor(int floor) => floor % _bossEvery == 0;

    public int BossLevelBonus => _bossLevelBonus;
    public bool EnemyMixedSynergy => _enemyMixedSynergy;

    // 층마다 증가 × 보스 층 배율
    public float GetEnemyStatMultiplier(int floor) =>
        (1f + _enemyStatPerFloor * (floor - 1)) * (IsBossFloor(floor) ? _bossStatMultiplier : 1f);

    // 자동 생성 팀 한 마리가 ★★ / ★★★일 확률 (시작 층 전이면 0)
    public float GetStar2Chance(int floor) => GetLevelChance(_star2FromFloor, floor);
    public float GetStar3Chance(int floor) => GetLevelChance(_star3FromFloor, floor);

    private float GetLevelChance(int fromFloor, int floor)
    {
        if (fromFloor <= 0 || floor < fromFloor)
        {
            return 0f;
        }
        return Mathf.Min(1f, _levelChanceAtStart + _levelChancePerFloor * (floor - fromFloor));
    }
}
