using System.IO;
using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > Create Initial Creature Data 로
// 타입 상성표 + 초기 크리처 15종 에셋 + 임시 색깔 네모 그림을 만든다.
// 값은 GAME_DESIGN.md 6-1 초안. 이미 있는 에셋은 덮어쓰지 않는다 (조정한 값 보호).
public static class CreatureDataBuilder
{
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";
    private const string TypeChartPath = "Assets/_Project/Data/TypeChart.asset";
    private const string SpriteFolder = "Assets/_Project/Art/Placeholders/Creatures";
    private const int SpriteSize = 64;
    private const int BorderSize = 4;

    private struct Seed
    {
        public string Id;
        public string Name;
        public CreatureType Type;
        public int Tier;
        public int Health;
        public int Attack;
        public float Speed;
        public string Skill;

        public Seed(string id, string name, CreatureType type, int tier, int health, int attack, float speed, string skill)
        {
            Id = id;
            Name = name;
            Type = type;
            Tier = tier;
            Health = health;
            Attack = attack;
            Speed = speed;
            Skill = skill;
        }
    }

    private static readonly Seed[] Seeds =
    {
        new Seed("Embit", "불똥이", CreatureType.Fire, 1, 90, 12, 1f, "3번째 공격마다 피해 1.5배"),
        new Seed("HearthBear", "화로곰", CreatureType.Fire, 2, 145, 19, 1f, "체력이 50% 이하가 되면 공격력 +30%"),
        new Seed("LavaMane", "용암갈기", CreatureType.Fire, 3, 215, 29, 1f, "4번째 공격마다 적 전체에 공격력 50% 피해"),

        new Seed("Plopper", "퐁당이", CreatureType.Water, 1, 100, 10, 1f, "3번째 공격마다 체력이 가장 낮은 아군 체력 10% 회복"),
        new Seed("SprayOtter", "물보라수달", CreatureType.Water, 2, 160, 16, 1f, "공격한 적의 공격 속도 10% 감소 (중첩 없음)"),
        new Seed("TideWhale", "해일고래", CreatureType.Water, 3, 240, 24, 1f, "전투 시작 시 아군 전체 최대 체력 +15%"),

        new Seed("Sproutee", "새싹토리", CreatureType.Grass, 1, 120, 9, 1f, "3초마다 자신 체력 5% 회복"),
        new Seed("VineFox", "덩굴여우", CreatureType.Grass, 2, 190, 14, 1f, "공격 시 15% 확률로 대상 2초 묶기 (공격 불가)"),
        new Seed("GroveStag", "숲뿔사슴", CreatureType.Grass, 3, 290, 22, 1f, "쓰러질 때 아군 전체 체력 20% 회복"),

        new Seed("Zappy", "찌릿새", CreatureType.Electric, 1, 85, 9, 1.3f, "전투 시작 후 3초간 공격 속도 2배"),
        new Seed("FlashWeasel", "번쩍족제비", CreatureType.Electric, 2, 135, 14, 1.3f, "공격 시 25% 확률로 한 번 더 공격"),
        new Seed("StormHawk", "뇌운매", CreatureType.Electric, 3, 205, 22, 1.3f, "3번째 공격마다 뒷줄 적 1마리에게 공격력 150% 피해"),

        new Seed("Pebblet", "돌멩군", CreatureType.Rock, 1, 140, 8, 0.75f, "받는 피해 10% 감소"),
        new Seed("GravelShell", "자갈갑옷", CreatureType.Rock, 2, 225, 13, 0.75f, "처음 체력 50% 이하가 될 때 최대 체력 20% 보호막"),
        new Seed("BedrockRhino", "암반코뿔소", CreatureType.Rock, 3, 335, 19, 0.75f, "전투 시작 시 앞줄 적 1마리 2초 기절"),
    };

    // 불 > 풀 > 물 > 불, 번개 > 물, 바위 > 번개
    private static readonly CreatureType[,] Advantages =
    {
        { CreatureType.Fire, CreatureType.Grass },
        { CreatureType.Grass, CreatureType.Water },
        { CreatureType.Water, CreatureType.Fire },
        { CreatureType.Electric, CreatureType.Water },
        { CreatureType.Rock, CreatureType.Electric },
    };

    [MenuItem("MonsterCamp/Create Initial Creature Data")]
    public static void Build()
    {
        Directory.CreateDirectory(CreatureFolder);
        Directory.CreateDirectory(SpriteFolder);
        AssetDatabase.Refresh();

        int created = 0;
        int skipped = 0;

        if (CreateTypeChart())
        {
            created++;
        }
        else
        {
            skipped++;
        }

        foreach (Seed seed in Seeds)
        {
            if (CreateCreature(seed))
            {
                created++;
            }
            else
            {
                skipped++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"크리처 데이터 생성 완료: 새로 만듦 {created}개, 이미 있어서 건너뜀 {skipped}개");
    }

    private static bool CreateTypeChart()
    {
        if (AssetDatabase.LoadAssetAtPath<TypeChart>(TypeChartPath) != null)
        {
            return false;
        }

        var chart = ScriptableObject.CreateInstance<TypeChart>();
        AssetDatabase.CreateAsset(chart, TypeChartPath);

        var so = new SerializedObject(chart);
        SerializedProperty list = so.FindProperty("_advantages");
        int count = Advantages.GetLength(0);
        list.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            SerializedProperty element = list.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Strong").enumValueIndex = (int)Advantages[i, 0];
            element.FindPropertyRelative("Weak").enumValueIndex = (int)Advantages[i, 1];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    private static bool CreateCreature(Seed seed)
    {
        string path = $"{CreatureFolder}/{seed.Id}.asset";
        if (AssetDatabase.LoadAssetAtPath<CreatureData>(path) != null)
        {
            return false;
        }

        var data = ScriptableObject.CreateInstance<CreatureData>();
        AssetDatabase.CreateAsset(data, path);

        var so = new SerializedObject(data);
        so.FindProperty("_displayName").stringValue = seed.Name;
        so.FindProperty("_type").enumValueIndex = (int)seed.Type;
        so.FindProperty("_tier").intValue = seed.Tier;
        so.FindProperty("_maxHealth").intValue = seed.Health;
        so.FindProperty("_attack").intValue = seed.Attack;
        so.FindProperty("_attackSpeed").floatValue = seed.Speed;
        so.FindProperty("_skillDescription").stringValue = seed.Skill;
        so.FindProperty("_sprite").objectReferenceValue = CreatePlaceholderSprite(seed);
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // 타입 색 네모 + 테두리. 티어가 높을수록 진하다.
    private static Sprite CreatePlaceholderSprite(Seed seed)
    {
        string path = $"{SpriteFolder}/{seed.Id}.png";

        if (!File.Exists(path))
        {
            Color fill = GetTypeColor(seed.Type);
            if (seed.Tier == 1)
            {
                fill = Color.Lerp(fill, Color.white, 0.35f);
            }
            else if (seed.Tier == 3)
            {
                fill = Color.Lerp(fill, Color.black, 0.35f);
            }
            Color border = Color.Lerp(fill, Color.black, 0.5f);

            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    bool isBorder = x < BorderSize || y < BorderSize ||
                                    x >= SpriteSize - BorderSize || y >= SpriteSize - BorderSize;
                    texture.SetPixel(x, y, isBorder ? border : fill);
                }
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = SpriteSize;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Color GetTypeColor(CreatureType type)
    {
        switch (type)
        {
            case CreatureType.Fire: return new Color(0.90f, 0.25f, 0.20f);
            case CreatureType.Water: return new Color(0.20f, 0.45f, 0.90f);
            case CreatureType.Grass: return new Color(0.25f, 0.75f, 0.30f);
            case CreatureType.Electric: return new Color(0.95f, 0.85f, 0.20f);
            default: return new Color(0.55f, 0.45f, 0.35f);
        }
    }
}
