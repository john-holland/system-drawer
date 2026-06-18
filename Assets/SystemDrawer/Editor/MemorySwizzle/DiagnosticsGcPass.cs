#if UNITY_EDITOR
using System;
using Locomotion.Audio;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using Weather.Executor;

/// <summary>Manual GC pass plus sticky cache clears for diagnostics sessions.</summary>
public static class DiagnosticsGcPass
{
    const string MenuPath = "Window/System Drawer/Diagnostics/Perform GC Pass";

    [MenuItem(MenuPath, false, 52)]
    public static void PerformGcPassMenu()
    {
        PerformGcPass(logResult: true);
    }

    [MenuItem(MenuPath, true)]
    public static bool PerformGcPassMenuValidate() => !EditorApplication.isCompiling;

    public static void PerformGcPass(bool logResult = true)
    {
        long beforeManaged = Profiler.GetTotalAllocatedMemoryLong();
        long beforeMono = Profiler.GetMonoUsedSizeLong();

        ClearStickyCaches();

        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long afterManaged = Profiler.GetTotalAllocatedMemoryLong();
        long afterMono = Profiler.GetMonoUsedSizeLong();

        if (logResult)
        {
            Debug.Log(
                "[Diagnostics] GC pass complete.\n" +
                $"  Managed allocated: {FormatBytes(beforeManaged)} -> {FormatBytes(afterManaged)} " +
                $"({FormatDelta(beforeManaged - afterManaged)})\n" +
                $"  Mono used:         {FormatBytes(beforeMono)} -> {FormatBytes(afterMono)} " +
                $"({FormatDelta(beforeMono - afterMono)})");
        }
    }

    static void ClearStickyCaches()
    {
        WeatherDiagnosticCaches.ClearStickyCaches();

        AudioPathingSolver[] solvers = UnityEngine.Object.FindObjectsByType<AudioPathingSolver>(FindObjectsSortMode.None);
        for (int i = 0; i < solvers.Length; i++)
        {
            if (solvers[i] != null)
                solvers[i].ClearTransmissionCache();
        }

        PerfTrace.Flush();
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            bytes = 0;
        const double kb = 1024d;
        const double mb = kb * 1024d;
        const double gb = mb * 1024d;
        if (bytes >= gb)
            return (bytes / gb).ToString("0.##") + " GB";
        if (bytes >= mb)
            return (bytes / mb).ToString("0.##") + " MB";
        if (bytes >= kb)
            return (bytes / kb).ToString("0.##") + " KB";
        return bytes + " B";
    }

    static string FormatDelta(long deltaBytes) =>
        (deltaBytes >= 0 ? "freed " : "grew ") + FormatBytes(Math.Abs(deltaBytes));
}
#endif
