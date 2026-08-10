using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the default parkour fall BehaviorTree (Sequence → PrepareLand + LimbPlacement).
/// Shared by the editor prefab menu and runtime bootstrap.
/// </summary>
public static class ParkourFallTreeFactory
{
    public const string DefaultTreeName = "ParkourFallBehaviorTree";
    public const string PrefabAssetPath = "Assets/locomotion/Prefabs/ActorRagdolls/ParkourFallBehaviorTree.prefab";
    public const string ResourcesAssetPath = "Assets/locomotion/Resources/ParkourFallBehaviorTree.prefab";
    public const string ResourcesLoadName = "ParkourFallBehaviorTree";

    public static BehaviorTree Build(Transform parent = null)
    {
        var root = new GameObject(DefaultTreeName);
        if (parent != null)
            root.transform.SetParent(parent, false);

        var bt = root.AddComponent<BehaviorTree>();
        bt.decisionTime = 0f;
        root.AddComponent<ParkourLandAnimationDriver>();

        var sequenceGo = new GameObject("ParkourFallSequence");
        sequenceGo.transform.SetParent(root.transform, false);
        var sequence = sequenceGo.AddComponent<RagdollPlayerSequenceNode>();
        bt.rootNode = sequence;

        var prepareGo = new GameObject("PrepareLandAnimation");
        prepareGo.transform.SetParent(sequenceGo.transform, false);
        var prepare = prepareGo.AddComponent<PrepareLandAnimationNode>();
        prepare.landDurationSeconds = 1.25f;
        prepare.useLandingGoalOverride = false;

        var placeGo = new GameObject("FallLimbPlacement");
        placeGo.transform.SetParent(sequenceGo.transform, false);
        var place = placeGo.AddComponent<ParkourFallLimbPlacementNode>();
        place.durationSec = 1.25f;
        place.animationGroupTag = ParkourAnimationGroup.FallRolls;
        place.fallCurve = new ParkourFallProceduralCurve();
        place.fallCurve.EnsureDefaultLimbs();
        place.landDriver = root.GetComponent<ParkourLandAnimationDriver>();

        sequence.children = new List<BehaviorTreeNode> { prepare, place };
        return bt;
    }
}
