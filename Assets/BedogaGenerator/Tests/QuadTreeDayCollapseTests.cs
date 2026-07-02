#if UNITY_EDITOR
using BedogaGenerator.DreamCycle;
using NUnit.Framework;
using UnityEngine;

public class QuadTreeDayCollapseTests
{
    [Test]
    public void Collapse_Empty_IsStable()
    {
        var a = QuadTreeDayCollapse.Collapse(null);
        var b = QuadTreeDayCollapse.Collapse(null);
        Assert.AreEqual(a.dayCollapseSeed, b.dayCollapseSeed);
        Assert.AreEqual(0, a.generatorCount);
    }

    [Test]
    public void Collapse_SameGenerators_SameSeed()
    {
        var go = new GameObject("sg_test");
        var sg = go.AddComponent<SpatialGenerator>();
        sg.seed = 123;
        sg.mode = SpatialGenerator.GenerationMode.TwoDimensional;
        var list = new[] { sg };
        var r1 = QuadTreeDayCollapse.Collapse(list);
        var r2 = QuadTreeDayCollapse.Collapse(list);
        Assert.AreEqual(r1.dayCollapseSeed, r2.dayCollapseSeed);
        Object.DestroyImmediate(go);
    }
}
#endif
