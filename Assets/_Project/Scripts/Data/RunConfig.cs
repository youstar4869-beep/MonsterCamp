using UnityEngine;

// 한 판(런) 설정: 목숨, 라운드별 적 팀
[CreateAssetMenu(fileName = "RunConfig", menuName = "MonsterCamp/Run Config")]
public class RunConfig : ScriptableObject
{
    [SerializeField, Min(1)] private int _maxLives = 3;
    [Tooltip("라운드 순서대로. 개수 = 총 라운드 수")]
    [SerializeField] private EnemyTeamData[] _rounds;

    public int MaxLives => _maxLives;
    public int RoundCount => _rounds.Length;

    // round는 1부터
    public EnemyTeamData GetEnemyTeam(int round) => _rounds[round - 1];
}
