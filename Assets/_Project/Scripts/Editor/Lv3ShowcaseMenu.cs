using UnityEditor;
using UnityEngine;

// 테스트용 메뉴: 3성 일러스트 연출 초기화 / 미리보기 / 임포트 설정 다시 적용
public static class Lv3ShowcaseMenu
{
    [MenuItem("MonsterCamp/3성 연출 초기화")]
    public static void ResetSeen()
    {
        // Play 중이면 게임이 들고 있는 값을 바꾼다 (파일만 고치면 게임이 나중에 덮어쓰므로)
        if (Application.isPlaying)
        {
            var meta = Object.FindFirstObjectByType<MetaProgress>();
            if (meta != null)
            {
                meta.ResetLv3Illustrations();
                Debug.Log("[Lv3] 3성 연출 초기화: 모든 크리처의 첫 ★★★ 연출이 다시 나옵니다");
                return;
            }
        }

        MetaSaveData data = SaveSystem.LoadMeta();
        data.seenLv3Illustrations?.Clear();
        SaveSystem.SaveMeta(data);
        Debug.Log("[Lv3] 3성 연출 초기화 (meta.json): 모든 크리처의 첫 ★★★ 연출이 다시 나옵니다");
    }

    // Project 창에서 크리처 에셋(Data/Creatures)이나 일러스트(xxx_Lv3.png)를 고르고 Play 중에 실행
    [MenuItem("MonsterCamp/3성 연출 미리보기 (선택한 크리처)")]
    public static void Preview()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[Lv3] Play 중에만 미리볼 수 있어요");
            return;
        }
        Object selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning("[Lv3] Project 창에서 크리처 에셋이나 xxx_Lv3 일러스트를 먼저 고르세요");
            return;
        }

        string key = selected.name.EndsWith("_Lv3") ? selected.name.Substring(0, selected.name.Length - 4) : selected.name;
        CreatureData creature = FindCreature(key);
        var showcase = Object.FindFirstObjectByType<Lv3Showcase>(FindObjectsInactive.Include);
        if (creature == null || showcase == null)
        {
            Debug.LogWarning(creature == null
                ? $"[Lv3] 영문키 '{key}' 크리처 에셋이 없어요"
                : "[Lv3] 씬에 Lv3Showcase가 없어요. MonsterCamp > Build Main Scene을 다시 실행하세요");
            return;
        }
        if (Resources.Load<Sprite>(key + "_Lv3") == null)
        {
            Debug.LogWarning($"[Lv3] '{key}_Lv3' 일러스트가 없어서 연출 없이 넘어갑니다 ({IllustrationImporter.Folder}Resources/)");
        }
        showcase.Preview(creature);
    }

    [MenuItem("MonsterCamp/3성 일러스트 임포트 설정 다시 적용")]
    public static void Reimport()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IllustrationImporter.Folder.TrimEnd('/') });
        foreach (string guid in guids)
        {
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
        }
        Debug.Log($"[Lv3] 일러스트 {guids.Length}장 다시 임포트");
    }

    private static CreatureData FindCreature(string key)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:CreatureData " + key))
        {
            var creature = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guid));
            if (creature != null && creature.name == key)
            {
                return creature;
            }
        }
        return null;
    }
}
