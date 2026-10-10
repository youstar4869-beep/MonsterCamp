using System.IO;
using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > 조합 데이터 만들기 로 조합 크리처 17종 + 히든 5종과 조합식 22개 에셋을 Data/Fusion 아래에 만들고,
// ShopConfig 조합식 목록에 없는 조합식을 뒤에 붙인다.
// 값은 GAME_DESIGN.md v0.2 "2. 조합식 17종"·"히든 조합" (★ 기준, 레벨 배율은 기존 크리처와 같음).
// 그림은 TODO 25 전까지 재료 A의 Lv1~3 그림을 임시로 쓴다 (히든은 재료 A 기본 그림만, 레벨별 칸은 비워 둠).
// 이미 있는 에셋은 덮어쓰지 않는다 (조정한 값 보호)
public static class FusionDataBuilder
{
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";
    private const string FusionCreatureFolder = "Assets/_Project/Data/Fusion/Creatures";
    private const string RecipeFolder = "Assets/_Project/Data/Fusion/Recipes";
    private const string ShopConfigPath = "Assets/_Project/Data/ShopConfig.asset";

    private struct Skill
    {
        public SkillType Type;
        public float Every;
        public float Chance;
        public float Value1;
        public float Value2;

        public Skill(SkillType type, float every, float chance, float value1, float value2)
        {
            Type = type;
            Every = every;
            Chance = chance;
            Value1 = value1;
            Value2 = value2;
        }
    }

    private struct Seed
    {
        public FusionGrade Grade;
        public string MaterialA;
        public string MaterialB;
        public string Id;
        public string Name;
        public CreatureType Type;
        // 교차 조합만 (없으면 null)
        public CreatureType? SecondType;
        public int Health;
        public int Attack;
        public float Speed;
        public string Description;
        public Skill Skill1;
        public Skill Skill2;
        // 히든 조합 (재료가 조합 크리처, 세기 1골드의 8배)
        public bool Hidden;
    }

    private static Skill S(SkillType type, float every = 0f, float chance = 0f, float value1 = 0f, float value2 = 0f)
    {
        return new Skill(type, every, chance, value1, value2);
    }

    private static readonly Skill NoSkill = new Skill(SkillType.None, 0f, 0f, 0f, 0f);

    private static readonly Seed[] Seeds =
    {
        // ---- 하급 ----
        new Seed { Grade = FusionGrade.Lower, MaterialA = "Embit", MaterialB = "MatchMouse", Id = "EmberImp", Name = "불씨도깨비",
            Type = CreatureType.Fire, Health = 225, Attack = 30, Speed = 1f,
            Description = "2번째 공격마다 피해 2배",
            Skill1 = S(SkillType.EveryNthAttackDamage, every: 2f, value1: 2f), Skill2 = NoSkill },
        new Seed { Grade = FusionGrade.Lower, MaterialA = "Plopper", MaterialB = "BubbleCrab", Id = "BubbleClaw", Name = "물방울집게",
            Type = CreatureType.Water, Health = 250, Attack = 25, Speed = 1f,
            Description = "받는 피해 15% 감소, 3번째 공격마다 체력 가장 낮은 아군 12% 회복",
            Skill1 = S(SkillType.DamageReduction, value1: 0.15f), Skill2 = S(SkillType.EveryNthAttackHealLowest, every: 3f, value1: 0.12f) },
        new Seed { Grade = FusionGrade.Lower, MaterialA = "Sproutee", MaterialB = "MossSnail", Id = "BloomSnail", Name = "꽃껍질달팽이",
            Type = CreatureType.Grass, Health = 300, Attack = 23, Speed = 1f,
            Description = "2초마다 자신 체력 6% 회복",
            Skill1 = S(SkillType.PeriodicSelfHeal, every: 2f, value1: 0.06f), Skill2 = NoSkill },
        new Seed { Grade = FusionGrade.Lower, MaterialA = "Zappy", MaterialB = "SparkSquirrel", Id = "VoltGlider", Name = "번개날다람쥐",
            Type = CreatureType.Electric, Health = 215, Attack = 23, Speed = 1.3f,
            Description = "전투 시작 후 3초간 공격 속도 2배, 공격 시 20% 확률로 한 번 더 공격",
            Skill1 = S(SkillType.BattleStartHaste, value1: 2f, value2: 3f), Skill2 = S(SkillType.OnHitChanceExtraAttack, chance: 0.2f) },
        new Seed { Grade = FusionGrade.Lower, MaterialA = "Pebblet", MaterialB = "SandMole", Id = "BoulderMoleKing", Name = "바위두더지왕",
            Type = CreatureType.Rock, Health = 350, Attack = 20, Speed = 0.75f,
            Description = "처음 받는 공격 2번 무시, 받는 피해 10% 감소",
            Skill1 = S(SkillType.IgnoreFirstHit, value1: 2f), Skill2 = S(SkillType.DamageReduction, value1: 0.1f) },

        // ---- 중급 ----
        new Seed { Grade = FusionGrade.Middle, MaterialA = "HearthBear", MaterialB = "CinderLizard", Id = "VolcanoBear", Name = "화산곰",
            Type = CreatureType.Fire, Health = 315, Attack = 42, Speed = 1f,
            Description = "공격한 적에게 3초 화상 (1초마다 공격력 30%), 체력이 50% 이하가 되면 공격력 +40%",
            Skill1 = S(SkillType.OnHitBurn, value1: 0.3f, value2: 3f), Skill2 = S(SkillType.LowHealthAttackUp, value1: 0.5f, value2: 0.4f) },
        new Seed { Grade = FusionGrade.Middle, MaterialA = "SprayOtter", MaterialB = "CoralSeahorse", Id = "WaveOtter", Name = "파도수달",
            Type = CreatureType.Water, Health = 350, Attack = 35, Speed = 1f,
            Description = "공격한 적의 공격 속도 20% 감소, 3번째 공격마다 체력 가장 낮은 아군 15% 회복",
            Skill1 = S(SkillType.OnHitSlow, value1: 0.2f), Skill2 = S(SkillType.EveryNthAttackHealLowest, every: 3f, value1: 0.15f) },
        new Seed { Grade = FusionGrade.Middle, MaterialA = "VineFox", MaterialB = "PetalMoth", Id = "BloomVineFox", Name = "꽃덩굴여우",
            Type = CreatureType.Grass, Health = 420, Attack = 32, Speed = 1f,
            Description = "공격 시 25% 확률로 2초 묶기, 25% 확률로 2초 공격력 30% 감소",
            Skill1 = S(SkillType.OnHitChanceRoot, chance: 0.25f, value2: 2f), Skill2 = S(SkillType.OnHitChanceWeaken, chance: 0.25f, value1: 0.3f, value2: 2f) },
        new Seed { Grade = FusionGrade.Middle, MaterialA = "FlashWeasel", MaterialB = "VoltEel", Id = "StormWeasel", Name = "뇌전족제비",
            Type = CreatureType.Electric, Health = 300, Attack = 32, Speed = 1.3f,
            Description = "공격 시 30% 확률로 한 번 더 공격, 25% 확률로 옆 적에게 공격력 60% 피해",
            Skill1 = S(SkillType.OnHitChanceExtraAttack, chance: 0.3f), Skill2 = S(SkillType.OnHitChanceSplash, chance: 0.25f, value1: 0.6f) },
        new Seed { Grade = FusionGrade.Middle, MaterialA = "GravelShell", MaterialB = "CrystalHog", Id = "CrystalArmor", Name = "수정갑주",
            Type = CreatureType.Rock, Health = 490, Attack = 28, Speed = 0.75f,
            Description = "전투 시작 시 자신 최대 체력 30% 보호막, 받은 공격 피해 25% 반사",
            Skill1 = S(SkillType.BattleStartSelfShield, value1: 0.3f), Skill2 = S(SkillType.Thorns, value1: 0.25f) },

        // ---- 교차 ----
        new Seed { Grade = FusionGrade.Cross, MaterialA = "LavaMane", MaterialB = "BedrockRhino", Id = "VolcanoRhino", Name = "화산코뿔소",
            Type = CreatureType.Rock, SecondType = CreatureType.Fire, Health = 630, Attack = 36, Speed = 0.75f,
            Description = "전투 시작 시 앞줄 적 2마리 2초 기절, 4번째 공격마다 적 전체에 공격력 60% 피해",
            Skill1 = S(SkillType.BattleStartStunFront, value1: 2f, value2: 2f), Skill2 = S(SkillType.EveryNthAttackHitAll, every: 4f, value1: 0.6f) },
        new Seed { Grade = FusionGrade.Cross, MaterialA = "TideWhale", MaterialB = "StormHawk", Id = "TempestWhale", Name = "폭풍고래",
            Type = CreatureType.Electric, SecondType = CreatureType.Water, Health = 385, Attack = 41, Speed = 1.3f,
            Description = "전투 시작 시 아군 전체 최대 체력 +15%, 3번째 공격마다 뒷줄 적 2마리에게 공격력 150% 피해",
            Skill1 = S(SkillType.BattleStartTeamMaxHealth, value1: 0.15f), Skill2 = S(SkillType.EveryNthAttackHitBackRow, every: 3f, value1: 1.5f, value2: 2f) },

        // ---- 상급 ----
        new Seed { Grade = FusionGrade.Upper, MaterialA = "BlazeWolf", MaterialB = "SunPhoenix", Id = "SolarWolf", Name = "일륜늑대",
            Type = CreatureType.Fire, Health = 540, Attack = 72, Speed = 1f,
            Description = "처음 쓰러지면 체력 50%로 부활, 적을 쓰러뜨릴 때마다 공격 속도 +30% (누적)",
            Skill1 = S(SkillType.Revive, value1: 0.5f), Skill2 = S(SkillType.OnKillAttackSpeed, value1: 0.3f) },
        new Seed { Grade = FusionGrade.Upper, MaterialA = "WhirlOctopus", MaterialB = "AbyssDragon", Id = "AbyssKraken", Name = "심연크라켄",
            Type = CreatureType.Water, Health = 600, Attack = 60, Speed = 1f,
            Description = "3번째 공격마다 적 전체에 공격력 60% 피해 + 아군 전체 12% 회복, 5초마다 적 전체 공격 속도 15% 감소 (5초)",
            Skill1 = S(SkillType.EveryNthAttackHitAllHealTeam, every: 3f, value1: 0.6f, value2: 0.12f),
            Skill2 = S(SkillType.PeriodicSlowAllEnemies, every: 5f, value1: 0.15f, value2: 5f) },
        new Seed { Grade = FusionGrade.Upper, MaterialA = "Flytrap", MaterialB = "ElderTreant", Id = "WorldTree", Name = "세계수",
            Type = CreatureType.Grass, Health = 720, Attack = 54, Speed = 1f,
            Description = "아군 전체 최대 체력 +30%, 1초마다 아군 전체 1.5% 회복, 전투 시작 시 뒷줄 적 2마리 3초 묶기",
            Skill1 = S(SkillType.TeamHealthAndRegen, every: 1f, value1: 0.3f, value2: 0.015f),
            Skill2 = S(SkillType.BattleStartRootBack, value1: 3f, value2: 2f) },
        new Seed { Grade = FusionGrade.Upper, MaterialA = "ThunderRam", MaterialB = "VoltQilin", Id = "HeavenQilin", Name = "천뢰기린",
            Type = CreatureType.Electric, Health = 510, Attack = 54, Speed = 1.3f,
            Description = "공격이 적 3마리에게 동시에, 4번째 공격마다 적 전체에 공격력 70% 번개",
            Skill1 = S(SkillType.MultiTarget, value1: 3f), Skill2 = S(SkillType.EveryNthAttackHitAll, every: 4f, value1: 0.7f) },
        new Seed { Grade = FusionGrade.Upper, MaterialA = "StoneGolem", MaterialB = "MountainTortoise", Id = "EarthTitan", Name = "대지거신",
            Type = CreatureType.Rock, Health = 840, Attack = 48, Speed = 0.75f,
            Description = "받는 피해 40% 감소, 전투 시작 시 아군 전체 최대 체력 15% 보호막, 쓰러지면 적 전체 2초 기절",
            Skill1 = S(SkillType.DamageReductionDeathStun, value1: 0.4f, value2: 2f), Skill2 = S(SkillType.BattleStartTeamShield, value1: 0.15f) },

        // ---- 히든 (재료 = 조합 크리처, 세기 8배: 1골드 체력 100·공격 10에 첫 타입 보정) ----
        new Seed { Grade = FusionGrade.Upper, Hidden = true, MaterialA = "SolarWolf", MaterialB = "VolcanoRhino", Id = "CrimsonEmperor", Name = "홍염제왕",
            Type = CreatureType.Fire, SecondType = CreatureType.Rock, Health = 720, Attack = 96, Speed = 1f,
            Description = "공격할 때 적 전체에 3초 화상 (1초마다 공격력 25%), 적을 쓰러뜨릴 때마다 공격력 +20% (누적)",
            Skill1 = S(SkillType.OnHitBurnAll, value1: 0.25f, value2: 3f), Skill2 = S(SkillType.OnKillAttackUp, value1: 0.2f) },
        new Seed { Grade = FusionGrade.Upper, Hidden = true, MaterialA = "AbyssKraken", MaterialB = "TempestWhale", Id = "OceanDragon", Name = "창해신룡",
            Type = CreatureType.Water, SecondType = CreatureType.Electric, Health = 800, Attack = 80, Speed = 1f,
            Description = "3번째 공격마다 적 전체에 공격력 80% 피해 + 공격 속도 25% 감소 (3초), 아군 전체 12% 회복",
            Skill1 = S(SkillType.EveryNthAttackHitAllHealTeam, every: 3f, value1: 0.8f, value2: 0.12f),
            Skill2 = S(SkillType.EveryNthAttackSlowAll, every: 3f, value1: 0.25f, value2: 3f) },
        new Seed { Grade = FusionGrade.Upper, Hidden = true, MaterialA = "WorldTree", MaterialB = "BloomVineFox", Id = "ForestEmpress", Name = "녹림여제",
            Type = CreatureType.Grass, Health = 960, Attack = 72, Speed = 1f,
            Description = "아군 전체 최대 체력 +40%, 쓰러진 아군 1마리를 체력 30%로 부활 (전투당 1번)",
            Skill1 = S(SkillType.BattleStartTeamMaxHealth, value1: 0.4f), Skill2 = S(SkillType.ReviveAlly, value1: 0.3f) },
        new Seed { Grade = FusionGrade.Upper, Hidden = true, MaterialA = "HeavenQilin", MaterialB = "StormWeasel", Id = "ThunderEmperor", Name = "뇌제",
            Type = CreatureType.Electric, Health = 680, Attack = 72, Speed = 1.3f,
            Description = "공격할 때 다른 적 전체에 공격력 50% 피해가 튀고, 20% 확률로 대상 1초 기절",
            Skill1 = S(SkillType.OnHitSplashAll, value1: 0.5f), Skill2 = S(SkillType.OnHitChanceStun, chance: 0.2f, value2: 1f) },
        new Seed { Grade = FusionGrade.Upper, Hidden = true, MaterialA = "EarthTitan", MaterialB = "CrystalArmor", Id = "CrystalTitanKing", Name = "수정거신왕",
            Type = CreatureType.Rock, Health = 1120, Attack = 64, Speed = 0.75f,
            Description = "받는 피해 50% 감소, 받은 공격 피해 30% 반사, 전투 시작 시 아군 전체 최대 체력 20% 보호막",
            Skill1 = S(SkillType.DamageReductionThorns, value1: 0.5f, value2: 0.3f), Skill2 = S(SkillType.BattleStartTeamShield, value1: 0.2f) },
    };

    [MenuItem("MonsterCamp/조합 데이터 만들기")]
    public static void Build()
    {
        Directory.CreateDirectory(FusionCreatureFolder);
        Directory.CreateDirectory(RecipeFolder);
        int created = 0;
        var recipes = new System.Collections.Generic.List<FusionRecipe>();
        foreach (Seed seed in Seeds)
        {
            CreatureData materialA = LoadMaterial(seed.MaterialA);
            CreatureData materialB = LoadMaterial(seed.MaterialB);
            if (materialA == null || materialB == null)
            {
                continue;
            }

            string creaturePath = $"{FusionCreatureFolder}/{seed.Id}.asset";
            var result = AssetDatabase.LoadAssetAtPath<CreatureData>(creaturePath);
            if (result == null)
            {
                result = ScriptableObject.CreateInstance<CreatureData>();
                AssetDatabase.CreateAsset(result, creaturePath);
                WriteCreature(result, seed, materialA);
                created++;
            }

            string recipePath = $"{RecipeFolder}/{seed.Id}Recipe.asset";
            var recipe = AssetDatabase.LoadAssetAtPath<FusionRecipe>(recipePath);
            if (recipe == null)
            {
                recipe = ScriptableObject.CreateInstance<FusionRecipe>();
                AssetDatabase.CreateAsset(recipe, recipePath);
                var so = new SerializedObject(recipe);
                so.FindProperty("_materialA").objectReferenceValue = materialA;
                so.FindProperty("_materialB").objectReferenceValue = materialB;
                so.FindProperty("_result").objectReferenceValue = result;
                so.FindProperty("_grade").enumValueIndex = (int)seed.Grade;
                so.FindProperty("_hidden").boolValue = seed.Hidden;
                so.ApplyModifiedPropertiesWithoutUndo();
                created++;
            }
            recipes.Add(recipe);
        }
        int added = AddToShopConfig(recipes);
        AssetDatabase.SaveAssets();
        Debug.Log($"[조합 데이터] 새로 만든 에셋 {created}개 ({FusionCreatureFolder}, {RecipeFolder}), ShopConfig에 조합식 {added}개 추가");
    }

    // ShopConfig 조합식 목록에 없는 조합식을 뒤에 붙인다 (앞 순서는 그대로: 적·봇 난수가 바뀌지 않게)
    private static int AddToShopConfig(System.Collections.Generic.List<FusionRecipe> recipes)
    {
        var shop = AssetDatabase.LoadAssetAtPath<ShopConfig>(ShopConfigPath);
        if (shop == null)
        {
            Debug.LogError($"[조합 데이터] ShopConfig가 없습니다: {ShopConfigPath}");
            return 0;
        }
        var so = new SerializedObject(shop);
        SerializedProperty list = so.FindProperty("_fusionRecipes");
        int added = 0;
        foreach (FusionRecipe recipe in recipes)
        {
            bool found = false;
            for (int i = 0; i < list.arraySize && !found; i++)
            {
                found = list.GetArrayElementAtIndex(i).objectReferenceValue == recipe;
            }
            if (!found)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = recipe;
                added++;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return added;
    }

    // 재료: 상점 크리처, 없으면 조합 크리처 (히든 조합의 재료)
    private static CreatureData LoadMaterial(string id)
    {
        var creature = AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{id}.asset")
                       ?? AssetDatabase.LoadAssetAtPath<CreatureData>($"{FusionCreatureFolder}/{id}.asset");
        if (creature == null)
        {
            Debug.LogError($"[조합 데이터] 재료 크리처가 없습니다: {id} ({CreatureFolder}, {FusionCreatureFolder})");
        }
        return creature;
    }

    private static void WriteCreature(CreatureData creature, Seed seed, CreatureData materialA)
    {
        var so = new SerializedObject(creature);
        so.FindProperty("_displayName").stringValue = seed.Name;
        so.FindProperty("_type").enumValueIndex = (int)seed.Type;
        // 등급(가격 칸)은 재료 중 높은 등급. 판매가(두 재료 합)는 2단계에서
        so.FindProperty("_tier").intValue = GetTier(seed.Grade);
        so.FindProperty("_hasSecondType").boolValue = seed.SecondType.HasValue;
        so.FindProperty("_secondType").enumValueIndex = (int)(seed.SecondType ?? seed.Type);
        so.FindProperty("_synergyCount").intValue = seed.Grade == FusionGrade.Upper ? 2 : 1;
        so.FindProperty("_fusionOnly").boolValue = true;
        so.FindProperty("_hidden").boolValue = seed.Hidden;
        so.FindProperty("_maxHealth").intValue = seed.Health;
        so.FindProperty("_attack").intValue = seed.Attack;
        so.FindProperty("_attackSpeed").floatValue = seed.Speed;
        so.FindProperty("_skillDescription").stringValue = seed.Description;
        WriteSkill(so, "_skill", seed.Skill1);
        WriteSkill(so, "_skill2", seed.Skill2);

        // 임시 그림: 재료 A의 그림 (TODO 25에서 교체). 히든은 기본 그림만, 레벨별 칸은 비워 둔다 (만든 뒤 f_hidden.py 그림으로 교체됨)
        var source = new SerializedObject(materialA);
        so.FindProperty("_sprite").objectReferenceValue = source.FindProperty("_sprite").objectReferenceValue;
        SerializedProperty sprites = so.FindProperty("_levelSprites");
        SerializedProperty sourceSprites = source.FindProperty("_levelSprites");
        sprites.arraySize = CreatureData.MaxLevel;
        for (int i = 0; i < CreatureData.MaxLevel; i++)
        {
            sprites.GetArrayElementAtIndex(i).objectReferenceValue =
                !seed.Hidden && i < sourceSprites.arraySize ? sourceSprites.GetArrayElementAtIndex(i).objectReferenceValue : null;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // prefix: "_skill" → _skillType, _skillEvery ... / "_skill2" → _skill2Type ...
    private static void WriteSkill(SerializedObject so, string prefix, Skill skill)
    {
        so.FindProperty(prefix + "Type").enumValueIndex = (int)skill.Type;
        so.FindProperty(prefix + "Every").floatValue = skill.Every;
        so.FindProperty(prefix + "Chance").floatValue = skill.Chance;
        so.FindProperty(prefix + "Value1").floatValue = skill.Value1;
        so.FindProperty(prefix + "Value2").floatValue = skill.Value2;
    }

    private static int GetTier(FusionGrade grade)
    {
        switch (grade)
        {
            case FusionGrade.Lower: return 1;
            case FusionGrade.Middle: return 2;
            case FusionGrade.Cross: return 3;
            default: return CreatureData.MaxTier;
        }
    }
}
