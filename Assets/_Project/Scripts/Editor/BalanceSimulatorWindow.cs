using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// 메뉴 MonsterCamp > 밸런스 시뮬레이터: 판 수, 시드, 봇을 고르고 "실행"을 누르면
// 화면 없이 한 판 전체를 여러 번 돌려 요약을 창에 보여 주고, 자세한 내용은 프로젝트 폴더의 Balance/에 CSV로 저장한다
public class BalanceSimulatorWindow : EditorWindow
{
    private enum BotChoice
    {
        All,
        Random,
        Basic,
        Synergy,
        Saver
    }

    // 프로젝트 폴더(Assets 옆) 기준
    private const string OutputFolder = "Balance";
    // 봇마다 앞쪽 몇 판을 라운드별로 기록 (sample_games.txt)
    private const int SampleGames = 3;
    // Data/Difficulty 안 에셋 이름
    private static readonly string[] Difficulties = { "Normal", "Hard", "Impossible" };

    [SerializeField] private int _games = 1000;
    [SerializeField] private int _seed = 12345;
    [SerializeField] private BotChoice _bot = BotChoice.All;
    [SerializeField] private bool _duels = true;
    [SerializeField] private bool _locked;
    [SerializeField] private bool _fusion;
    [SerializeField] private int _difficulty;
    [SerializeField] private string _summary = "";
    private Vector2 _scroll;

    [MenuItem("MonsterCamp/밸런스 시뮬레이터")]
    public static void Open()
    {
        GetWindow<BalanceSimulatorWindow>("밸런스 시뮬레이터");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("봇이 한 판 전체(난이도의 1층~마지막 층, 목숨 3)를 화면 없이 반복합니다.", EditorStyles.wordWrappedLabel);
        _games = Mathf.Max(1, EditorGUILayout.IntField("판 수 (봇마다)", _games));
        _seed = EditorGUILayout.IntField("시드", _seed);
        _difficulty = EditorGUILayout.Popup("난이도", _difficulty, new[] { "보통", "어려움", "불가능" });
        _bot = (BotChoice)EditorGUILayout.Popup("봇", (int)_bot, new[] { "전체 4종", "랜덤", "기본", "시너지", "저축" });
        _duels = EditorGUILayout.Toggle("1:1 대결표·등급 가치 포함", _duels);
        _locked = EditorGUILayout.Toggle(
            new GUIContent("처음 계정처럼 잠금", "MetaConfig에서 잠긴 트레이너·크리처(5골드)를 빼고 돌린다"), _locked);
        _fusion = EditorGUILayout.Toggle(
            new GUIContent("조합 해금", "봇이 조합을 쓴다 (보통 클리어 후 계정). 어려움·불가능은 원래 해금된 뒤에 열림"), _fusion);

        if (GUILayout.Button("실행", GUILayout.Height(32)))
        {
            Run();
        }
        EditorGUILayout.LabelField($"CSV 저장 위치: {OutputFolder}/ (프로젝트 폴더)", EditorStyles.miniLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.TextArea(_summary, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Run()
    {
        try
        {
            _summary = Simulate(_games, _seed, ToBots(_bot), _duels, _locked, _fusion, Difficulties[_difficulty], OutputFolder, (label, progress) =>
                EditorUtility.DisplayCancelableProgressBar("밸런스 시뮬레이터", label, progress));
            Debug.Log("[밸런스] 시뮬레이션 완료\n" + _summary);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    private static BalanceBotKind[] ToBots(BotChoice choice)
    {
        switch (choice)
        {
            case BotChoice.Random: return new[] { BalanceBotKind.Random };
            case BotChoice.Basic: return new[] { BalanceBotKind.Basic };
            case BotChoice.Synergy: return new[] { BalanceBotKind.Synergy };
            case BotChoice.Saver: return new[] { BalanceBotKind.Saver };
            default: return (BalanceBotKind[])System.Enum.GetValues(typeof(BalanceBotKind));
        }
    }

    // progress(글, 0~1)가 true를 돌려주면 남은 봇을 건너뛴다
    private static string Simulate(int games, int seed, BalanceBotKind[] bots, bool duels, bool locked, bool fusion, string difficulty,
        string folder, System.Func<string, float, bool> progress)
    {
        var simulator = new BalanceSimulator(locked, difficulty, fusion);
        var results = new List<BalanceSimulator.BotStats>();
        bool cancelled = false;
        foreach (BalanceBotKind bot in bots)
        {
            string label = $"{BalanceSimulator.BotName(bot)} 봇 {games}판";
            results.Add(simulator.RunGames(bot, games, seed, rate => cancelled = progress(label, rate), SampleGames));
            if (cancelled)
            {
                break;
            }
        }

        List<BalanceSimulator.DuelResult> duelResults = null;
        List<BalanceSimulator.TierValueResult> tierValue = null;
        if (duels && !cancelled)
        {
            progress("1:1 대결표", 0f);
            duelResults = simulator.RunDuels(seed);
            progress("등급 가치", 0.5f);
            tierValue = simulator.RunTierValue(seed);
        }

        string summary = $"난이도 {difficulty} · 판 수 {games} · 시드 {seed} · {(locked ? "처음 계정처럼 잠금" : "모두 해금")} · 조합 {(fusion ? "해금" : "잠금")}" +
                         $"{(cancelled ? " · 중간에 멈춤" : "")}\n" +
                         simulator.BuildSummary(results, duelResults, tierValue);
        simulator.WriteCsv(folder, results, duelResults, tierValue, summary);
        return summary;
    }

    // 배치 모드용 (Unity를 화면 없이 실행):
    // Unity.exe -batchmode -projectPath <프로젝트> -executeMethod BalanceSimulatorWindow.RunBatch
    //   [-simGames 1000] [-simSeed 12345] [-simOut Balance] [-simBots Basic,Saver] [-simNoDuels] [-simLocked] [-simDifficulty Hard] [-simFusion]
    public static void RunBatch()
    {
        int games = 1000;
        int seed = 12345;
        string folder = OutputFolder;
        bool duels = true;
        bool locked = false;
        bool fusion = false;
        string difficulty = Difficulties[0];
        BalanceBotKind[] bots = ToBots(BotChoice.All);

        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            string next = i + 1 < args.Length ? args[i + 1] : "";
            switch (args[i])
            {
                case "-simGames": games = int.Parse(next); break;
                case "-simSeed": seed = int.Parse(next); break;
                case "-simOut": folder = next; break;
                case "-simNoDuels": duels = false; break;
                case "-simLocked": locked = true; break;
                case "-simFusion": fusion = true; break;
                case "-simDifficulty": difficulty = next; break;
                case "-simBots":
                    bots = System.Array.ConvertAll(next.Split(','),
                        name => (BalanceBotKind)System.Enum.Parse(typeof(BalanceBotKind), name.Trim()));
                    break;
            }
        }

        int exitCode = 0;
        try
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            string summary = Simulate(games, seed, bots, duels, locked, fusion, difficulty, folder, (label, rate) => false);
            Debug.Log($"[밸런스] 완료 ({watch.Elapsed.TotalSeconds:0}초) → {Path.GetFullPath(folder)}\n{summary}");
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            exitCode = 1;
        }
        EditorApplication.Exit(exitCode);
    }
}
