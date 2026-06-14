#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Creates the default mode-transition activation prefab under Assets/locomotion/Prefabs/Travel/.</summary>
public static class TravelModeTransitionActivationPrefabCreator
{
    const string PrefabPath = "Assets/locomotion/Prefabs/Travel/ModeTransitionActivation.prefab";

    [MenuItem("Locomotion/Travel/Create Mode Transition Activation Prefab")]
    public static void CreatePrefab()
    {
        var root = new GameObject("ModeTransitionActivation");
        root.AddComponent<TravelActivationSequenceNode>();

        var legAnimGo = new GameObject("ApplyTravelLegAnimation");
        legAnimGo.transform.SetParent(root.transform, false);
        legAnimGo.AddComponent<ApplyTravelLegAnimationNode>();

        var transGo = new GameObject("ApplyTravelModeTransition");
        transGo.transform.SetParent(root.transform, false);
        transGo.AddComponent<ApplyTravelModeTransitionNode>();

        var driveGo = new GameObject("ApplyDrivePhase");
        driveGo.transform.SetParent(root.transform, false);
        driveGo.AddComponent<ApplyDrivePhaseNode>();

        System.IO.Directory.CreateDirectory("Assets/locomotion/Prefabs/Travel");
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        Debug.Log($"Created travel activation prefab at {PrefabPath}");
    }
}
#endif
