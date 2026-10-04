using UnityEditor;
using UnityEngine;

// 테스트용 메뉴: 조합 해금 ↔ 잠금 (meta.json). 발견·만듦 기록은 그대로 둔다
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
}
