using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// One animation-set option for IK training preset UI (label, detail, catalog index).
/// </summary>
public struct IkAnimationTrainingPresetEntry
{
    public int catalogIndex;
    public string label;
    public string detail;
    public RagdollAnimationSet set;
}

/// <summary>
/// Builds preset entries from a ragdoll animation catalog and applies clip config to training fields.
/// </summary>
public static class IkAnimationTrainingPresetCatalog
{
    public static List<IkAnimationTrainingPresetEntry> Build(IReadOnlyList<RagdollAnimationSet> catalog)
    {
        var entries = new List<IkAnimationTrainingPresetEntry>();
        if (catalog == null)
            return entries;

        for (int i = 0; i < catalog.Count; i++)
        {
            RagdollAnimationSet set = catalog[i];
            if (set == null)
                continue;

            entries.Add(new IkAnimationTrainingPresetEntry
            {
                catalogIndex = i,
                label = ResolveLabel(set, i),
                detail = BuildDetail(set),
                set = set
            });
        }

        return entries;
    }

    public static void ApplyToTraining(
        ref AnimationBehaviorTree animationTree,
        ref PhysicsIKTrainingCategory testCategory,
        PhysicsIKTrainingRunAsset runAsset,
        RagdollAnimationSet set)
    {
        if (set == null)
            return;

        if (set.animationTree != null)
            animationTree = set.animationTree;

        ABTClipConfig config = set.animationTree != null ? set.animationTree.GetActiveConfiguration() : null;
        if (config != null)
        {
            testCategory = config.testCategory;
            if (runAsset != null)
                runAsset.initialPoseMode = config.initialPoseMode;
        }
    }

    static string ResolveLabel(RagdollAnimationSet set, int index)
    {
        if (!string.IsNullOrEmpty(set.displayName))
            return set.displayName.Trim();
        if (set.animationTree != null && !string.IsNullOrEmpty(set.animationTree.name))
            return set.animationTree.name;
        return $"Animation {index}";
    }

    static string BuildDetail(RagdollAnimationSet set)
    {
        var sb = new StringBuilder(128);
        ABTClipConfig config = set.animationTree != null ? set.animationTree.GetActiveConfiguration() : null;

        PhysicsIKTrainingCategory category = config != null
            ? config.testCategory
            : PhysicsIKTrainingCategory.Locomotion;
        IKTrainingInitialPoseMode pose = config != null
            ? config.initialPoseMode
            : IKTrainingInitialPoseMode.FirstFrame;

        sb.Append(category);
        sb.Append(" · ");
        sb.Append(pose);

        string clipName = ResolveClipName(set, config);
        if (!string.IsNullOrEmpty(clipName))
        {
            sb.Append(" · clip: ");
            sb.Append(clipName);
        }

        RagdollAnimationTransitionSettings transition = set.transitionSettings;
        if (transition != null)
        {
            sb.Append(" · blend ");
            sb.Append(transition.blendDuration.ToString("0.##"));
            sb.Append("s ");
            sb.Append(transition.blendMode);
        }

        return sb.ToString();
    }

    static string ResolveClipName(RagdollAnimationSet set, ABTClipConfig config)
    {
        if (config?.clip != null)
            return config.clip.name;
        if (!string.IsNullOrEmpty(config?.displayName))
            return config.displayName;
        if (set.animationTree?.animationClip != null)
            return set.animationTree.animationClip.name;
        return null;
    }
}
