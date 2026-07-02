#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class SeedDependencyTreeTests
{
    [Test]
    public void DeriveSeed_MasterNode_ReturnsMaster()
    {
        var tree = ScriptableObject.CreateInstance<SeedDependencyTreeAsset>();
        tree.masterSeed = 99;
        tree.nodes = new[]
        {
            new SeedDependencyNode { id = "root", parentId = null, deriveFn = SeedDeriveFn.Master }
        };
        Assert.AreEqual(99, tree.DeriveSeed("root"));
        Object.DestroyImmediate(tree);
    }

    [Test]
    public void DeriveSeed_DayCollapse_CombinesParent()
    {
        var tree = ScriptableObject.CreateInstance<SeedDependencyTreeAsset>();
        tree.masterSeed = 10;
        tree.nodes = new[]
        {
            new SeedDependencyNode { id = "day", parentId = null, deriveFn = SeedDeriveFn.DayCollapse }
        };
        int a = tree.DeriveSeed("day", dayCollapseSeed: 5);
        int b = tree.DeriveSeed("day", dayCollapseSeed: 5);
        int c = tree.DeriveSeed("day", dayCollapseSeed: 6);
        Assert.AreEqual(a, b);
        Assert.AreNotEqual(a, c);
        Object.DestroyImmediate(tree);
    }

    [Test]
    public void HashCombine_IsDeterministic()
    {
        int h = SeedDependencyTreeAsset.HashCombine(42, 7);
        Assert.AreEqual(h, SeedDependencyTreeAsset.HashCombine(42, 7));
    }
}
#endif
