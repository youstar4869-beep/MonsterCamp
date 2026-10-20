using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

// 메뉴 MonsterCamp > 크리처 그림 점검: 모든 크리처(일반·조합·히든)의 Lv1~3 그림 연결·파일 이름·임포트 설정·
// Sprite Atlas 포함·투명 그림과 3성 일러스트 파일을 확인해 Console에 적는다 (아무것도 고치지 않음)
public static class CreatureSpriteCheckMenu
{
    private const string AtlasPath = "Assets/_Project/Art/CreatureAtlas.spriteatlasv2";
    private static readonly string[] DataFolders = { "Assets/_Project/Data/Creatures", "Assets/_Project/Data/Fusion/Creatures" };

    [MenuItem("MonsterCamp/크리처 그림 점검")]
    public static void Check()
    {
        var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
        if (atlas != null)
        {
            SpriteAtlasUtility.PackAtlases(new[] { atlas }, EditorUserBuildSettings.activeBuildTarget);
        }

        int count = 0;
        int problems = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:CreatureData", DataFolders))
        {
            var creature = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guid));
            count++;
            for (int level = 1; level <= CreatureData.MaxLevel; level++)
            {
                string problem = CheckSprite(creature, level, atlas);
                if (problem != null)
                {
                    problems++;
                    Debug.LogWarning($"[그림 점검] {creature.name} ({creature.DisplayName}) Lv{level}: {problem}", creature);
                }
            }
            if (Resources.Load<Sprite>(creature.name + "_Lv3") == null)
            {
                problems++;
                Debug.LogWarning($"[그림 점검] {creature.name} ({creature.DisplayName}): 3성 일러스트 없음 " +
                                 $"({IllustrationImporter.Folder}Resources/{creature.name}_Lv3.png)", creature);
            }
        }
        Debug.Log($"[그림 점검] 크리처 {count}종 확인, 문제 {problems}개" + (atlas == null ? $" (Sprite Atlas 없음: {AtlasPath})" : ""));
    }

    private static string CheckSprite(CreatureData creature, int level, SpriteAtlas atlas)
    {
        Sprite sprite = creature.GetSprite(level);
        if (sprite == null)
        {
            return "그림 칸이 비어 있음";
        }
        string path = AssetDatabase.GetAssetPath(sprite);
        if (!path.EndsWith($"/{creature.name}_Lv{level}.png"))
        {
            return $"다른 이름의 그림이 연결됨: {path}";
        }
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer) || importer.textureType != TextureImporterType.Sprite)
        {
            return $"Sprite로 임포트되지 않음: {path}";
        }
        if (atlas != null && !atlas.CanBindTo(sprite))
        {
            return $"Sprite Atlas에 없음: {path}";
        }
        var texture = new Texture2D(2, 2);
        texture.LoadImage(System.IO.File.ReadAllBytes(path));
        int opaque = 0;
        foreach (Color32 pixel in texture.GetPixels32())
        {
            opaque += pixel.a > 10 ? 1 : 0;
        }
        Object.DestroyImmediate(texture);
        return opaque < 20 ? $"거의 투명한 그림: {path}" : null;
    }
}
