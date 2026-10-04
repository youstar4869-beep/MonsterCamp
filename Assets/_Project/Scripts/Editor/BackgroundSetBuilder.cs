using UnityEditor;
using UnityEngine;

// 배경 세트 에셋(Data/BackgroundSet)을 Art/Backgrounds 그림으로 채운다. 이미 있는 에셋은 빈 칸만 채운다(바꾼 구간 보호).
// 실행 중 층을 골라 전투 배경을 바로 보는 테스트 메뉴도 여기 있다
public static class BackgroundSetBuilder
{
    private const string AssetPath = "Assets/_Project/Data/BackgroundSet.asset";
    private const string ArtFolder = "Assets/_Project/Art/Backgrounds/";
    private const string TestMenu = "MonsterCamp/배경 확인 (실행 중)/";

    // 층 구간 (시작 층, 끝 층, 그림 이름) — GAME_DESIGN "5. 배경화면"
    private static readonly (int start, int end, string name)[] Ranges =
    {
        (1, 10, "Meadow"),
        (11, 20, "Forest"),
        (21, 30, "Beach"),
        (31, 50, "Volcano"),
        (51, 70, "Snow"),
        (71, 90, "Storm"),
        (91, 100, "Sky"),
    };

    [MenuItem("MonsterCamp/배경 세트 만들기")]
    public static void BuildMenu()
    {
        Selection.activeObject = GetOrCreate();
        Debug.Log("[배경] 준비 완료: " + AssetPath);
    }

    public static BackgroundSet GetOrCreate()
    {
        var set = AssetDatabase.LoadAssetAtPath<BackgroundSet>(AssetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<BackgroundSet>();
            AssetDatabase.CreateAsset(set, AssetPath);
        }

        var so = new SerializedObject(set);
        FillIfEmpty(so.FindProperty("_title"), "Title");
        FillIfEmpty(so.FindProperty("_shop"), "Shop");
        FillIfEmpty(so.FindProperty("_clouds"), "Battle_Clouds");
        SerializedProperty ranges = so.FindProperty("_battleRanges");
        if (ranges.arraySize == 0)
        {
            ranges.arraySize = Ranges.Length;
            for (int i = 0; i < Ranges.Length; i++)
            {
                SerializedProperty range = ranges.GetArrayElementAtIndex(i);
                range.FindPropertyRelative("StartFloor").intValue = Ranges[i].start;
                range.FindPropertyRelative("EndFloor").intValue = Ranges[i].end;
                range.FindPropertyRelative("Normal").objectReferenceValue = LoadSprite("Battle_" + Ranges[i].name);
                range.FindPropertyRelative("Boss").objectReferenceValue = LoadSprite("Battle_" + Ranges[i].name + "_Boss");
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
        return set;
    }

    private static void FillIfEmpty(SerializedProperty property, string spriteName)
    {
        if (property.objectReferenceValue == null)
        {
            property.objectReferenceValue = LoadSprite(spriteName);
        }
    }

    private static Sprite LoadSprite(string spriteName)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + spriteName + ".png");
        if (sprite == null)
        {
            Debug.LogWarning($"[배경] 그림이 없습니다: {ArtFolder}{spriteName}.png");
        }
        return sprite;
    }

    // ---------- 테스트: 실행 중 전투 화면을 띄우고 층 배경만 바꾼다 ----------

    [MenuItem(TestMenu + "1층 초원")] private static void Floor1() => ShowBattle(1, false);
    [MenuItem(TestMenu + "11층 숲")] private static void Floor11() => ShowBattle(11, false);
    [MenuItem(TestMenu + "21층 바닷가")] private static void Floor21() => ShowBattle(21, false);
    [MenuItem(TestMenu + "31층 화산")] private static void Floor31() => ShowBattle(31, false);
    [MenuItem(TestMenu + "51층 설원")] private static void Floor51() => ShowBattle(51, false);
    [MenuItem(TestMenu + "71층 폭풍")] private static void Floor71() => ShowBattle(71, false);
    [MenuItem(TestMenu + "91층 하늘")] private static void Floor91() => ShowBattle(91, false);
    [MenuItem(TestMenu + "보스 10층")] private static void Boss10() => ShowBattle(10, true);
    [MenuItem(TestMenu + "보스 50층")] private static void Boss50() => ShowBattle(50, true);
    [MenuItem(TestMenu + "보스 100층")] private static void Boss100() => ShowBattle(100, true);

    [MenuItem(TestMenu + "1층 초원", true)]
    [MenuItem(TestMenu + "11층 숲", true)]
    [MenuItem(TestMenu + "21층 바닷가", true)]
    [MenuItem(TestMenu + "31층 화산", true)]
    [MenuItem(TestMenu + "51층 설원", true)]
    [MenuItem(TestMenu + "71층 폭풍", true)]
    [MenuItem(TestMenu + "91층 하늘", true)]
    [MenuItem(TestMenu + "보스 10층", true)]
    [MenuItem(TestMenu + "보스 50층", true)]
    [MenuItem(TestMenu + "보스 100층", true)]
    private static bool IsPlaying() => Application.isPlaying;

    private static void ShowBattle(int floor, bool isBoss)
    {
        var battle = Object.FindFirstObjectByType<BattleScreen>(FindObjectsInactive.Include);
        var flow = Object.FindFirstObjectByType<ScreenFlow>();
        if (battle == null || flow == null)
        {
            Debug.LogWarning("[배경] Main 씬을 실행 중일 때만 쓸 수 있어요.");
            return;
        }
        flow.Show(GameScreen.Battle);
        battle.ShowBackground(floor, isBoss);
        Debug.Log($"[배경] {floor}층{(isBoss ? " 보스" : "")} 배경");
    }
}
