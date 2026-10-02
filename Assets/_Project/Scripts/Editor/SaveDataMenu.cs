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

    [MenuItem("MonsterCamp/저장 폴더 열기")]
    public static void OpenSaveFolder()
    {
        string folder = SaveSystem.Folder;
        Directory.CreateDirectory(folder);
        EditorUtility.RevealInFinder(folder + "/");
    }
}
