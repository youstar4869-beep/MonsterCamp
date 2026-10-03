using UnityEngine;

// 한 판(런) 설정: 층별 적 팀 (층 수·목숨은 DifficultyData)
[CreateAssetMenu(fileName = "RunConfig", menuName = "MonsterCamp/Run Config")]
public class RunConfig : ScriptableObject
{
    [Tooltip("1~10층 적 팀 순서대로. 마지막 팀은 보스 층에 쓴다")]
    [SerializeField] private EnemyTeamData[] _rounds;

    // floor는 1부터. 임시 규칙 (TODO 22에서 자동 생성으로 바꿈):
    // 보스 층은 마지막 팀, 나머지는 (층 - 1) % 팀 수 + 1번째 팀
    public EnemyTeamData GetEnemyTeam(int floor, bool isBoss)
    {
        return isBoss ? _rounds[_rounds.Length - 1] : _rounds[(floor - 1) % _rounds.Length];
    }
}
