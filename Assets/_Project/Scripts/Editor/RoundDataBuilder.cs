using System.IO;
using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > Create Round Data 로
// 라운드별 적 팀 10개 + 런 설정(RunConfig)을 만든다.
// 값은 GAME_DESIGN.md 7-1 초안. 이미 있는 에셋은 덮어쓰지 않는다 (조정한 값 보호).
public static class RoundDataBuilder
{
    private const string CreatureFolder = "Assets/_Project/Data/Creatures";
    private const string EnemyTeamFolder = "Assets/_Project/Data/EnemyTeams";
    private const string RunConfigPath = "Assets/_Project/Data/RunConfig.asset";
    private const int SlotCount = 5;

    private struct RoundSeed
    {
        public string Name;
        public bool IsBoss;
        // 5칸: 0~1 앞줄, 2~4 뒷줄. "크리처id:레벨", null은 빈 칸
        public string[] Slots;

        public RoundSeed(string name, bool isBoss, params string[] slots)
        {
            Name = name;
            IsBoss = isBoss;
            Slots = slots;
        }
    }

    private static readonly RoundSeed[] Rounds =
    {
        new RoundSeed("새싹 들판", false, "Sproutee:1", null, "Plopper:1", null, null),
        new RoundSeed("불씨 언덕", false, "Embit:1", "Pebblet:1", "Zappy:1", null, null),
        new RoundSeed("개울가 무리", false, "Pebblet:1", "Sproutee:1", "Plopper:1", "Zappy:1", null),
        new RoundSeed("숲 어귀", false, "HearthBear:1", "VineFox:1", "Embit:1", "SprayOtter:1", null),
        new RoundSeed("바위 성채", true, "GravelShell:2", "Pebblet:2", "Embit:2", "LavaMane:1", "Plopper:2"),
        new RoundSeed("물안개 호수", false, "HearthBear:2", "VineFox:1", "FlashWeasel:1", "SprayOtter:2", null),
        new RoundSeed("천둥 협곡", false, "GravelShell:2", "VineFox:2", "Zappy:2", "SprayOtter:2", "Embit:2"),
        new RoundSeed("깊은 숲", false, "BedrockRhino:1", "HearthBear:2", "FlashWeasel:2", "TideWhale:1", "StormHawk:1"),
        new RoundSeed("용암 동굴", false, "GravelShell:2", "GroveStag:1", "LavaMane:2", "FlashWeasel:2", "SprayOtter:2"),
        new RoundSeed("폭풍 왕좌", true, "BedrockRhino:2", "GroveStag:2", "LavaMane:2", "StormHawk:2", "TideWhale:2"),
    };

    [MenuItem("MonsterCamp/Create Round Data")]
    public static void Build()
    {
        Directory.CreateDirectory(EnemyTeamFolder);
        AssetDatabase.Refresh();

        int created = 0;
        var teams = new EnemyTeamData[Rounds.Length];
        for (int i = 0; i < Rounds.Length; i++)
        {
            string path = $"{EnemyTeamFolder}/Round{i + 1:00}.asset";
            teams[i] = AssetDatabase.LoadAssetAtPath<EnemyTeamData>(path);
            if (teams[i] == null)
            {
                teams[i] = CreateEnemyTeam(Rounds[i], path);
                created++;
            }
        }

        if (AssetDatabase.LoadAssetAtPath<RunConfig>(RunConfigPath) == null)
        {
            CreateRunConfig(teams);
            created++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"라운드 데이터 생성 완료: 새로 만듦 {created}개 (이미 있는 에셋은 건너뜀)");
    }

    private static EnemyTeamData CreateEnemyTeam(RoundSeed seed, string path)
    {
        var team = ScriptableObject.CreateInstance<EnemyTeamData>();
        AssetDatabase.CreateAsset(team, path);

        var so = new SerializedObject(team);
        so.FindProperty("_displayName").stringValue = seed.Name;
        so.FindProperty("_isBoss").boolValue = seed.IsBoss;
        SerializedProperty members = so.FindProperty("_members");
        members.arraySize = SlotCount;
        for (int i = 0; i < SlotCount; i++)
        {
            SerializedProperty member = members.GetArrayElementAtIndex(i);
            string slot = i < seed.Slots.Length ? seed.Slots[i] : null;
            if (slot == null)
            {
                member.FindPropertyRelative("Creature").objectReferenceValue = null;
                member.FindPropertyRelative("Level").intValue = 1;
                continue;
            }

            string[] parts = slot.Split(':');
            member.FindPropertyRelative("Creature").objectReferenceValue = LoadCreature(parts[0]);
            member.FindPropertyRelative("Level").intValue = int.Parse(parts[1]);
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return team;
    }

    private static void CreateRunConfig(EnemyTeamData[] teams)
    {
        var config = ScriptableObject.CreateInstance<RunConfig>();
        AssetDatabase.CreateAsset(config, RunConfigPath);

        var so = new SerializedObject(config);
        SerializedProperty rounds = so.FindProperty("_rounds");
        rounds.arraySize = teams.Length;
        for (int i = 0; i < teams.Length; i++)
        {
            rounds.GetArrayElementAtIndex(i).objectReferenceValue = teams[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static CreatureData LoadCreature(string id)
    {
        var creature = AssetDatabase.LoadAssetAtPath<CreatureData>($"{CreatureFolder}/{id}.asset");
        if (creature == null)
        {
            Debug.LogError($"크리처를 찾을 수 없습니다: {id}. 먼저 MonsterCamp > Create Initial Creature Data 를 실행하세요.");
        }
        return creature;
    }
}
