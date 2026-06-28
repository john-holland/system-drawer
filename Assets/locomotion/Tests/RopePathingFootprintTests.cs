using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class RopePathingFootprintTests
{
    [Test]
    public void CollectPathSamples_CapsAtMax()
    {
        var go = new GameObject("rope");
        var sys = go.AddComponent<RopeSystem>();
        sys.Initialize();

        var list = new List<Vector3>();
        sys.CollectPathSamples(list, 4);
        Assert.LessOrEqual(list.Count, 4);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void FootprintRegistry_RegisterUnregister()
    {
        var go = new GameObject("fp");
        var fp = go.AddComponent<RopePathingFootprint>();
        RopePathingFootprintRegistry.Register(fp);
        bool found = false;
        foreach (var x in RopePathingFootprintRegistry.All)
        {
            if (x == fp) { found = true; break; }
        }
        Assert.IsTrue(found);
        RopePathingFootprintRegistry.Unregister(fp);
        Object.DestroyImmediate(go);
    }
}
