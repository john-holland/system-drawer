#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Locomotion.EditorTools;
using Locomotion.Musculature;

/// <summary>EditMode tests for hair + animation root ensure used by Repair Ragdoll.</summary>
public sealed class RagdollHairAndAnimationEnsureTests
{
    [Test]
    public void EnsureHairRuntime_CreatesDriverBinder_AndIsIdempotent()
    {
        var actor = new GameObject("HairActor");
        actor.SetActive(false);
        var head = new GameObject("Head");
        head.transform.SetParent(actor.transform, false);
        var system = actor.AddComponent<RagdollSystem>();
        system.headComponent = head.AddComponent<RagdollHead>();

        HairPlumePhysicsDriver driver = RagdollAutoWire.EnsureHairRuntime(actor, new RagdollAutoWire.Report());
        Assert.IsNotNull(driver);
        Assert.IsNotNull(driver.GetComponent<HairBodyCapsuleBinder>());
        Assert.AreSame(head.transform, driver.scalpRoot);
        Assert.IsNotNull(driver.bodyBinder);

        string configPath = driver.config != null ? AssetDatabase.GetAssetPath(driver.config) : null;

        HairPlumePhysicsDriver again = RagdollAutoWire.EnsureHairRuntime(actor, new RagdollAutoWire.Report());
        Assert.AreSame(driver, again);
        Assert.AreEqual(1, actor.GetComponentsInChildren<HairPlumePhysicsDriver>(true).Length);

        Object.DestroyImmediate(actor);
        if (!string.IsNullOrEmpty(configPath))
            AssetDatabase.DeleteAsset(configPath);
    }

    [Test]
    public void EnsureAnimationRoots_CreatesManagers_AndAnimationRoot()
    {
        var actor = new GameObject("AnimActor");
        actor.SetActive(false);
        actor.AddComponent<RagdollSystem>();

        RagdollAutoWire.EnsureAnimationRoots(actor, new RagdollAutoWire.Report());

        var setManager = actor.GetComponent<RagdollAnimationSetManager>();
        var ikManager = actor.GetComponent<RagdollIKAnimationManager>();
        Assert.IsNotNull(setManager);
        Assert.IsNotNull(ikManager);
        Assert.AreSame(setManager, ikManager.animationSetManager);

        var ragdoll = actor.GetComponent<RagdollSystem>();
        Assert.IsNotNull(ragdoll.animationContainer);
        Assert.IsNotNull(ragdoll.animationTree);

        Transform animRoot = ragdoll.animationTree.transform.Find("AnimationRoot");
        Assert.IsNotNull(animRoot);
        Assert.IsNotNull(animRoot.GetComponent<AnimationBehaviorTreeNode>());
        Assert.IsNotNull(ragdoll.animationTree.rootNode);

        RagdollAutoWire.EnsureAnimationRoots(actor, new RagdollAutoWire.Report());
        Assert.AreEqual(1, actor.GetComponentsInChildren<RagdollAnimationSetManager>(true).Length);
        Assert.AreEqual(1, actor.GetComponentsInChildren<RagdollIKAnimationManager>(true).Length);
        Assert.AreEqual(1, CountNamed(actor, "AnimationRoot"));

        Object.DestroyImmediate(actor);
    }

    [Test]
    public void RepairRagdoll_IncludesHairAndAnimation()
    {
        var actor = new GameObject("RepairActor");
        actor.SetActive(false);
        var head = new GameObject("Head");
        head.transform.SetParent(actor.transform, false);
        var system = actor.AddComponent<RagdollSystem>();
        system.headComponent = head.AddComponent<RagdollHead>();

        var report = new RagdollAutoWire.Report();
        RagdollAutoWire.RepairRagdoll(actor, report);

        Assert.IsNotNull(actor.GetComponentInChildren<Brain>(true));
        Assert.IsNotNull(actor.GetComponentInChildren<HairPlumePhysicsDriver>(true));
        Assert.IsNotNull(actor.GetComponentInChildren<RagdollAnimationSetManager>(true));
        Assert.IsNotNull(actor.GetComponentInChildren<AnimationBehaviorTree>(true));

        string configPath = null;
        var driver = actor.GetComponentInChildren<HairPlumePhysicsDriver>(true);
        if (driver != null && driver.config != null)
            configPath = AssetDatabase.GetAssetPath(driver.config);

        Object.DestroyImmediate(actor);
        if (!string.IsNullOrEmpty(configPath))
            AssetDatabase.DeleteAsset(configPath);
    }

    static int CountNamed(GameObject root, string name)
    {
        int n = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name) n++;
        }
        return n;
    }
}
#endif
