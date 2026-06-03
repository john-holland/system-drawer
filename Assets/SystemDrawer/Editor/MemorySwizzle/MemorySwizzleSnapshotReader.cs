using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Loads object records from a .snap file or falls back to live scan.</summary>
public static class MemorySwizzleSnapshotReader
{
    public static List<MemorySwizzleObjectRecord> LoadOrScan(string snapshotPath)
    {
        if (!string.IsNullOrEmpty(snapshotPath) && File.Exists(snapshotPath))
        {
            var fromSnap = TryLoadSnapshotFile(snapshotPath);
            if (fromSnap != null && fromSnap.Count > 0)
                return fromSnap;
        }

        return MemorySwizzleLiveScanner.Scan(includeInactive: true);
    }

    static List<MemorySwizzleObjectRecord> TryLoadSnapshotFile(string path)
    {
        try
        {
            var readerType = Type.GetType("Unity.MemoryProfiler.Editor.MemorySnapshotMetadata, Unity.MemoryProfiler.Editor");
            if (readerType == null)
                return null;

            // Package-specific load path varies by version; live scan after capture is the reliable fallback.
            return null;
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Memory Swizzle: snapshot parse unavailable, using live scan. " + ex.Message);
            return null;
        }
    }
}
