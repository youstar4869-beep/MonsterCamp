using UnityEngine;

// 한 판(런) 설정: 1~10층 적 팀, 11층부터 자동 생성 규칙 (층 수·목숨·적 레벨은 DifficultyData)
[CreateAssetMenu(fileName = "RunConfig", menuName = "MonsterCamp/Run Config")]
public class RunConfig : ScriptableObject
{
    [Tooltip("1~10층 적 팀 순서대로. 이 뒤의 층은 EnemyTeamGenerator가 만든다")]
    [SerializeField] private EnemyTeamData[] _rounds;

    [Header("자동 생성 팀 (11층부터)")]
    [Tooltip("적 마릿수 (앞줄 2 + 뒷줄 3까지)")]
    [SerializeField, Range(1, 5)] private int _teamSize = 5;
    [Tooltip("주 타입을 2개 고를 확률 (아니면 1개)")]
    [SerializeField, Range(0f, 1f)] private float _twoTypeChance = 0.5f;
    [Tooltip("주 타입 1개일 때 그 타입 마릿수 (나머지는 아무 타입)")]
    [SerializeField, Min(0)] private int _singleTypeCount = 4;
    [Tooltip("주 타입 2개일 때 첫 번째 / 두 번째 타입 마릿수")]
    [SerializeField, Min(0)] private int _firstTypeCount = 3;
    [SerializeField, Min(0)] private int _secondTypeCount = 2;
    [Tooltip("보스 층 1~5골드 확률(%). 상점 확률 대신 쓴다")]
    [SerializeField] private int[] _bossTierPercents = { 0, 0, 20, 45, 35 };
    [Tooltip("조합 시너지를 노린 팀 확률 (적 조합 시너지를 쓰는 난이도만)")]
    [SerializeField, Range(0f, 1f)] private float _mixedTeamChance = 0.5f;

    public int FixedTeamCount => _rounds.Length;
    public int TeamSize => _teamSize;
    public float TwoTypeChance => _twoTypeChance;
    public int SingleTypeCount => _singleTypeCount;
    public int FirstTypeCount => _firstTypeCount;
    public int SecondTypeCount => _secondTypeCount;
    public int[] BossTierPercents => _bossTierPercents;
    public float MixedTeamChance => _mixedTeamChance;

    // floor는 1부터 FixedTeamCount까지
    public EnemyTeamData GetFixedTeam(int floor) => _rounds[floor - 1];
}
