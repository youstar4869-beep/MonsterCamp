using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

// Android 실기기 테스트용 Player Settings: 이름·패키지·세로 고정·버전을 맞추고,
// IL2CPP·ARM64·최소 API 25는 바꾸지 않고 확인만 한다. 여러 번 실행해도 같은 결과
public static class AndroidPlayerSettingsMenu
{
    private const string CompanyName = "youstar";
    private const string ProductName = "몬스터 캠프";
    private const string PackageName = "com.youstar.monstercamp";
    private const string Version = "0.1.0";
    private const int VersionCode = 1;

    [MenuItem("MonsterCamp/Android 플레이어 설정 적용")]
    public static void Apply()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;

        PlayerSettings.bundleVersion = Version;
        PlayerSettings.Android.bundleVersionCode = VersionCode;
        AssetDatabase.SaveAssets();

        bool il2cpp = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP;
        bool arm64Only = PlayerSettings.Android.targetArchitectures == AndroidArchitecture.ARM64;
        bool minApi25 = PlayerSettings.Android.minSdkVersion == AndroidSdkVersions.AndroidApiLevel25;
        string check = $"IL2CPP {(il2cpp ? "OK" : "아님")}, ARM64만 {(arm64Only ? "OK" : "아님")}, 최소 API 25 {(minApi25 ? "OK" : "아님")}";
        if (il2cpp && arm64Only && minApi25)
        {
            Debug.Log($"[Android 설정] {ProductName} ({PackageName}) {Version}({VersionCode}), 세로 고정. {check}");
        }
        else
        {
            Debug.LogWarning($"[Android 설정] 적용 완료, 확인 필요: {check}");
        }
    }
}
