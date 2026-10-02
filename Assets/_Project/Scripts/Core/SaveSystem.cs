using System;
using System.IO;
using UnityEngine;

// Application.persistentDataPath 에 meta.json / run.json 저장.
// 어떤 오류가 나도 예외를 밖으로 던지지 않는다 (게임이 멈추면 안 됨).
// 깨진 파일은 "이름.broken-날짜시간" 으로 옮겨 두고 없는 것으로 처리한다.
public static class SaveSystem
{
    public const string MetaFileName = "meta.json";
    public const string RunFileName = "run.json";

    public static string Folder => Application.persistentDataPath;
    private static string MetaPath => Path.Combine(Folder, MetaFileName);
    private static string RunPath => Path.Combine(Folder, RunFileName);

    public static bool HasRun => File.Exists(RunPath);

    // ---------- meta.json ----------

    // 없거나 깨졌으면 기본값을 돌려준다 (깨진 파일은 백업)
    public static MetaSaveData LoadMeta()
    {
        if (TryRead(MetaPath, out MetaSaveData data) && data.version == MetaSaveData.CurrentVersion)
        {
            return data;
        }
        if (File.Exists(MetaPath))
        {
            BackupBroken(MetaPath);
        }
        return new MetaSaveData();
    }

    public static void SaveMeta(MetaSaveData data)
    {
        data.version = MetaSaveData.CurrentVersion;
        Write(MetaPath, data);
    }

    // ---------- run.json ----------

    // 읽기에 실패하면 false. 파일이 있었는데 못 읽었으면 백업으로 옮기고 wasBroken = true
    public static bool TryLoadRun(out RunSaveData data, out bool wasBroken)
    {
        wasBroken = false;
        if (!File.Exists(RunPath))
        {
            data = null;
            return false;
        }

        if (TryRead(RunPath, out data) && data.version == RunSaveData.CurrentVersion)
        {
            return true;
        }

        BackupBrokenRun();
        wasBroken = true;
        data = null;
        return false;
    }

    public static void SaveRun(RunSaveData data)
    {
        data.version = RunSaveData.CurrentVersion;
        Write(RunPath, data);
    }

    public static void DeleteRun()
    {
        try
        {
            if (File.Exists(RunPath))
            {
                File.Delete(RunPath);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] run.json 삭제 실패: {e.Message}");
        }
    }

    // 내용은 읽혔지만 쓸 수 없는 데이터일 때 (예: 없는 트레이너)
    public static void BackupBrokenRun()
    {
        BackupBroken(RunPath);
    }

    // ---------- 내부 ----------

    private static bool TryRead<T>(string path, out T data) where T : class
    {
        data = null;
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }
            data = JsonUtility.FromJson<T>(File.ReadAllText(path));
            return data != null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] {Path.GetFileName(path)} 읽기 실패: {e.Message}");
            data = null;
            return false;
        }
    }

    // 임시 파일에 다 쓴 뒤 덮어쓴다 (쓰는 도중 꺼져도 원래 파일이 깨지지 않게)
    private static void Write<T>(string path, T data)
    {
        string tempPath = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllText(tempPath, JsonUtility.ToJson(data, true));
            File.Copy(tempPath, path, true);
            File.Delete(tempPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] {Path.GetFileName(path)} 저장 실패: {e.Message}");
        }
    }

    private static void BackupBroken(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return;
            }
            string backupPath = $"{path}.broken-{DateTime.Now:yyyyMMdd-HHmmss}";
            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
            File.Move(path, backupPath);
            Debug.LogWarning($"[Save] 깨진 저장 파일을 백업했습니다: {backupPath}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] 백업 실패, 파일을 지웁니다: {e.Message}");
            try
            {
                File.Delete(path);
            }
            catch (Exception)
            {
                // 지우지도 못하면 다음 저장 때 덮어쓴다
            }
        }
    }
}
