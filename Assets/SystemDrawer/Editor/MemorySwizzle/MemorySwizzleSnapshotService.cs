using System;
using System.Collections;
using System.IO;
using Unity.EditorCoroutines.Editor;
using Unity.Profiling.Memory;
using UnityEditor;
using UnityEngine;

/// <summary>Captures Memory Profiler snapshots into Library/MemorySwizzle.</summary>
public static class MemorySwizzleSnapshotService
{
    public const string CacheDir = "Library/MemorySwizzle";
    public const string LastSnapshotFile = "last.snap";

    public static string LastSnapshotPath => Path.Combine(CacheDir, LastSnapshotFile);
    public static bool IsCapturing { get; private set; }
    public static event Action<bool, string> CaptureFinished;

    public static void CaptureAsync()
    {
        if (IsCapturing)
            return;

        Directory.CreateDirectory(CacheDir);
        string path = Path.GetFullPath(LastSnapshotPath);
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch { /* ignore */ }
        }

        IsCapturing = true;
        EditorCoroutineUtility.StartCoroutineOwnerless(CaptureCoroutine(path));
    }

    static IEnumerator CaptureCoroutine(string path)
    {
        bool done = false;
        bool success = false;
        string resultPath = path;

        MemoryProfiler.TakeSnapshot(
            path,
            (p, ok) =>
            {
                success = ok;
                resultPath = p;
                done = true;
            },
            CaptureFlags.ManagedObjects | CaptureFlags.NativeObjects | CaptureFlags.NativeAllocations);

        while (!done)
            yield return null;

        IsCapturing = false;
        CaptureFinished?.Invoke(success, success ? resultPath : null);
    }
}
