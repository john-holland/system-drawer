using System;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

/// <summary>Live engine memory buckets via Profiler counters (no snapshot required).</summary>
public sealed class UnitySystemsBuilder : IMemorySwizzleTreeBuilder
{
    public MemorySwizzleViewMode Mode => MemorySwizzleViewMode.UnitySystems;

    public MemorySwizzleNode Build(MemorySwizzleBuildContext ctx)
    {
        var root = MemorySwizzleNode.Create("engine", "Engine Memory", 0, MemorySwizzleKind.Root, Mode);
        long attributed = 0;

        AddCounter(root, "Mono Used", ReadMonoUsed(), ref attributed);
        AddCounter(root, "Mono Heap", ReadMonoHeap(), ref attributed);
        AddRecorder(root, "Gfx Driver", "Gfx Used Memory", ref attributed);
        AddRecorder(root, "Textures", "Texture Memory", ref attributed);
        AddRecorder(root, "Meshes", "Mesh Memory", ref attributed);
        AddRecorder(root, "Audio", "Audio Reserved Memory", ref attributed);
        AddRecorder(root, "Audio Used", "Audio Used Memory", ref attributed);
        AddRecorder(root, "Animation", "AnimationClip Memory", ref attributed);
        AddRecorder(root, "Physics", "Physics Used Memory", ref attributed);
        AddRecorder(root, "Physics 2D", "Physics2D Used Memory", ref attributed);
        AddRecorder(root, "GC Reserved", "GC Reserved Memory", ref attributed);
        AddRecorder(root, "GC Used", "GC Used Memory", ref attributed);
        AddRecorder(root, "Profiler", "Profiler Used Memory", ref attributed);
        AddRecorder(root, "System Used", "System Used Memory", ref attributed);

        long totalUsed = ReadTotalUsed();
        long untracked = Math.Max(0, totalUsed - attributed);
        if (untracked > 0)
            AddLeaf(root, "Untracked / Other", untracked, ref attributed);

        root.ComputeTotalBytes();
        root.ApplyPercentOfParent(root.SizeBytes);
        return root;
    }

    static long ReadMonoUsed() => Profiler.GetMonoUsedSizeLong();
    static long ReadMonoHeap() => Profiler.GetMonoHeapSizeLong();
    static long ReadTotalUsed() => Profiler.GetTotalAllocatedMemoryLong();

    static void AddRecorder(MemorySwizzleNode parent, string label, string statName, ref long attributed)
    {
        long v = TryReadRecorder(statName);
        if (v > 0)
            AddLeaf(parent, label, v, ref attributed);
    }

    static long TryReadRecorder(string statName)
    {
        try
        {
            var sampler = ProfilerRecorder.StartNew(ProfilerCategory.Memory, statName);
            if (!sampler.Valid)
            {
                sampler.Dispose();
                return 0;
            }
            long v = sampler.LastValue;
            sampler.Dispose();
            return Math.Max(0, v);
        }
        catch
        {
            return 0;
        }
    }

    static void AddCounter(MemorySwizzleNode parent, string label, long bytes, ref long attributed)
    {
        if (bytes > 0)
            AddLeaf(parent, label, bytes, ref attributed);
    }

    static void AddLeaf(MemorySwizzleNode parent, string label, long bytes, ref long attributed)
    {
        var n = MemorySwizzleNode.Create(label.ToLowerInvariant(), label, bytes, MemorySwizzleKind.System, MemorySwizzleViewMode.UnitySystems);
        parent.Children.Add(n);
        attributed += bytes;
    }
}
