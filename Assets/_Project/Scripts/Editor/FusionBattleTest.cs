using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > 조합 크리처 전투 테스트: 조합식마다 조합 크리처 ★★ 1마리 vs 재료 두 마리 ★★ (조합에 쓰는 것과 같은 레벨).
// 앞줄 첫 칸끼리, 타입 시너지만 (조합 시너지·트레이너·아이템 없음). 반은 조합 쪽이 내 팀, 반은 적 팀 (먼저 계산되는 쪽 유리함 제거)
// 히든 조합은 추가로 같은 골드대 팀(재료의 재료 4마리 ★★, 판매가 합이 같음)과도 붙여 본다
public static class FusionBattleTest
{
    private const string DataFolder = "Assets/_Project/Data";
    private const string RecipeFolder = DataFolder + "/Fusion/Recipes";
    private const int Battles = 40;
    private const int Level = 2;
    private const float MaxSeconds = 180f;

    [MenuItem("MonsterCamp/조합 크리처 전투 테스트")]
    public static void Run()
    {
        Debug.Log(Simulate());
    }

    // 배치 모드: Unity.exe -batchmode -projectPath <프로젝트> -executeMethod FusionBattleTest.RunBatch
    public static void RunBatch()
    {
        int exitCode = 0;
        try
        {
            Debug.Log(Simulate());
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            exitCode = 1;
        }
        EditorApplication.Exit(exitCode);
    }

    private static string Simulate()
    {
        var synergy = AssetDatabase.LoadAssetAtPath<SynergyConfig>($"{DataFolder}/SynergyConfig.asset");
        float[,] multipliers = BattleSetupFactory.CreateTypeMultipliers(
            AssetDatabase.LoadAssetAtPath<TypeChart>($"{DataFolder}/TypeChart.asset"));
        FusionRecipe[] recipes = AssetDatabase.FindAssets("t:FusionRecipe", new[] { RecipeFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<FusionRecipe>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(recipe => recipe != null && recipe.Result != null)
            .OrderBy(recipe => recipe.Grade).ThenBy(recipe => recipe.Result.Type).ToArray();

        var fusion = new List<BattleSetupFactory.Member>();
        var materials = new List<BattleSetupFactory.Member>();
        var text = new StringBuilder($"[조합 크리처 전투 테스트] 조합 ★★ 1마리 vs 재료 두 마리 ★★, 조합식마다 {Battles}판\n");
        text.AppendLine("등급 | 조합 크리처 | 재료 | 조합 승률 | 평균 시간 | 오류");
        int errors = 0;
        foreach (FusionRecipe recipe in recipes)
        {
            fusion.Clear();
            fusion.Add(new BattleSetupFactory.Member { Creature = recipe.Result, Level = Level, SlotIndex = 0 });
            materials.Clear();
            materials.Add(new BattleSetupFactory.Member { Creature = recipe.MaterialA, Level = Level, SlotIndex = 0 });
            materials.Add(new BattleSetupFactory.Member { Creature = recipe.MaterialB, Level = Level, SlotIndex = 1 });

            float wins = 0f;
            float seconds = 0f;
            int recipeErrors = 0;
            for (int i = 0; i < Battles; i++)
            {
                bool fusionIsPlayer = i % 2 == 0;
                try
                {
                    float playerScore = Fight(fusionIsPlayer ? fusion : materials, fusionIsPlayer ? materials : fusion,
                        synergy, multipliers, 1000 + i, out float elapsed);
                    wins += fusionIsPlayer ? playerScore : 1f - playerScore;
                    seconds += elapsed;
                }
                catch (System.Exception exception)
                {
                    recipeErrors++;
                    Debug.LogException(exception);
                }
            }
            errors += recipeErrors;
            text.AppendLine($"{recipe.GradeName} | {recipe.Result.DisplayName} | " +
                            $"{recipe.MaterialA.DisplayName}+{recipe.MaterialB.DisplayName} | {wins / Battles * 100f:0}% | " +
                            $"{seconds / Battles:0.0}초 | {recipeErrors}");
        }
        text.AppendLine($"조합식 {recipes.Length}개, 오류 {errors}건");
        errors += SimulateHidden(recipes, synergy, multipliers, text);
        text.AppendLine($"전체 오류 {errors}건");
        return text.ToString();
    }

    // 히든 ★★ 1마리 vs 같은 골드대 팀: 재료 두 조합 크리처의 재료 4마리 ★★ (앞줄 2, 뒷줄 2)
    private static int SimulateHidden(FusionRecipe[] recipes, SynergyConfig synergy, float[,] multipliers, StringBuilder text)
    {
        text.AppendLine($"[히든 조합] 히든 ★★ 1마리 vs 같은 골드대 팀 (재료의 재료 4마리 ★★), {Battles}판");
        text.AppendLine("히든 | 상대 팀 | 히든 승률 | 평균 시간 | 오류");
        var hidden = new List<BattleSetupFactory.Member>();
        var team = new List<BattleSetupFactory.Member>();
        int errors = 0;
        int count = 0;
        foreach (FusionRecipe recipe in recipes.Where(r => r.Hidden))
        {
            FusionRecipe recipeA = recipes.FirstOrDefault(r => r.Result == recipe.MaterialA);
            FusionRecipe recipeB = recipes.FirstOrDefault(r => r.Result == recipe.MaterialB);
            if (recipeA == null || recipeB == null)
            {
                text.AppendLine($"{recipe.Result.DisplayName} | 재료 조합식을 못 찾음 | - | - | 1");
                errors++;
                continue;
            }
            count++;
            hidden.Clear();
            hidden.Add(new BattleSetupFactory.Member { Creature = recipe.Result, Level = Level, SlotIndex = 0 });
            team.Clear();
            CreatureData[] members = { recipeA.MaterialA, recipeA.MaterialB, recipeB.MaterialA, recipeB.MaterialB };
            for (int i = 0; i < members.Length; i++)
            {
                team.Add(new BattleSetupFactory.Member { Creature = members[i], Level = Level, SlotIndex = i });
            }

            float wins = 0f;
            float seconds = 0f;
            int recipeErrors = 0;
            for (int i = 0; i < Battles; i++)
            {
                bool hiddenIsPlayer = i % 2 == 0;
                try
                {
                    float playerScore = Fight(hiddenIsPlayer ? hidden : team, hiddenIsPlayer ? team : hidden,
                        synergy, multipliers, 2000 + i, out float elapsed);
                    wins += hiddenIsPlayer ? playerScore : 1f - playerScore;
                    seconds += elapsed;
                }
                catch (System.Exception exception)
                {
                    recipeErrors++;
                    Debug.LogException(exception);
                }
            }
            errors += recipeErrors;
            text.AppendLine($"{recipe.Result.DisplayName} | {string.Join("+", members.Select(m => m.DisplayName))} | " +
                            $"{wins / Battles * 100f:0}% | {seconds / Battles:0.0}초 | {recipeErrors}");
        }
        text.AppendLine($"히든 {count}종, 오류 {errors}건");
        return errors;
    }

    // 내 팀 승리 1, 패배 0, 시간 초과 0.5
    private static float Fight(List<BattleSetupFactory.Member> players, List<BattleSetupFactory.Member> enemies,
        SynergyConfig synergy, float[,] multipliers, int seed, out float elapsed)
    {
        List<BattleUnitSetup> playerSetups = BattleSetupFactory.CreateTeam(players, synergy, null, false);
        List<BattleUnitSetup> enemySetups = BattleSetupFactory.CreateTeam(enemies, synergy, null, false);
        var simulation = new BattleSimulation(playerSetups, enemySetups, multipliers, seed);
        simulation.Start();
        while (!simulation.IsOver && simulation.ElapsedTime < MaxSeconds)
        {
            simulation.Tick(BalanceSimulator.BattleStep);
        }
        elapsed = simulation.ElapsedTime;
        return !simulation.IsOver ? 0.5f : simulation.PlayerWon ? 1f : 0f;
    }
}
