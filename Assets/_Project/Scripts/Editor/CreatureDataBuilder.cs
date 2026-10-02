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
        // 스킬 효과 (칸의 뜻은 SkillType 설명 참고)
        public SkillType SkillType;
        public float SkillEvery;
        public float SkillChance;
        public float SkillValue1;
        public float SkillValue2;

        public Seed(string id, string name, CreatureType type, int tier, int health, int attack, float speed, string skill,
            SkillType skillType, float skillEvery, float skillChance, float skillValue1, float skillValue2)
        {
            Id = id;
            Name = name;
            Type = type;
            Tier = tier;
            Health = health;
            Attack = attack;
            Speed = speed;
            Skill = skill;
            SkillType = skillType;
            SkillEvery = skillEvery;
            SkillChance = skillChance;
            SkillValue1 = skillValue1;
            SkillValue2 = skillValue2;
        }
    }

    private static readonly Seed[] Seeds =
    {
        new Seed("Embit", "불똥이", CreatureType.Fire, 1, 95, 12, 1f, "3번째 공격마다 피해 1.5배", SkillType.EveryNthAttackDamage, 3f, 0f, 1.5f, 0f),
        new Seed("HearthBear", "화로곰", CreatureType.Fire, 2, 163, 21, 1f, "체력이 50% 이하가 되면 공격력 +30%", SkillType.LowHealthAttackUp, 0f, 0f, 0.5f, 0.3f),
        new Seed("LavaMane", "용암갈기", CreatureType.Fire, 3, 237, 31, 1f, "4번째 공격마다 적 전체에 공격력 50% 피해", SkillType.EveryNthAttackHitAll, 4f, 0f, 0.5f, 0f),

        new Seed("Plopper", "퐁당이", CreatureType.Water, 1, 94, 10, 1f, "3번째 공격마다 체력이 가장 낮은 아군 체력 10% 회복", SkillType.EveryNthAttackHealLowest, 3f, 0f, 0.1f, 0f),
        new Seed("SprayOtter", "물보라수달", CreatureType.Water, 2, 180, 18, 1f, "공격한 적의 공격 속도 10% 감소 (중첩 없음)", SkillType.OnHitSlow, 0f, 0f, 0.1f, 0f),
        new Seed("TideWhale", "해일고래", CreatureType.Water, 3, 265, 26, 1f, "전투 시작 시 아군 전체 최대 체력 +15%", SkillType.BattleStartTeamMaxHealth, 0f, 0f, 0.15f, 0f),

        new Seed("Sproutee", "새싹토리", CreatureType.Grass, 1, 120, 9, 1f, "3초마다 자신 체력 5% 회복", SkillType.PeriodicSelfHeal, 3f, 0f, 0.05f, 0f),
        new Seed("VineFox", "덩굴여우", CreatureType.Grass, 2, 213, 16, 1f, "공격 시 15% 확률로 대상 2초 묶기 (공격 불가)", SkillType.OnHitChanceRoot, 0f, 0.15f, 0f, 2f),
        new Seed("GroveStag", "숲뿔사슴", CreatureType.Grass, 3, 320, 24, 1f, "쓰러질 때 아군 전체 체력 20% 회복", SkillType.OnDeathHealTeam, 0f, 0f, 0.2f, 0f),

        new Seed("Zappy", "찌릿새", CreatureType.Electric, 1, 85, 9, 1.3f, "전투 시작 후 2초간 공격 속도 2배", SkillType.BattleStartHaste, 0f, 0f, 2f, 2f),
        new Seed("FlashWeasel", "번쩍족제비", CreatureType.Electric, 2, 151, 16, 1.3f, "공격 시 25% 확률로 한 번 더 공격", SkillType.OnHitChanceExtraAttack, 0f, 0.25f, 0f, 0f),
        new Seed("StormHawk", "뇌운매", CreatureType.Electric, 3, 226, 22, 1.3f, "3번째 공격마다 뒷줄 적 1마리에게 공격력 150% 피해", SkillType.EveryNthAttackHitBackRow, 3f, 0f, 1.5f, 0f),

        new Seed("Pebblet", "돌멩군", CreatureType.Rock, 1, 158, 8, 0.75f, "받는 피해 10% 감소", SkillType.DamageReduction, 0f, 0f, 0.1f, 0f),
        new Seed("GravelShell", "자갈갑옷", CreatureType.Rock, 2, 253, 15, 0.75f, "처음 체력 50% 이하가 될 때 최대 체력 20% 보호막", SkillType.LowHealthShield, 0f, 0f, 0.5f, 0.2f),
        new Seed("BedrockRhino", "암반코뿔소", CreatureType.Rock, 3, 397, 21, 0.75f, "전투 시작 시 앞줄 적 1마리 2초 기절", SkillType.BattleStartStunFront, 0f, 0f, 2f, 0f),

        // ---- 1~5골드 등급 추가 20종 (CREATURE_TIERS.md 5·6번) ----
        // 1골드
        new Seed("MatchMouse", "성냥쥐", CreatureType.Fire, 1, 90, 13, 1f, "첫 공격 피해 2배", SkillType.FirstAttackDamage, 0f, 0f, 2f, 0f),
        new Seed("BubbleCrab", "거품게", CreatureType.Water, 1, 110, 9, 1f, "받는 피해 10% 감소", SkillType.DamageReduction, 0f, 0f, 0.1f, 0f),
        new Seed("MossSnail", "이끼달팽이", CreatureType.Grass, 1, 135, 7, 1f, "체력이 높고 공격이 약함, 5초마다 체력 5% 회복", SkillType.PeriodicSelfHeal, 5f, 0f, 0.05f, 0f),
        new Seed("SparkSquirrel", "찌릿다람쥐", CreatureType.Electric, 1, 80, 9, 1.3f, "공격 속도 +15%", SkillType.AttackSpeedUp, 0f, 0f, 0.15f, 0f),
        new Seed("SandMole", "모래두더지", CreatureType.Rock, 1, 145, 9, 0.75f, "처음 받는 공격 피해 무시", SkillType.IgnoreFirstHit, 0f, 0f, 0f, 0f),
        // 2골드
        new Seed("CinderLizard", "숯불도마뱀", CreatureType.Fire, 2, 166, 21, 1f, "공격한 적에게 3초 동안 화상(초당 작은 피해)", SkillType.OnHitBurn, 0f, 0f, 0.25f, 3f),
        new Seed("CoralSeahorse", "산호해마", CreatureType.Water, 2, 174, 17, 1f, "3번째 공격마다 체력이 가장 낮은 아군 회복", SkillType.EveryNthAttackHealLowest, 3f, 0f, 0.1f, 0f),
        new Seed("PetalMoth", "꽃잎나방", CreatureType.Grass, 2, 196, 16, 1f, "공격 시 20% 확률로 적 공격력 2초 감소", SkillType.OnHitChanceWeaken, 0f, 0.2f, 0.3f, 2f),
        new Seed("VoltEel", "전기뱀장어", CreatureType.Electric, 2, 146, 17, 1.3f, "공격 시 25% 확률로 옆 적에게 튀는 피해", SkillType.OnHitChanceSplash, 0f, 0.25f, 0.5f, 0f),
        new Seed("CrystalHog", "수정고슴도치", CreatureType.Rock, 2, 242, 15, 0.75f, "받은 피해의 15% 반사", SkillType.Thorns, 0f, 0f, 0.15f, 0f),
        // 4골드
        new Seed("BlazeWolf", "불꽃늑대", CreatureType.Fire, 4, 360, 43, 1f, "적을 쓰러뜨리면 공격 속도 +30% (누적)", SkillType.OnKillAttackSpeed, 0f, 0f, 0.3f, 0f),
        new Seed("WhirlOctopus", "소용돌이문어", CreatureType.Water, 4, 353, 35, 1f, "5초마다 적 전체 공격 속도 15% 감소", SkillType.PeriodicSlowAllEnemies, 5f, 0f, 0.15f, 5f),
        new Seed("Flytrap", "덩굴파리지옥", CreatureType.Grass, 4, 400, 31, 1f, "전투 시작 시 뒷줄 적 1마리를 3초 묶음", SkillType.BattleStartRootBack, 0f, 0f, 3f, 0f),
        new Seed("ThunderRam", "천둥양", CreatureType.Electric, 4, 306, 32, 1.3f, "4번째 공격마다 적 전체에 번개 피해", SkillType.EveryNthAttackHitAll, 4f, 0f, 0.6f, 0f),
        new Seed("StoneGolem", "바위골렘", CreatureType.Rock, 4, 530, 28, 0.75f, "전투 시작 시 아군 전체에 최대 체력 12% 보호막", SkillType.BattleStartTeamShield, 0f, 0f, 0.12f, 0f),
        // 5골드 (타입 시너지에서 2마리로 계산)
        new Seed("SunPhoenix", "태양불사조", CreatureType.Fire, 5, 309, 38, 1f, "처음 쓰러지면 체력 50%로 한 번 부활", SkillType.Revive, 0f, 0f, 0.5f, 0f),
        new Seed("AbyssDragon", "심해룡", CreatureType.Water, 5, 345, 31, 1f, "3번째 공격마다 적 전체 피해 + 아군 전체 회복", SkillType.EveryNthAttackHitAllHealTeam, 3f, 0f, 0.5f, 0.1f),
        new Seed("ElderTreant", "고목거인", CreatureType.Grass, 5, 380, 30, 1f, "아군 전체 최대 체력 +25%, 1초마다 아군 전체 회복", SkillType.TeamHealthAndRegen, 1f, 0f, 0.25f, 0.01f),
        new Seed("VoltQilin", "뇌광기린", CreatureType.Electric, 5, 360, 33, 1.3f, "공격이 적 3마리에게 동시에 맞음", SkillType.MultiTarget, 0f, 0f, 3f, 0f),
        new Seed("MountainTortoise", "산맥거북", CreatureType.Rock, 5, 515, 31, 0.75f, "받는 피해 40% 감소, 쓰러지면 적 전체 기절 1.7초", SkillType.DamageReductionDeathStun, 0f, 0f, 0.4f, 1.7f),
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
        WriteSkill(so, seed);
        so.FindProperty("_sprite").objectReferenceValue = CreatePlaceholderSprite(seed);
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    // 메뉴 MonsterCamp > 크리처 스킬 값 채우기:
    // 이미 있는 크리처 에셋에 스킬 종류·수치만 넣는다 (체력·공격력·설명 등 다른 값은 그대로)
    [MenuItem("MonsterCamp/크리처 스킬 값 채우기")]
    public static void FillSkillValues()
    {
        int filled = 0;
        foreach (Seed seed in Seeds)
        {
            var data = AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{seed.Id}.asset");
            if (data == null)
            {
                Debug.LogWarning($"[스킬] 크리처 에셋이 없습니다: {seed.Id}");
                continue;
            }
            var so = new SerializedObject(data);
            WriteSkill(so, seed);
            so.ApplyModifiedPropertiesWithoutUndo();
            filled++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[스킬] 스킬 값 채우기 완료: {filled}종 (다른 값은 바꾸지 않음)");
    }

    private static void WriteSkill(SerializedObject so, Seed seed)
    {
        so.FindProperty("_skillType").enumValueIndex = (int)seed.SkillType;
        so.FindProperty("_skillEvery").floatValue = seed.SkillEvery;
        so.FindProperty("_skillChance").floatValue = seed.SkillChance;
        so.FindProperty("_skillValue1").floatValue = seed.SkillValue1;
        so.FindProperty("_skillValue2").floatValue = seed.SkillValue2;
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
