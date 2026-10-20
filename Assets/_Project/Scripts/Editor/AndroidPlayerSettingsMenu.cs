using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 출시용 Player Settings와 빌드 메뉴. 여러 번 실행해도 같은 결과
// - MonsterCamp > 출시용 Player Settings 적용: 이름·패키지·버전·세로 고정·IL2CPP·ARM64·Target API·아이콘·스플래시 배경·로그
// - Build > Release AAB: 업로드 키(키스토어)로 서명한 AAB (키스토어·비밀번호가 없으면 멈추고 안내)
// - Build > Test APK: 내 폰 설치 확인용 APK (디버그 키로 서명, 출시와 같은 설정)
public static class AndroidPlayerSettingsMenu
{
    private const string CompanyName = "youstar";
    // 폰 홈 화면 이름 (스토어 이름은 "몬스터캠프: 크리처 오토배틀" / "Monster Camp: Auto Battler", Play Console에서 입력)
    private const string ProductName = "몬스터캠프";
    private const string PackageName = "com.youstar.monstercamp";
    private const string Version = "1.0.0";
    private const int VersionCode = 1;
    // Google Play: 2026-08-31부터 새 앱·업데이트는 Android 16 (API 36) 이상
    private const int TargetApi = 36;
    private const int MinApi = 25;
    // 아이콘 밤하늘 색 (#1E285A)
    private static readonly Color SplashBackground = new Color32(30, 40, 90, 255);

    private const string IconFolder = "Assets/_Project/Art/AppIcon/";
    private const string IconPath = IconFolder + "icon_A_pixel_camp_1024.png";
    private const string LegacyIconPath = IconFolder + "icon_A_pixel_camp_512.png";
    private const string RoundIconPath = IconFolder + "icon_round_1024.png";
    private const string ForegroundPath = IconFolder + "adaptive_foreground_432.png";
    private const string BackgroundPath = IconFolder + "adaptive_background_432.png";

    public const string KeystoreSuggestedPath = @"C:\Projects\keys\monstercamp-upload.keystore";
    private const string BuildFolder = "Builds";
    private static string AabPath => $"{BuildFolder}/MonsterCamp_{Version}_{VersionCode}.aab";
    private static string ApkPath => $"{BuildFolder}/MonsterCamp_{Version}_{VersionCode}_test.apk";

    [MenuItem("MonsterCamp/출시용 Player Settings 적용")]
    public static void Apply()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.Android.bundleVersionCode = VersionCode;

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)TargetApi;
        if ((int)PlayerSettings.Android.minSdkVersion != MinApi)
        {
            Debug.LogWarning($"[출시 설정] 최소 API가 {(int)PlayerSettings.Android.minSdkVersion}입니다 (기존 {MinApi}, 바꾸지 않음)");
        }

        ApplyIcons();

        // 스플래시: Unity 로고는 그대로, 배경색만
        PlayerSettings.SplashScreen.backgroundColor = SplashBackground;

        // 로그 스택 트레이스: 일반·경고·Assert는 없음, Error·Exception만 스크립트 스택 (출력 걸러내기는 ReleaseLogFilter)
        PlayerSettings.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Assert, StackTraceLogType.None);
        PlayerSettings.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        PlayerSettings.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.allowDebugging = false;

        AssetDatabase.SaveAssets();
        Debug.Log($"[출시 설정] {ProductName} ({PackageName}) {Version}({VersionCode}), 세로 고정, IL2CPP·ARM64, " +
                  $"Target API {TargetApi}·최소 API {(int)PlayerSettings.Android.minSdkVersion}, 아이콘, 스플래시 배경 #1E285A, " +
                  "Development Build 끔, 스택 트레이스 Error·Exception만");
    }

    // 기본 아이콘 1024 + Android Adaptive(전경·배경)·Round·Legacy. 도트라 Point 필터, 압축·밉맵 없음
    private static void ApplyIcons()
    {
        foreach (string path in new[] { IconPath, LegacyIconPath, RoundIconPath, ForegroundPath, BackgroundPath })
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }
            else
            {
                Debug.LogWarning($"[출시 설정] 아이콘 없음: {path}");
            }
        }

        var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
        var legacy = AssetDatabase.LoadAssetAtPath<Texture2D>(LegacyIconPath) ?? icon;
        var round = AssetDatabase.LoadAssetAtPath<Texture2D>(RoundIconPath) ?? icon;
        var foreground = AssetDatabase.LoadAssetAtPath<Texture2D>(ForegroundPath);
        var background = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);

        SetAndroidIcons(AndroidPlatformIconKind.Legacy, legacy);
        SetAndroidIcons(AndroidPlatformIconKind.Round, round);
        // Adaptive: 0번 칸 배경, 1번 칸 전경
        SetAndroidIcons(AndroidPlatformIconKind.Adaptive, background, foreground);
    }

    private static void SetAndroidIcons(PlatformIconKind kind, params Texture2D[] layers)
    {
        PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
        foreach (PlatformIcon icon in icons)
        {
            for (int layer = 0; layer < layers.Length && layer < icon.maxLayerCount; layer++)
            {
                icon.SetTexture(layers[layer], layer);
            }
        }
        PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
    }

    [MenuItem("Build/Release AAB")]
    public static void BuildReleaseAab()
    {
        string problem = KeystoreProblem();
        if (problem != null)
        {
            string message = problem + "\n\n1. Edit > Project Settings > Player > Android > Publishing Settings\n" +
                             "2. Keystore Manager로 업로드 키를 만들거나 기존 키를 고르기\n" +
                             $"   (추천 위치: {KeystoreSuggestedPath})\n" +
                             "3. Project Keystore·Project Key 비밀번호 입력 (Unity를 켤 때마다 다시 입력)\n" +
                             "4. 다시 Build > Release AAB";
            Debug.LogWarning("[빌드] AAB를 만들지 않았습니다: " + problem);
            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("업로드 키가 필요해요", message, "확인");
            }
            return;
        }
        Build(AabPath, appBundle: true, debugKey: false);
    }

    [MenuItem("Build/Test APK")]
    public static void BuildTestApk()
    {
        Build(ApkPath, appBundle: false, debugKey: true);
    }

    // 키스토어·비밀번호가 준비 안 됐으면 이유, 됐으면 null
    private static string KeystoreProblem()
    {
        if (!PlayerSettings.Android.useCustomKeystore)
        {
            return "Custom Keystore가 꺼져 있어요 (지금은 디버그 키라 Google Play에 올릴 수 없음)";
        }
        if (string.IsNullOrEmpty(PlayerSettings.Android.keystoreName) || !File.Exists(PlayerSettings.Android.keystoreName))
        {
            return $"키스토어 파일이 없어요: {PlayerSettings.Android.keystoreName}";
        }
        if (string.IsNullOrEmpty(PlayerSettings.Android.keyaliasName))
        {
            return "키 별칭(Alias)을 고르지 않았어요";
        }
        if (string.IsNullOrEmpty(PlayerSettings.Android.keystorePass) || string.IsNullOrEmpty(PlayerSettings.Android.keyaliasPass))
        {
            return "키스토어·키 비밀번호가 비어 있어요 (Unity를 다시 켜면 다시 입력해야 해요)";
        }
        return null;
    }

    // 출시 설정을 다시 적용하고 빌드. debugKey면 잠깐 디버그 키로 바꿨다가 되돌린다
    private static void Build(string path, bool appBundle, bool debugKey)
    {
        Apply();
        Directory.CreateDirectory(BuildFolder);
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0)
        {
            Debug.LogError("[빌드] Build Settings에 켜진 씬이 없습니다. MonsterCamp > Build Main Scene을 먼저 실행하세요");
            return;
        }

        bool keystoreBefore = PlayerSettings.Android.useCustomKeystore;
        bool bundleBefore = EditorUserBuildSettings.buildAppBundle;
        try
        {
            if (debugKey)
            {
                PlayerSettings.Android.useCustomKeystore = false;
            }
            EditorUserBuildSettings.buildAppBundle = appBundle;
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = path,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None,
            });
            BuildSummary summary = report.summary;
            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[빌드] 완료: {path} ({summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalMinutes:0.0}분)");
                if (!Application.isBatchMode)
                {
                    EditorUtility.RevealInFinder(path);
                }
            }
            else
            {
                Debug.LogError($"[빌드] 실패 ({summary.result}), 오류 {summary.totalErrors}개: {path}");
            }
        }
        finally
        {
            PlayerSettings.Android.useCustomKeystore = keystoreBefore;
            EditorUserBuildSettings.buildAppBundle = bundleBefore;
        }
    }
}
