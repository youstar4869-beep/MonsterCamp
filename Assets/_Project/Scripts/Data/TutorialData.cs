using System;
using UnityEngine;

// 튜토리얼 단계. 단계별 동작(비출 곳, 넘어가는 조건)은 TutorialController에 있다
public enum TutorialStep
{
    // 1라운드 상점
    BuyCreature,
    PlaceInTeam,
    StartBattle,
    // 1라운드 전투
    BattleInfo,
    // 2라운드 상점
    MergeInfo,
    Merge,
    SellInfo,
    SynergyInfo
}

// 튜토리얼 문구와 미리 정해 둔 상점·적 팀 (문구를 고치려면 이 에셋을 고친다)
[CreateAssetMenu(fileName = "TutorialData", menuName = "MonsterCamp/Tutorial Data")]
public class TutorialData : ScriptableObject
{
    [Serializable]
    public struct StepText
    {
        public TutorialStep Step;
        [TextArea(2, 4)] public string Message;
    }

    [Tooltip("진행 순서대로")]
    [SerializeField] private StepText[] _steps;

    [Header("미리 정해 둔 상점 (5칸)")]
    [Tooltip("1라운드에 사고 2라운드에 합성할 크리처")]
    [SerializeField] private CreatureData _mergeCreature;
    [SerializeField] private CreatureData[] _round1Shop;
    [Tooltip("_mergeCreature가 2칸 이상 있어야 합성할 수 있다")]
    [SerializeField] private CreatureData[] _round2Shop;

    [Header("약한 적 팀 (라운드 순서)")]
    [SerializeField] private EnemyTeamData[] _enemyTeams;

    public int StepCount => _steps.Length;
    public CreatureData MergeCreature => _mergeCreature;

    public TutorialStep GetStep(int index) => _steps[index].Step;
    public string GetMessage(int index) => _steps[index].Message;

    // round는 1부터. 정해 둔 것이 없으면 null
    public CreatureData[] GetShop(int round)
    {
        switch (round)
        {
            case 1: return _round1Shop;
            case 2: return _round2Shop;
            default: return null;
        }
    }

    public EnemyTeamData GetEnemyTeam(int round)
    {
        return round >= 1 && round <= _enemyTeams.Length ? _enemyTeams[round - 1] : null;
    }
}
