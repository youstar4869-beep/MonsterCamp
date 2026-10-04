using UnityEditor;
using UnityEngine;

// 테스트용 메뉴: 조합 해금 ↔ 잠금 (meta.json). 발견·만듦 기록은 그대로 둔다
public static class FusionMenu
{
    [MenuItem("MonsterCamp/조합 해금·잠금 전환")]
    public static void Toggle()
    {
        // Play 중이면 게임이 들고 있는 값을 바꾼다 (파일만 고치면 게임이 나중에 덮어쓰므로)
        if (Application.isPlaying)
        {
            var meta = Object.FindFirstObjectByType<MetaProgress>();
            if (meta != null)
            {
                meta.SetFusionUnlocked(!meta.FusionUnlocked);
                Debug.Log($"[Save] 조합 {(meta.FusionUnlocked ? "해금" : "잠금")} (상점 화면은 다음 동작 때 반영)");
                return;
            }
        }

        MetaSaveData data = SaveSystem.LoadMeta();
        data.fusionUnlocked = !data.fusionUnlocked;
        SaveSystem.SaveMeta(data);
        Debug.Log($"[Save] 조합 {(data.fusionUnlocked ? "해금" : "잠금")} (meta.json)");
    }
}
