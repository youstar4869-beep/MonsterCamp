using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > 크리처 그림 연결:
// Art/Creatures/{영문키}_Lv1~3.png 를 같은 이름의 CreatureData(Data/Creatures/{영문키}.asset)에 연결한다.
// 기본 그림 칸은 Lv1 그림으로 바꾼다. 임포트 설정이 다른 PNG는 먼저 고쳐서 다시 임포트한다
public static class CreatureSpriteLinker
{
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";

    [MenuItem("MonsterCamp/크리처 그림 연결")]
    public static void Link()
    {
        int reimported = FixImportSettings();
        // 3성 의인화를 쓰는 중이면 연결 후 다시 적용한다 (Lv3 칸이 32x32로 돌아가지 않게)
        bool lv3Woman = Lv3WomanMenu.IsApplied();

        int linked = 0;
        int missing = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder }))
        {
            var creature = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guid));
            var so = new SerializedObject(creature);
            SerializedProperty levelSprites = so.FindProperty("_levelSprites");
            levelSprites.arraySize = CreatureData.MaxLevel;

            bool complete = true;
            for (int level = 1; level <= CreatureData.MaxLevel; level++)
            {
                string path = $"{CreatureSpriteImporter.Folder}{creature.name}_Lv{level}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    Debug.LogWarning($"[크리처 그림] 없음: {path}");
                    complete = false;
                    missing++;
                    continue;
                }
                levelSprites.GetArrayElementAtIndex(level - 1).objectReferenceValue = sprite;
                if (level == 1)
                {
                    so.FindProperty("_sprite").objectReferenceValue = sprite;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            if (complete)
            {
                linked++;
            }
        }

        if (lv3Woman)
        {
            Lv3WomanMenu.Set(true);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[크리처 그림] 연결 완료: {linked}종 (그림 없는 칸 {missing}개, 임포트 설정 고친 PNG {reimported}장)");
    }

    // PNG가 스크립트보다 먼저 임포트됐으면 설정이 기본값일 수 있으므로 다시 맞춘다
    private static int FixImportSettings()
    {
        int count = 0;
        string folder = CreatureSpriteImporter.Folder.TrimEnd('/');
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetImporter.GetAtPath(path) is TextureImporter importer && CreatureSpriteImporter.Apply(importer))
            {
                importer.SaveAndReimport();
                count++;
            }
        }
        return count;
    }
}
