using System.Text;
using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > 크리처 스킬 발동 점검:
// 크리처 에셋 35종 각각 1마리(Lv1) 대 기본 적 2마리로 BattleSimulation을 돌려 스킬 발동 횟수를 찍는다 (0번이면 경고)
public static class SkillTestMenu
{
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";
    private const string TypeChartPath = "Assets/_Project/Data/TypeChart.asset";
    private const int Seed = 12345;

    [MenuItem("MonsterCamp/크리처 스킬 발동 점검")]
    public static void Run()
    {
        var typeChart = AssetDatabase.LoadAssetAtPath<TypeChart>(TypeChartPath);
        float[,] typeMultipliers = BattleSetupFactory.CreateTypeMultipliers(typeChart);

        string[] guids = AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder });
        var creatures = new CreatureData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            creatures[i] = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        System.Array.Sort(creatures, (a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : a.Type.CompareTo(b.Type));

        var log = new StringBuilder("[스킬 점검] 약한 적(이김) / 강한 적(쓰러짐) 발동 횟수\n");
        int zero = 0;
        foreach (CreatureData creature in creatures)
        {
            SkillTrial.Result result = SkillTrial.Run(BattleSetupFactory.Create(creature, 1, 0), typeMultipliers, Seed);
            log.Append($"{creature.Tier}골드 {creature.DisplayName} ({creature.SkillType}): " +
                       $"{result.WeakTriggers}회 / {result.StrongTriggers}회");
            if (creature.SkillType == SkillType.None)
            {
                log.Append("  ← 스킬 종류가 비어 있음 (MonsterCamp > 크리처 스킬 값 채우기)");
                zero++;
            }
            else if (result.Total == 0)
            {
                log.Append("  ← 발동 0번");
                zero++;
            }
            log.Append('\n');
        }
        log.Append($"{creatures.Length}종 중 문제 {zero}종");

        if (zero > 0)
        {
            Debug.LogWarning(log.ToString());
        }
        else
        {
            Debug.Log(log.ToString());
        }
    }
}
