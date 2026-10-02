using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

// 크리처 등급 1~5골드 개편 (CREATURE_TIERS.md 확정안)을 기존 에셋에 적용하고, 규칙을 숫자로 점검한다
public static class TierUpdateMenu
{
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";
    private const string ShopConfigPath = "Assets/_Project/Data/ShopConfig.asset";
    private const string MetaConfigPath = "Assets/_Project/Data/MetaConfig.asset";

    // 5골드 중 3종은 각 별 10개로 잠근다 (지금 잠긴 3골드 3종은 푼다).
    // 태양불사조·심해룡은 처음부터 열림 (2026-10-02 처음 계정 밸런스, BALANCE_REPORT.md 3-1)
    private static readonly string[] LockedFiveGold =
        { "ElderTreant", "VoltQilin", "MountainTortoise" };
    private const int FiveGoldUnlockCost = 10;

    // CREATURE_TIERS.md 3번 표: (시작 라운드, 1~5골드 %)
    private static readonly (int fromRound, int[] percents)[] TierOdds =
    {
        (1, new[] { 75, 25, 0, 0, 0 }),
        (3, new[] { 50, 35, 15, 0, 0 }),
        (5, new[] { 30, 35, 25, 10, 0 }),
        (7, new[] { 20, 30, 30, 15, 5 }),
        (9, new[] { 15, 25, 30, 20, 10 }),
    };

    [MenuItem("MonsterCamp/크리처 등급 1~5골드 적용")]
    public static void Apply()
    {
        // 1) 새 20종 에셋 (기존 15종은 건너뜀) → 2) 35종 그림 연결
        CreatureDataBuilder.Build();
        CreatureSpriteLinker.Link();

        CreatureData[] creatures = LoadAllCreatures();
        UpdateShopConfig(creatures, out int added);
        UpdateMetaConfig();
        int teams = RoundDataBuilder.ApplyTierUpdate();

        AssetDatabase.SaveAssets();
        Debug.Log($"[등급 개편] 적용 완료: 크리처 {creatures.Length}종, 상점 목록에 {added}종 추가, " +
                  $"5골드 {LockedFiveGold.Length}종 잠금(별 {FiveGoldUnlockCost}), 적 팀 {teams}개 갱신");
    }

    // 상점 가격 {1~5}, 판매 배수 {1,3,9}, 라운드별 등급 확률, 크리처 목록에 없는 크리처 추가
    private static void UpdateShopConfig(CreatureData[] creatures, out int added)
    {
        added = 0;
        var config = AssetDatabase.LoadAssetAtPath<ShopConfig>(ShopConfigPath);
        if (config == null)
        {
            Debug.LogError("[등급 개편] ShopConfig가 없습니다. 먼저 Build Main Scene을 실행하세요.");
            return;
        }

        var so = new SerializedObject(config);
        SetInts(so.FindProperty("_tierPrices"), 1, 2, 3, 4, 5);
        SetInts(so.FindProperty("_levelSellMultipliers"), 1, 3, 9);

        SerializedProperty odds = so.FindProperty("_tierOdds");
        odds.arraySize = TierOdds.Length;
        for (int i = 0; i < TierOdds.Length; i++)
        {
            SerializedProperty element = odds.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("FromRound").intValue = TierOdds[i].fromRound;
            SetInts(element.FindPropertyRelative("Percents"), TierOdds[i].percents);
        }

        SerializedProperty pool = so.FindProperty("_creaturePool");
        var inPool = new HashSet<Object>();
        for (int i = 0; i < pool.arraySize; i++)
        {
            inPool.Add(pool.GetArrayElementAtIndex(i).objectReferenceValue);
        }
        foreach (CreatureData creature in creatures)
        {
            if (inPool.Contains(creature))
            {
                continue;
            }
            pool.arraySize++;
            pool.GetArrayElementAtIndex(pool.arraySize - 1).objectReferenceValue = creature;
            added++;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    // 잠긴 크리처를 5골드 5종으로 바꾼다. 이미 해금한 저장 데이터(meta.json)는 그대로 열려 있다
    private static void UpdateMetaConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath);
        if (config == null)
        {
            Debug.LogError("[등급 개편] MetaConfig가 없습니다. 먼저 Build Main Scene을 실행하세요.");
            return;
        }

        var so = new SerializedObject(config);
        SerializedProperty locked = so.FindProperty("_lockedCreatures");
        locked.arraySize = LockedFiveGold.Length;
        for (int i = 0; i < LockedFiveGold.Length; i++)
        {
            SerializedProperty element = locked.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("Creature").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{LockedFiveGold[i]}.asset");
            element.FindPropertyRelative("Cost").intValue = FiveGoldUnlockCost;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    // ---------- 점검 ----------

    // 상점 확률(라운드별 2000번 뽑기), 판매 가격, 5골드 시너지 계산을 Console에 찍는다
    [MenuItem("MonsterCamp/크리처 등급 규칙 점검")]
    public static void Check()
    {
        var shop = AssetDatabase.LoadAssetAtPath<ShopConfig>(ShopConfigPath);
        CreatureData[] creatures = LoadAllCreatures();
        var log = new StringBuilder("[등급 점검]\n");

        // 등급별 마리 수
        var perTier = new int[CreatureData.MaxTier + 1];
        foreach (CreatureData creature in creatures)
        {
            perTier[creature.Tier]++;
        }
        log.Append($"크리처 {creatures.Length}종 — 1골드 {perTier[1]}, 2골드 {perTier[2]}, 3골드 {perTier[3]}, " +
                   $"4골드 {perTier[4]}, 5골드 {perTier[5]}\n");

        // 상점 등급 확률 (잠금 무시, 칸 2000개)
        const int rolls = 2000;
        foreach (int round in new[] { 1, 3, 5, 7, 9 })
        {
            var counts = new int[CreatureData.MaxTier + 1];
            for (int i = 0; i < rolls; i++)
            {
                CreatureData picked = shop.GetRandomCreature(round, _ => true);
                if (picked != null)
                {
                    counts[picked.Tier]++;
                }
            }
            log.Append($"{round}라운드 상점: ");
            for (int tier = 1; tier <= CreatureData.MaxTier; tier++)
            {
                log.Append($"{tier}골드 {counts[tier] * 100f / rolls:0.#}%  ");
            }
            log.Append('\n');
        }

        // 판매 가격 = 구매 가격 × {1, 3, 9}
        foreach (int tier in new[] { 1, 3, 5 })
        {
            CreatureData sample = System.Array.Find(creatures, c => c.Tier == tier);
            if (sample == null)
            {
                continue;
            }
            log.Append($"판매 {sample.DisplayName}({tier}골드): ");
            for (int level = 1; level <= CreatureData.MaxLevel; level++)
            {
                log.Append($"Lv{level} {shop.GetSellPrice(new CreatureInstance(sample, level))}G  ");
            }
            log.Append('\n');
        }

        // 시너지: 5골드 1마리 = 2, 같은 크리처 중복은 1번만
        CreatureData fiveGold = System.Array.Find(creatures, c => c.Tier == CreatureData.MaxTier);
        CreatureData oneGold = System.Array.Find(creatures, c => c.Tier == 1 && fiveGold != null && c.Type == fiveGold.Type);
        if (fiveGold != null && oneGold != null)
        {
            var typeCounts = new int[TypeSynergy.TypeCount];
            TypeSynergy.CountUnique(new[] { fiveGold }, typeCounts);
            int single = typeCounts[(int)fiveGold.Type];
            TypeSynergy.CountUnique(new[] { fiveGold, fiveGold, oneGold }, typeCounts);
            int mixed = typeCounts[(int)fiveGold.Type];
            log.Append($"시너지 {TypeSynergy.GetTypeName(fiveGold.Type)}: {fiveGold.DisplayName} 1마리 = {single}, " +
                       $"{fiveGold.DisplayName} 2마리 + {oneGold.DisplayName} = {mixed} (기대값 2, 3)\n");
        }

        Debug.Log(log.ToString());
    }

    private static CreatureData[] LoadAllCreatures()
    {
        string[] guids = AssetDatabase.FindAssets("t:CreatureData", new[] { CreatureFolder });
        var creatures = new CreatureData[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            creatures[i] = AssetDatabase.LoadAssetAtPath<CreatureData>(AssetDatabase.GUIDToAssetPath(guids[i]));
        }
        return creatures;
    }

    private static void SetInts(SerializedProperty array, params int[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
        {
            array.GetArrayElementAtIndex(i).intValue = values[i];
        }
    }
}
