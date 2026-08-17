#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.EditorTools;
using Locomotion.Rig;
using NUnit.Framework;
using UnityEngine;

public sealed class ArbitrarySkeletonFitterTests
{
    [Test]
    public void SynonymAndLaterality_MapsThighToHumanLeg()
    {
        var sources = new List<string> { "Hips", "LeftThigh", "RightThigh", "Head" };
        var parents = new List<int> { -1, 0, 0, 0 };
        var targets = new List<string> { "Human:Hips", "Human:LeftUpperLeg", "Human:RightUpperLeg", "Human:Head" };
        var fit = ArbitrarySkeletonFitter.Fit(sources, parents, targets, "Animal");
        var remap = fit.ToRemap();
        Assert.AreEqual("Human:Hips", remap["Hips"]);
        Assert.AreEqual("Human:LeftUpperLeg", remap["LeftThigh"]);
        Assert.AreEqual("Human:RightUpperLeg", remap["RightThigh"]);
        Assert.AreEqual("Human:Head", remap["Head"]);
        Assert.AreEqual("Left", ArbitrarySkeletonFitter.InferLaterality("LeftThigh"));
        Assert.AreEqual("Right", ArbitrarySkeletonFitter.InferLaterality("RightThigh"));
    }

    [Test]
    public void Unmatched_OfferedAsAnimalRows()
    {
        var sources = new List<string> { "Hips", "Tail1" };
        var parents = new List<int> { -1, 0 };
        var targets = new List<string> { "Human:Hips" };
        var fit = ArbitrarySkeletonFitter.Fit(sources, parents, targets, "Animal");
        Assert.IsTrue(fit.offeredAnimalRows.Contains("Animal:Tail1"));
        Assert.AreEqual("Animal:Tail1", fit.ToRemap()["Tail1"]);
        Assert.IsTrue(fit.unmatchedSource.Contains("Tail1"));
    }

    [Test]
    public void ApplyOfferedRows_AddsBoneMapEntries()
    {
        var go = new GameObject("FitMap");
        try
        {
            var map = go.AddComponent<BoneMap>();
            map.Set("Human:Hips", go.transform);
            var fit = ArbitrarySkeletonFitter.FitToBoneMap(
                new List<string> { "Hips", "WingL" },
                new List<int> { -1, 0 },
                map,
                "Animal");
            ArbitrarySkeletonFitter.ApplyOfferedRows(map, fit);
            Assert.IsTrue(map.TryGet("Animal:WingL", out _));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
