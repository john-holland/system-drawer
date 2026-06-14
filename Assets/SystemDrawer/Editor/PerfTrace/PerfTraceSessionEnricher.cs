#if UNITY_EDITOR
using System.Collections.Generic;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>Editor-only diagnostic enrichment for captured sessions.</summary>
public static class PerfTraceSessionEnricher
{
    public static void Enrich(PerfTraceSession session)
    {
        if (session == null)
            return;

        var namedTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
        session.ScriptingBackend = PlayerSettings.GetScriptingBackend(namedTarget).ToString();
        session.Platform = Application.platform.ToString();
        session.FrameIndex = Time.frameCount;

#if UNITY_2020_2_OR_NEWER
        if (FrameTimingManager.IsFeatureEnabled())
        {
            FrameTimingManager.CaptureFrameTimings();
            var timings = new FrameTiming[1];
            uint count = FrameTimingManager.GetLatestTimings(1, timings);
            if (count > 0)
            {
                session.CpuFrameMs = timings[0].cpuFrameTime;
                session.GpuFrameMs = timings[0].gpuFrameTime;
            }
        }
#endif

        session.MemoryCounters = BuildMemoryCounterSummary();
    }

    static string BuildMemoryCounterSummary()
    {
        var parts = new List<string>
        {
            FormatMemory("GC Used Memory"),
            FormatMemory("System Used Memory"),
            FormatMemory("Texture Memory")
        };
        parts.RemoveAll(string.IsNullOrEmpty);
        return string.Join("; ", parts);
    }

    static string FormatMemory(string statName)
    {
        long v = ReadMemory(statName).value;
        return v > 0 ? statName + ": " + PerfTraceFormat.Bytes(v) : "";
    }

    static (string label, long value) ReadMemory(string statName)
    {
        try
        {
            using var recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, statName);
            if (!recorder.Valid)
                return (statName, 0);
            return (statName, recorder.LastValue);
        }
        catch
        {
            return (statName, 0);
        }
    }
}
#endif
