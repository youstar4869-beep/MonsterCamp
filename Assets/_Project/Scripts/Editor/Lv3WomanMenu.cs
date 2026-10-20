using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > 3성 의인화 적용 / 3성 원래대로:
// 모든 크리처(일반·조합·히든)의 Lv3 그림을 Art/Creatures/Lv3Woman/{영문키}_Lv3.png(48x48)로 바꾸거나
// 원래 32x32 Art/Creatures/{영문키}_Lv3.png로 되돌린다. 원래 파일은 지우지 않는다.
// 임포트 설정은 CreatureSpriteImporter가 하위 폴더까지 같은 값(Point, 압축 없음, PPU 32)으로 맞춘다
public static class Lv3WomanMenu
{
    public const string Folder = CreatureSpriteImporter.Folder + "Lv3Woman/";
    private static readonly string[] DataFolders = { "Assets/_Project/Data/Creatures", "Assets/_Project/Data/Fusion/Creatures" };
    private const int Lv3Index = CreatureData.MaxLevel - 1;

    [MenuItem("MonsterCamp/3성 의인화 적용")]
    public static void ApplyMenu() => Debug.Log($"[3성 의인화] 적용: {Set(true)}종");

    [MenuItem("MonsterCamp/3성 원래대로")]
    public static void RevertMenu() => Debug.Log($"[3성 의인화] 원래대로: {Set(false)}종");

    // 지금 의인화 그림을 쓰는 크리처가 하나라도 있으면 true (빌더를 다시 돌린 뒤 유지할지 판단)
    public static bool IsApplied()
    {
        foreach (CreatureData creature in LoadAll())
        {
            Sprite sprite = Lv3Sprite(creature);
            if (sprite != null && AssetDatabase.GetAssetPath(sprite).StartsWith(Folder))
            {
                return true;
            }
        }
        return false;
    }

    // woman: true면 의인화, false면 원래 그림. 바꾼 크리처 수
    public static int Set(bool woman)
    {
        int changed = 0;
        foreach (CreatureData creature in LoadAll())
        {
            string path = (woman ? Folder : CreatureSpriteImporter.Folder) + creature.name + "_Lv3.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[3성 의인화] 그림 없음: {path}");
                continue;
            }
            var so = new SerializedObject(creature);
            SerializedProperty sprites = so.FindProperty("_levelSprites");
            sprites.arraySize = CreatureData.MaxLevel;
            SerializedProperty lv3 = sprites.GetArrayElementAtIndex(Lv3Index);
            if (lv3.objectReferenceValue == sprite)
            {
                continue;
            }
            lv3.objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
            changed++;
        }
        AssetDatabase.SaveAssets();
        return changed;
    }

    private static Sprite Lv3Sprite(CreatureData creature)
    {
        var sprites = new SerializedObject(creature).FindProperty("_levelSprites");
        return sprites.arraySize > Lv3Index ? sprites.GetArrayElementAtIndex(Lv3Index).objectReferenceValue as Sprite : null;
    }

    private static System.Collections.Generic.IEnumerable<CreatureData> LoadAll()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:CreatureData", DataFolders))
        {
            var creature = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guid));
            if (creature != null)
            {
                yield return creature;
            }
        }
    }
}
