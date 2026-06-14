#if UNITY_EDITOR
using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;

/// <summary>Links PerfTrace sessions with Memory Swizzle snapshot captures.</summary>
public static class PerfTraceMemoryCorrelator
{
    const string LastCorrelationUtcPref = "PerfTrace.LastCorrelationUtc";
    const string LastCorrelationPathPref = "PerfTrace.LastCorrelationPath";

    public static string LastCorrelationUtc
    {
        get => EditorPrefs.GetString(LastCorrelationUtcPref, "");
        private set => EditorPrefs.SetString(LastCorrelationUtcPref, value ?? "");
    }

    public static string LastSnapshotPath
    {
        get => EditorPrefs.GetString(LastCorrelationPathPref, MemorySwizzleSnapshotService.LastSnapshotPath);
        private set => EditorPrefs.SetString(LastCorrelationPathPref, value ?? "");
    }

    public static event Action CorrelationUpdated;

    public static void CaptureCorrelatedMemorySnapshot(PerfTraceSession session = null)
    {
        string utc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
        LastCorrelationUtc = utc;
        if (session != null)
            session.CorrelationUtc = utc;

        MemorySwizzleSnapshotService.CaptureFinished -= OnCaptureFinished;
        MemorySwizzleSnapshotService.CaptureFinished += OnCaptureFinished;
        MemorySwizzleSnapshotService.CaptureAsync();
    }

    static void OnCaptureFinished(bool success, string path)
    {
        MemorySwizzleSnapshotService.CaptureFinished -= OnCaptureFinished;
        if (success && !string.IsNullOrEmpty(path))
            LastSnapshotPath = path;
        CorrelationUpdated?.Invoke();
    }

    public static void OpenMemorySwizzle()
    {
        EditorApplication.ExecuteMenuItem("Window/System Drawer/Diagnostics/Memory Swizzle View");
    }

    public static void OpenPerfTrace() => DiagnosticsWindowLauncher.TryOpenPerfTrace();

    public static string FormatLastCorrelationLabel()
    {
        if (string.IsNullOrEmpty(LastCorrelationUtc))
            return "";
        if (!DateTime.TryParse(LastCorrelationUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var utc))
            return "";
        return "Memory @ " + utc.ToLocalTime().ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture);
    }
}
#endif
