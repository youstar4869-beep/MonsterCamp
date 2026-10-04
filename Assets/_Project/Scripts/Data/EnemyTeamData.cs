using System;
using UnityEngine;

// 라운드 하나의 적 팀
[CreateAssetMenu(fileName = "EnemyTeam", menuName = "MonsterCamp/Enemy Team")]
public class EnemyTeamData : ScriptableObject
{
    [Serializable]
    public struct Member
    {
        public CreatureData Creature;
        [Range(1, CreatureData.MaxLevel)] public int Level;
    }

    [SerializeField] private string _displayName;
    [SerializeField] private bool _isBoss;
    [Tooltip("0~1 앞줄, 2~4 뒷줄. 빈 칸은 Creature를 비워 둔다")]
    [SerializeField] private Member[] _members = new Member[5];

    public string DisplayName => _displayName;
    public bool IsBoss => _isBoss;
    public int SlotCount => _members.Length;

    public Member GetMember(int slotIndex) => _members[slotIndex];

    // 자동 생성 팀 (EnemyTeamGenerator가 런타임 인스턴스에만 쓴다)
    public void SetGenerated(string displayName, bool isBoss, Member[] members)
    {
        _displayName = displayName;
        _isBoss = isBoss;
        _members = members;
    }
}
