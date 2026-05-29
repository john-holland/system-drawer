#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>Builds first- and third-person ragdoll player <see cref="BehaviorTree"/> prefabs under Assets/locomotion/Prefabs/PlayerRagdoll/.</summary>
public static class PlayerRagdollBehaviorTreePrefabMenu
{
    const string PrefabDir = "Assets/locomotion/Prefabs/PlayerRagdoll";

    [MenuItem("Locomotion/Create Player Ragdoll Behavior Tree Prefabs")]
    public static void CreatePrefabs()
    {
        if (!AssetDatabase.IsValidFolder("Assets/locomotion/Prefabs"))
            AssetDatabase.CreateFolder("Assets/locomotion", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/locomotion/Prefabs", "PlayerRagdoll");

        var fp = BuildTreeRoot(isThirdPerson: false, "FirstPersonRagdollControllerBehaviorTree");
        PrefabUtility.SaveAsPrefabAsset(fp, $"{PrefabDir}/FirstPersonRagdollControllerBehaviorTree.prefab");
        Object.DestroyImmediate(fp);

        var tp = BuildTreeRoot(isThirdPerson: true, "ThirdPersonRagdollControllerBehaviorTree");
        PrefabUtility.SaveAsPrefabAsset(tp, $"{PrefabDir}/ThirdPersonRagdollControllerBehaviorTree.prefab");
        Object.DestroyImmediate(tp);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[Locomotion] Saved player ragdoll BT prefabs under {PrefabDir}. Wire camera / pivot references on look nodes in the inspector.");
    }

    static GameObject BuildTreeRoot(bool isThirdPerson, string rootName)
    {
        var root = new GameObject(rootName);
        var buffer = root.AddComponent<RagdollPlayerInputBuffer>();
        buffer.options = new RagdollPlayerControllerOptions();

        var bt = root.AddComponent<BehaviorTree>();
        bt.decisionTime = 0f;

        var sequence = root.AddComponent<RagdollPlayerSequenceNode>();
        bt.rootNode = sequence;

        var read = AddChildNode<ReadRagdollPlayerMovementInputNode>(root, "ReadMovementInput");
        BehaviorTreeNode look = isThirdPerson
            ? AddChildNode<ThirdPersonCameraOrbitNode>(root, "ThirdPersonCameraOrbit")
            : AddChildNode<MouseLookFirstPersonNode>(root, "MouseLookFirstPerson");
        var move = AddChildNode<ApplyRagdollLocomotionNode>(root, "ApplyRagdollLocomotion");
        var anim = AddChildNode<DriveLocomotionAnimationNode>(root, "DriveLocomotionAnimation");

        sequence.children = new List<BehaviorTreeNode> { read, look, move, anim };
        return root;
    }

    static T AddChildNode<T>(GameObject root, string name) where T : BehaviorTreeNode
    {
        var go = new GameObject(name);
        go.transform.SetParent(root.transform, false);
        return go.AddComponent<T>();
    }
}
#endif
