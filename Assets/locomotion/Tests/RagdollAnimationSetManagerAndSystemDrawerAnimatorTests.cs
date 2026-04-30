using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// Covers <see cref="RagdollAnimationSetManager"/> interaction with <see cref="SystemDrawerAnimator"/>
/// deferral, plus layer weights, <see cref="SystemDrawerAnimator.ManagesBehaviorTree"/>, snapshots, and assert play order.
/// </summary>
public class RagdollAnimationSetManagerAndSystemDrawerAnimatorTests
{
    private static void InvokeTickLayers(SystemDrawerAnimator animator)
    {
        MethodInfo mi = typeof(SystemDrawerAnimator).GetMethod("TickLayers",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(mi);
        mi.Invoke(animator, null);
    }

    /// <summary>Minimal ABT + BehaviorTree with a <see cref="CleanupNode"/> root (Execute returns without throwing).</summary>
    private static AnimationBehaviorTree CreateAbtWithGeneratedTree(Transform parent, string objectName)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(parent);
        var abt = go.AddComponent<AnimationBehaviorTree>();
        var btHost = new GameObject("GeneratedBehaviorTree");
        btHost.transform.SetParent(go.transform);
        var bt = btHost.AddComponent<BehaviorTree>();
        var nodeGo = new GameObject("CleanupRoot");
        nodeGo.transform.SetParent(btHost.transform);
        var node = nodeGo.AddComponent<CleanupNode>();
        bt.rootNode = node;
        abt.generatedTree = bt;
        return abt;
    }

    [Test]
    public void RagdollAnimationSetManager_Play_IsDeferred_WhenSystemDrawerAnimatorDefers()
    {
        var root = new GameObject("root_defer");
        var manager = root.AddComponent<RagdollAnimationSetManager>();
        var animator = root.AddComponent<SystemDrawerAnimator>();
        animator.deferAnimationSetManagerPlayback = true;
        animator.ownsBehaviorTreeExecution = true;
        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 0, weight = 1f, animationBehaviorTree = null }
        };

        var set = new RagdollAnimationSet { displayName = "SetA" };
        manager.animationSets = new List<RagdollAnimationSet> { set };

        manager.Play(set);

        Assert.IsNull(manager.CurrentSet, "Play should return early and not assign CurrentSet when animator defers.");
        Object.DestroyImmediate(root);
    }

    [Test]
    public void RagdollAnimationSetManager_Play_AppliesSet_WhenDeferDisabled()
    {
        var root = new GameObject("root_nodefer");
        var manager = root.AddComponent<RagdollAnimationSetManager>();
        var animator = root.AddComponent<SystemDrawerAnimator>();
        animator.deferAnimationSetManagerPlayback = false;
        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 0, weight = 1f }
        };

        var set = new RagdollAnimationSet { displayName = "SetB" };
        manager.animationSets = new List<RagdollAnimationSet> { set };

        manager.Play(set);

        Assert.IsNotNull(manager.CurrentSet);
        Assert.AreEqual("SetB", manager.CurrentSet.displayName);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void RagdollAnimationSetManager_Play_AppliesSet_WhenAnimatorHasNoLayers()
    {
        var root = new GameObject("root_no_layers");
        var manager = root.AddComponent<RagdollAnimationSetManager>();
        var animator = root.AddComponent<SystemDrawerAnimator>();
        animator.layers = new List<AnimationLayerSlot>();

        var set = new RagdollAnimationSet { displayName = "SetC" };
        manager.animationSets = new List<RagdollAnimationSet> { set };

        manager.Play(set);

        Assert.IsNotNull(manager.CurrentSet);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void SystemDrawerAnimator_SetLayerWeight_GetLayerWeight_RoundTrip()
    {
        var root = new GameObject("root_weights");
        var animator = root.AddComponent<SystemDrawerAnimator>();
        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 3, weight = 1f }
        };

        animator.SetLayerWeight(3, 0.35f);
        Assert.AreEqual(0.35f, animator.GetLayerWeight(3), 1e-6f);
        Assert.AreEqual(-1f, animator.GetLayerWeight(99));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void SystemDrawerAnimator_ManagesBehaviorTree_WhenLayerWeightPositive()
    {
        var root = new GameObject("root_manage");
        var animator = root.AddComponent<SystemDrawerAnimator>();
        var abt = CreateAbtWithGeneratedTree(root.transform, "ABT");
        var bt = abt.generatedTree;
        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 0, weight = 1f, animationBehaviorTree = abt }
        };
        animator.ownsBehaviorTreeExecution = true;

        Assert.IsTrue(animator.ManagesBehaviorTree(bt));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void SystemDrawerAnimator_DoesNotManageBehaviorTree_WhenLayerWeightZero()
    {
        var root = new GameObject("root_noweight");
        var animator = root.AddComponent<SystemDrawerAnimator>();
        var abt = CreateAbtWithGeneratedTree(root.transform, "ABT2");
        var bt = abt.generatedTree;
        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 0, weight = 0f, animationBehaviorTree = abt }
        };
        animator.ownsBehaviorTreeExecution = true;

        Assert.IsFalse(animator.ManagesBehaviorTree(bt));
        Object.DestroyImmediate(root);
    }

    [Test]
    public void SystemDrawerAnimator_TickLayers_SkipsExecuteWhenWeightZero()
    {
        var root = new GameObject("root_tick_skip");
        var animator = root.AddComponent<SystemDrawerAnimator>();
        var abt = CreateAbtWithGeneratedTree(root.transform, "ABT3");
        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 0, weight = 0f, animationBehaviorTree = abt, displayName = "Z" }
        };

        InvokeTickLayers(animator);
        var snaps = animator.ActiveSnapshots;
        Assert.GreaterOrEqual(snaps.Count, 1);
        Assert.AreEqual(0f, snaps[0].weight);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void SystemDrawerAnimator_AssertPlayOrder_FailsHierarchy_WhenChildLayerTicksBeforeParent()
    {
        var root = new GameObject("root_hier_fail");
        var animator = root.AddComponent<SystemDrawerAnimator>();
        animator.strictPlayOrder = false;

        var parentAbt = CreateAbtWithGeneratedTree(root.transform, "ParentABT");
        var childAbt = CreateAbtWithGeneratedTree(parentAbt.transform, "ChildABT");

        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 0, weight = 1f, animationBehaviorTree = parentAbt },
            new AnimationLayerSlot { layerIndex = 1, weight = 1f, animationBehaviorTree = childAbt }
        };
        // Wrong evaluation order: child (layer 1) before parent (layer 0) — phases make parent tick after child.
        animator.playOrder = new List<int> { 1, 0 };

        LogAssert.Expect(LogType.Error, new Regex(@"\[SystemDrawerAnimator\].*Hierarchy order violation", RegexOptions.Singleline));

        InvokeTickLayers(animator);

        Assert.IsFalse(animator.LastAssertPassed);
        StringAssert.Contains("Hierarchy order violation", animator.LastAssertMessage);
        Object.DestroyImmediate(root);
    }

    [Test]
    public void SystemDrawerAnimator_AssertPlayOrder_Passes_WhenParentLayerBeforeChildInPlayOrder()
    {
        var root = new GameObject("root_hier_ok");
        var animator = root.AddComponent<SystemDrawerAnimator>();
        animator.strictPlayOrder = false;

        var parentAbt = CreateAbtWithGeneratedTree(root.transform, "ParentABT2");
        var childAbt = CreateAbtWithGeneratedTree(parentAbt.transform, "ChildABT2");

        animator.layers = new List<AnimationLayerSlot>
        {
            new AnimationLayerSlot { layerIndex = 0, weight = 1f, animationBehaviorTree = parentAbt },
            new AnimationLayerSlot { layerIndex = 1, weight = 1f, animationBehaviorTree = childAbt }
        };
        animator.playOrder = new List<int> { 0, 1 };

        InvokeTickLayers(animator);

        Assert.IsTrue(animator.LastAssertPassed);
        Assert.AreEqual("OK", animator.LastAssertMessage);
        Object.DestroyImmediate(root);
    }
}
