using System.Collections.Generic;
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
    [Tooltip("층마다 적 체력·공격력 증가 (0.03 = 3%). 시작 층까지는 그대로, n층은 1 + 값 × (n - 시작 층)배, 멈춤 층부터는 그 층 값")]
    [SerializeField, Min(0f)] private float _enemyStatPerFloor = 0.03f;
    [Tooltip("층마다 증가의 시작 층 (1이면 1층부터)")]
    [SerializeField, Min(1)] private int _enemyStatFromFloor = 1;
    [Tooltip("층마다 증가가 이 층에서 멈춤 (0이면 마지막 층까지)")]
    [SerializeField, Min(0)] private int _enemyStatToFloor;
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
    [Tooltip("확률 증가가 이 층에서 멈춤 (0이면 마지막 층까지)")]
    [SerializeField, Min(0)] private int _levelChanceToFloor;

    [Header("적 조합 크리처 (11층부터 자동 생성 팀)")]
    [Tooltip("이 층부터 적 팀에 조합 크리처가 섞임. 0이면 없음")]
    [SerializeField, Min(0)] private int _fusionFromFloor;
    [Tooltip("시작 층에서 한 마리가 같은 타입 조합 크리처로 바뀔 확률")]
    [SerializeField, Range(0f, 1f)] private float _fusionChanceAtStart = 0.1f;
    [Tooltip("시작 층 뒤로 층마다 더하는 확률")]
    [SerializeField, Min(0f)] private float _fusionChancePerFloor = 0.02f;
    [Tooltip("확률 최대")]
    [SerializeField, Range(0f, 1f)] private float _fusionChanceMax = 0.6f;

    [Header("보스 특수 능력 (비우면 없음)")]
    [Tooltip("보스 층마다 순서대로 1개 (첫 보스 층이 0번, 끝까지 쓰면 처음부터)")]
    [SerializeField] private BossAbility[] _bossAbilities;
    [Tooltip("마지막 층 보스는 이 능력들을 모두 (비우면 순서대로)")]
    [SerializeField] private BossAbility[] _finalBossAbilities;

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

    // 그 층 보스의 특수 능력 (보스 층이 아니거나 없으면 빈 목록)
    public IReadOnlyList<BossAbility> GetBossAbilities(int floor)
    {
        if (!IsBossFloor(floor))
        {
            return System.Array.Empty<BossAbility>();
        }
        if (floor >= _floorCount && _finalBossAbilities != null && _finalBossAbilities.Length > 0)
        {
            return _finalBossAbilities;
        }
        if (_bossAbilities == null || _bossAbilities.Length == 0)
        {
            return System.Array.Empty<BossAbility>();
        }
        return new[] { _bossAbilities[(floor / _bossEvery - 1) % _bossAbilities.Length] };
    }

    // 층마다 증가(시작 층~멈춤 층) × 보스 층 배율
    public float GetEnemyStatMultiplier(int floor) =>
        (1f + _enemyStatPerFloor * Mathf.Max(0, (_enemyStatToFloor > 0 ? Mathf.Min(floor, _enemyStatToFloor) : floor) - _enemyStatFromFloor)) * (IsBossFloor(floor) ? _bossStatMultiplier : 1f);

    // 자동 생성 팀 한 마리가 ★★ / ★★★일 확률 (시작 층 전이면 0)
    public float GetStar2Chance(int floor) => GetLevelChance(_star2FromFloor, floor);
    public float GetStar3Chance(int floor) => GetLevelChance(_star3FromFloor, floor);

    // 자동 생성 팀 한 마리가 조합 크리처로 바뀔 확률 (시작 층 전이면 0)
    public float GetEnemyFusionChance(int floor)
    {
        if (_fusionFromFloor <= 0 || floor < _fusionFromFloor)
        {
            return 0f;
        }
        return Mathf.Min(_fusionChanceMax, _fusionChanceAtStart + _fusionChancePerFloor * (floor - _fusionFromFloor));
    }

    private float GetLevelChance(int fromFloor, int floor)
    {
        if (fromFloor <= 0 || floor < fromFloor)
        {
            return 0f;
        }
        int to = _levelChanceToFloor > 0 ? Mathf.Min(floor, _levelChanceToFloor) : floor;
        return Mathf.Min(1f, _levelChanceAtStart + _levelChancePerFloor * Mathf.Max(0, to - fromFloor));
    }
}
