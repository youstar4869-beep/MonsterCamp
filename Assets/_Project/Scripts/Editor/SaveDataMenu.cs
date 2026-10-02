using System.IO;
using UnityEditor;
using UnityEngine;

// 테스트용 메뉴: 저장 데이터 삭제 / 저장 폴더 열기
public static class SaveDataMenu
{
    [MenuItem("MonsterCamp/저장 데이터 삭제")]
    public static void DeleteSaveData()
    {
        string folder = SaveSystem.Folder;
        if (!EditorUtility.DisplayDialog("저장 데이터 삭제",
                $"meta.json, run.json과 백업·임시 파일을 지웁니다.\n\n{folder}", "삭제", "취소"))
        {
            return;
        }

        int deleted = 0;
        if (Directory.Exists(folder))
        {
            foreach (string pattern in new[] { "meta.json*", "run.json*" })
            {
                foreach (string file in Directory.GetFiles(folder, pattern))
                {
                    File.Delete(file);
                    deleted++;
                }
            }
        }
        Debug.Log($"[Save] 저장 데이터 {deleted}개 파일 삭제: {folder}");
    }

    [MenuItem("MonsterCamp/별 100개 추가")]
    public static void AddStars()
    {
        const int amount = 100;

        // Play 중이면 게임이 들고 있는 값에 더한다 (파일만 고치면 게임이 나중에 덮어쓰므로)
        if (Application.isPlaying)
        {
            var meta = Object.FindFirstObjectByType<MetaProgress>();
            if (meta != null)
            {
                meta.AddStars(amount);
                Debug.Log($"[Save] 별 {amount}개 추가 → 전체 {meta.Stars}");
                return;
            }
        }

        MetaSaveData data = SaveSystem.LoadMeta();
        data.stars += amount;
        SaveSystem.SaveMeta(data);
        Debug.Log($"[Save] 별 {amount}개 추가 → 전체 {data.stars} (meta.json)");
    }

    [MenuItem("MonsterCamp/저장 폴더 열기")]
    public static void OpenSaveFolder()
    {
        string folder = SaveSystem.Folder;
        Directory.CreateDirectory(folder);
        EditorUtility.RevealInFinder(folder + "/");
    }
}
