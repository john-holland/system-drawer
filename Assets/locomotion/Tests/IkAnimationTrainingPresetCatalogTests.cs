#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class IkAnimationTrainingPresetCatalogTests
{
    [Test]
    public void Build_EmptyCatalog_ReturnsEmpty()
    {
        List<IkAnimationTrainingPresetEntry> entries = IkAnimationTrainingPresetCatalog.Build(null);
        Assert.IsNotNull(entries);
        Assert.AreEqual(0, entries.Count);

        entries = IkAnimationTrainingPresetCatalog.Build(new List<RagdollAnimationSet>());
        Assert.AreEqual(0, entries.Count);
    }

    [Test]
    public void Build_SingleSet_LabelAndDetail()
    {
        var go = new GameObject("abt");
        var tree = go.AddComponent<AnimationBehaviorTree>();
        tree.clipConfigurations = new List<ABTClipConfig>
        {
            new ABTClipConfig
            {
                displayName = "walk_cycle",
                testCategory = PhysicsIKTrainingCategory.ToolUse,
                initialPoseMode = IKTrainingInitialPoseMode.TPose
            }
        };
        tree.activeClipIndex = 0;

        var catalog = new List<RagdollAnimationSet>
        {
            new RagdollAnimationSet
            {
                displayName = "Walk",
                animationTree = tree,
                transitionSettings = new RagdollAnimationTransitionSettings { blendDuration = 0.5f }
            }
        };

        List<IkAnimationTrainingPresetEntry> entries = IkAnimationTrainingPresetCatalog.Build(catalog);
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual("Walk", entries[0].label);
        Assert.AreEqual(0, entries[0].catalogIndex);
        StringAssert.Contains("ToolUse", entries[0].detail);
        StringAssert.Contains("TPose", entries[0].detail);
        StringAssert.Contains("walk_cycle", entries[0].detail);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void Build_MultipleSets_PreservesIndices()
    {
        var catalog = new List<RagdollAnimationSet>
        {
            new RagdollAnimationSet { displayName = "A" },
            new RagdollAnimationSet { displayName = "B" },
            new RagdollAnimationSet { displayName = "C" }
        };

        List<IkAnimationTrainingPresetEntry> entries = IkAnimationTrainingPresetCatalog.Build(catalog);
        Assert.AreEqual(3, entries.Count);
        for (int i = 0; i < 3; i++)
            Assert.AreEqual(i, entries[i].catalogIndex);
    }

    [Test]
    public void ApplyToTraining_CopiesCategoryAndPose()
    {
        var go = new GameObject("abt");
        var tree = go.AddComponent<AnimationBehaviorTree>();
        tree.clipConfigurations = new List<ABTClipConfig>
        {
            new ABTClipConfig
            {
                testCategory = PhysicsIKTrainingCategory.Throw,
                initialPoseMode = IKTrainingInitialPoseMode.HPose
            }
        };
        tree.activeClipIndex = 0;

        var set = new RagdollAnimationSet { displayName = "Throw", animationTree = tree };
        var runAsset = ScriptableObject.CreateInstance<PhysicsIKTrainingRunAsset>();
        AnimationBehaviorTree animationTree = null;
        PhysicsIKTrainingCategory category = PhysicsIKTrainingCategory.Locomotion;

        IkAnimationTrainingPresetCatalog.ApplyToTraining(ref animationTree, ref category, runAsset, set);

        Assert.AreEqual(tree, animationTree);
        Assert.AreEqual(PhysicsIKTrainingCategory.Throw, category);
        Assert.AreEqual(IKTrainingInitialPoseMode.HPose, runAsset.initialPoseMode);

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(runAsset);
    }

    [Test]
    public void ApplyToTraining_SetsAnimationTree()
    {
        var go = new GameObject("abt");
        var tree = go.AddComponent<AnimationBehaviorTree>();
        var set = new RagdollAnimationSet { displayName = "Run", animationTree = tree };
        AnimationBehaviorTree animationTree = null;
        PhysicsIKTrainingCategory category = PhysicsIKTrainingCategory.Locomotion;

        IkAnimationTrainingPresetCatalog.ApplyToTraining(ref animationTree, ref category, null, set);

        Assert.AreEqual(tree, animationTree);

        Object.DestroyImmediate(go);
    }
}
#endif
