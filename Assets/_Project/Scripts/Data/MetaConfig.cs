using System;
using UnityEngine;

// 메타 성장 설정: 별 받는 규칙, 잠긴 트레이너·크리처와 해금 가격.
// 목록에 없는 트레이너·크리처는 처음부터 열려 있다
[CreateAssetMenu(fileName = "MetaConfig", menuName = "MonsterCamp/Meta Config")]
public class MetaConfig : ScriptableObject
{
    [Serializable]
    public struct LockedTrainer
    {
        public TrainerData Trainer;
        public int Cost;
    }

    [Serializable]
    public struct LockedCreature
    {
        public CreatureData Creature;
        public int Cost;
    }

    [Header("별 받는 규칙 (판이 끝날 때 한꺼번에 더함)")]
    [Tooltip("라운드를 이길 때마다")]
    [SerializeField, Min(0)] private int _starsPerWin = 1;
    [Tooltip("마지막 라운드가 아닌 보스 라운드(5라운드)를 이기면 추가")]
    [SerializeField, Min(0)] private int _bossBonus = 3;
    [Tooltip("마지막 라운드(10라운드)를 이겨 클리어하면 추가")]
    [SerializeField, Min(0)] private int _clearBonus = 10;

    [Header("잠긴 것 (해금 가격 = 별)")]
    [SerializeField] private LockedTrainer[] _lockedTrainers;
    [SerializeField] private LockedCreature[] _lockedCreatures;

    public int StarsPerWin => _starsPerWin;
    public int BossBonus => _bossBonus;
    public int ClearBonus => _clearBonus;

    // 0이면 처음부터 열려 있음
    public int GetCost(TrainerData trainer)
    {
        foreach (LockedTrainer locked in _lockedTrainers)
        {
            if (locked.Trainer == trainer)
            {
                return locked.Cost;
            }
        }
        return 0;
    }

    public int GetCost(CreatureData creature)
    {
        foreach (LockedCreature locked in _lockedCreatures)
        {
            if (locked.Creature == creature)
            {
                return locked.Cost;
            }
        }
        return 0;
    }
}
