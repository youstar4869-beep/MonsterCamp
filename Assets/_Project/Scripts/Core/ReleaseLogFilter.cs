using UnityEngine;

// 출시 빌드(에디터·Development Build가 아닐 때)에서는 Error·Exception 로그만 남긴다 (일반·경고 로그는 출력 안 함)
public static class ReleaseLogFilter
{
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Apply()
    {
        Debug.unityLogger.filterLogType = LogType.Error;
    }
#endif
}
