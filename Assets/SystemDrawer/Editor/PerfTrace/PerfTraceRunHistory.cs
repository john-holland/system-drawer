#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>Persists benchmark runs under Library/PerfTrace/runs/.</summary>
public static class PerfTraceRunHistory
{
    public const string CacheDir = "Library/PerfTrace/runs";
    public const string IndexFile = "index.json";

    [Serializable]
    sealed class IndexData
    {
        public List<string> runIds = new List<string>();
    }

    public static event Action HistoryChanged;

    public static IReadOnlyList<PerfTraceRunRecord> LoadIndex()
    {
        var list = new List<PerfTraceRunRecord>();
        string indexPath = Path.Combine(CacheDir, IndexFile);
        if (!File.Exists(indexPath))
            return list;

        try
        {
            var index = JsonUtility.FromJson<IndexData>(File.ReadAllText(indexPath));
            if (index?.runIds == null)
                return list;
            for (int i = index.runIds.Count - 1; i >= 0; i--)
            {
                var record = LoadRecord(index.runIds[i]);
                if (record != null)
                    list.Add(record);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PerfTrace: failed to load run index. " + ex.Message);
        }
        return list;
    }

    public static PerfTraceSession LoadSession(string runId)
    {
        if (string.IsNullOrEmpty(runId))
            return null;
        string path = Path.Combine(CacheDir, runId + ".session");
        if (!File.Exists(path))
            return null;
        try
        {
            return PerfTraceSessionSerialization.FromJson(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("PerfTrace: failed to load session " + runId + ". " + ex.Message);
            return null;
        }
    }

    public static void SaveRun(PerfTraceSession session, bool playModeSession)
    {
        if (session == null || session.Root == null)
            return;

        Directory.CreateDirectory(CacheDir);
        if (string.IsNullOrEmpty(session.RunId))
            session.RunId = PerfTraceSession.NewRunId();

        string now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(session.CapturedUtc))
            session.CapturedUtc = now;

        var record = new PerfTraceRunRecord
        {
            id = session.RunId,
            label = session.RunLabel ?? "Unnamed run",
            startedUtc = session.StartedUtc ?? now,
            endedUtc = now,
            sessionFile = session.RunId + ".session",
            playModeSession = playModeSession,
            totalRootMs = session.TotalRootMs,
            correlationUtc = session.CorrelationUtc ?? ""
        };

        string sessionPath = Path.Combine(CacheDir, record.sessionFile);
        string recordPath = Path.Combine(CacheDir, record.id + ".json");
        File.WriteAllText(sessionPath, PerfTraceSessionSerialization.ToJson(session, prettyPrint: true));
        File.WriteAllText(recordPath, JsonUtility.ToJson(record, prettyPrint: true));

        var index = LoadIndexData();
        index.runIds.Remove(record.id);
        index.runIds.Add(record.id);
        EnforceRetention(index);
        File.WriteAllText(Path.Combine(CacheDir, IndexFile), JsonUtility.ToJson(index, prettyPrint: true));
        HistoryChanged?.Invoke();
    }

    static IndexData LoadIndexData()
    {
        string indexPath = Path.Combine(CacheDir, IndexFile);
        if (!File.Exists(indexPath))
            return new IndexData();
        try
        {
            return JsonUtility.FromJson<IndexData>(File.ReadAllText(indexPath)) ?? new IndexData();
        }
        catch
        {
            return new IndexData();
        }
    }

    static PerfTraceRunRecord LoadRecord(string runId)
    {
        string path = Path.Combine(CacheDir, runId + ".json");
        if (!File.Exists(path))
            return null;
        try
        {
            return JsonUtility.FromJson<PerfTraceRunRecord>(File.ReadAllText(path));
        }
        catch
        {
            return null;
        }
    }

    static void EnforceRetention(IndexData index)
    {
        int max = PerfTrace.Settings.maxRetainedRuns;
        while (index.runIds.Count > max)
        {
            string oldest = index.runIds[0];
            index.runIds.RemoveAt(0);
            TryDeleteRunFiles(oldest);
        }
    }

    public static void DeleteRun(string runId)
    {
        if (string.IsNullOrEmpty(runId))
            return;
        var index = LoadIndexData();
        index.runIds.Remove(runId);
        File.WriteAllText(Path.Combine(CacheDir, IndexFile), JsonUtility.ToJson(index, prettyPrint: true));
        TryDeleteRunFiles(runId);
        HistoryChanged?.Invoke();
    }

    public static void ClearAll()
    {
        var index = LoadIndexData();
        for (int i = 0; i < index.runIds.Count; i++)
            TryDeleteRunFiles(index.runIds[i]);
        index.runIds.Clear();
        File.WriteAllText(Path.Combine(CacheDir, IndexFile), JsonUtility.ToJson(index, prettyPrint: true));
        HistoryChanged?.Invoke();
    }

    static void TryDeleteRunFiles(string runId)
    {
        TryDelete(Path.Combine(CacheDir, runId + ".json"));
        TryDelete(Path.Combine(CacheDir, runId + ".session"));
    }

    static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { /* ignore */ }
    }

    public static string FormatDropdownLabel(PerfTraceRunRecord record)
    {
        if (record == null)
            return "";
        DateTime utc;
        if (!DateTime.TryParse(record.endedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out utc))
            utc = DateTime.UtcNow;
        string local = utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        return local + " — " + (record.label ?? "Unnamed run");
    }

    public static string FormatLiveDropdownLabel(PerfTraceSession session)
    {
        if (session == null)
            return "Live — (empty)";
        string label = string.IsNullOrEmpty(session.RunLabel) ? "Unnamed run" : session.RunLabel;
        return "Live — " + label + " (" + PerfTraceFormat.Ms(session.TotalRootMs) + ")";
    }
}
#endif
