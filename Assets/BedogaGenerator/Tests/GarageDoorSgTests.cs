using NUnit.Framework;
using UnityEngine;

public sealed class GarageDoorSgTests
{
    [Test]
    public void ConfigureRepeat_SetsMullionAndMouldingLimits()
    {
        var go = new GameObject("garage");
        var door = go.AddComponent<GarageDoorNode>();
        var spec = ScriptableObject.CreateInstance<DoorAssemblySpec>();
        spec.sectionCount = 4;
        spec.mouldingSides = 4;
        door.assembly = spec;
        var stile = new GameObject("stile").AddComponent<DoorLockStileNode>();
        stile.transform.SetParent(go.transform, false);
        var mull = new GameObject("mull").AddComponent<DoorMullionNode>();
        mull.transform.SetParent(go.transform, false);
        var mould = new GameObject("mould").AddComponent<DoorMouldingNode>();
        mould.transform.SetParent(go.transform, false);
        door.ConfigureRepeat(4);
        Assert.AreEqual(2, stile.placementLimit);
        Assert.AreEqual(3, mull.placementLimit);
        Assert.AreEqual(4, mould.sides);
        Assert.AreEqual(4, mould.placementLimit);
        Assert.AreEqual(SGBehaviorTreeNode.PlaceSearchMode.Radial, mould.placeSearchMode);
        Object.DestroyImmediate(spec);
        Object.DestroyImmediate(go);
    }
}
