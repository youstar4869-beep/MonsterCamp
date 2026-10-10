using UnityEditor;
using UnityEngine;

// 테스트용 메뉴: 조합 해금 ↔ 잠금, 히든 조합 해금·발견·초기화 (meta.json)
public static class FusionMenu
{
    [MenuItem("MonsterCamp/조합 해금·잠금 전환")]
    public static void Toggle()
    {
        // Play 중이면 게임이 들고 있는 값을 바꾼다 (파일만 고치면 게임이 나중에 덮어쓰므로)
        if (Application.isPlaying)
        {
            var meta = Object.FindFirstObjectByType<MetaProgress>();
            if (meta != null)
            {
                meta.SetFusionUnlocked(!meta.FusionUnlocked);
                Debug.Log($"[Save] 조합 {(meta.FusionUnlocked ? "해금" : "잠금")} (상점 화면은 다음 동작 때 반영)");
                return;
            }
        }

        MetaSaveData data = SaveSystem.LoadMeta();
        data.fusionUnlocked = !data.fusionUnlocked;
        SaveSystem.SaveMeta(data);
        Debug.Log($"[Save] 조합 {(data.fusionUnlocked ? "해금" : "잠금")} (meta.json)");
    }

    // 테스트용 메뉴: 조합 해금 + 조합식 전부 발견 (도감 조합식 탭·상점 조합식 창 확인용)
    [MenuItem("MonsterCamp/조합 해금 + 조합식 전부 발견")]
    public static void UnlockAndDiscoverAll()
    {
        string[] guids = AssetDatabase.FindAssets("t:FusionRecipe");
        var recipes = new FusionRecipe[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            recipes[i] = AssetDatabase.LoadAssetAtPath<FusionRecipe>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }

        // Play 중이면 게임이 들고 있는 값을 바꾼다 (파일만 고치면 게임이 나중에 덮어쓰므로)
        if (Application.isPlaying)
        {
            var meta = Object.FindFirstObjectByType<MetaProgress>();
            if (meta != null)
            {
                meta.SetFusionUnlocked(true);
                foreach (FusionRecipe recipe in recipes)
                {
                    meta.SetDiscovered(recipe);
                }
                Debug.Log($"[Save] 조합 해금 + 조합식 {recipes.Length}종 발견");
                return;
            }
        }

        MetaSaveData data = SaveSystem.LoadMeta();
        data.fusionUnlocked = true;
        foreach (FusionRecipe recipe in recipes)
        {
            if (!data.discoveredRecipes.Contains(recipe.name))
            {
                data.discoveredRecipes.Add(recipe.name);
            }
        }
        SaveSystem.SaveMeta(data);
        Debug.Log($"[Save] 조합 해금 + 조합식 {recipes.Length}종 발견 (meta.json)");
    }

    // 히든 조합 해금 (조합도 같이 해금). 발견 기록은 그대로
    [MenuItem("MonsterCamp/히든 조합/히든 해금")]
    public static void UnlockHidden()
    {
        EditHidden(false, false, "히든 해금");
    }

    // 히든 해금 + 히든 조합식 5종 발견
    [MenuItem("MonsterCamp/히든 조합/히든 전부 발견")]
    public static void DiscoverAllHidden()
    {
        EditHidden(true, false, "히든 해금 + 히든 조합식 전부 발견");
    }

    // 히든 잠금 + 히든 조합식 발견·만듦 기록 지우기 (일반 조합은 그대로)
    [MenuItem("MonsterCamp/히든 조합/히든 초기화")]
    public static void ResetHidden()
    {
        EditHidden(false, true, "히든 초기화 (잠금, 발견 기록 지움)");
    }

    private static void EditHidden(bool discover, bool reset, string label)
    {
        FusionRecipe[] hidden = System.Array.FindAll(LoadRecipes(), recipe => recipe.Hidden);

        // Play 중이면 게임이 들고 있는 값을 바꾼다 (파일만 고치면 게임이 나중에 덮어쓰므로)
        if (Application.isPlaying)
        {
            var meta = Object.FindFirstObjectByType<MetaProgress>();
            if (meta != null)
            {
                if (reset)
                {
                    meta.ResetHidden(hidden);
                }
                else
                {
                    meta.SetFusionUnlocked(true);
                    meta.SetHiddenFusionUnlocked(true);
                    foreach (FusionRecipe recipe in discover ? hidden : new FusionRecipe[0])
                    {
                        meta.SetDiscovered(recipe);
                    }
                }
                Debug.Log($"[Save] {label}, 히든 {hidden.Length}종 (상점 화면은 다음 동작 때 반영)");
                return;
            }
        }

        MetaSaveData data = SaveSystem.LoadMeta();
        if (reset)
        {
            data.hiddenFusionUnlocked = false;
            foreach (FusionRecipe recipe in hidden)
            {
                data.discoveredRecipes.Remove(recipe.name);
                data.madeRecipes.Remove(recipe.name);
            }
        }
        else
        {
            data.fusionUnlocked = true;
            data.hiddenFusionUnlocked = true;
            foreach (FusionRecipe recipe in discover ? hidden : new FusionRecipe[0])
            {
                if (!data.discoveredRecipes.Contains(recipe.name))
                {
                    data.discoveredRecipes.Add(recipe.name);
                }
            }
        }
        SaveSystem.SaveMeta(data);
        Debug.Log($"[Save] {label}, 히든 {hidden.Length}종 (meta.json)");
    }

    private static FusionRecipe[] LoadRecipes()
    {
        string[] guids = AssetDatabase.FindAssets("t:FusionRecipe");
        var recipes = new FusionRecipe[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            recipes[i] = AssetDatabase.LoadAssetAtPath<FusionRecipe>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        return System.Array.FindAll(recipes, recipe => recipe != null);
    }
}
