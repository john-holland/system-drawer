#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Builds the default ragdoll get-up <see cref="BehaviorTree"/> prefab.</summary>
public static class RagdollGetUpBehaviorTreePrefabMenu
{
    const string PrefabDir = "Assets/locomotion/Prefabs/ActorRagdolls";
    const string ResourcesDir = "Assets/locomotion/Resources";

    [MenuItem("Locomotion/Create Ragdoll Get-Up Behavior Tree Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolder("Assets/locomotion", "Prefabs");
        EnsureFolder("Assets/locomotion/Prefabs", "ActorRagdolls");
        EnsureFolder("Assets/locomotion", "Resources");

        BehaviorTree bt = RagdollGetUpTreeFactory.Build();
        GameObject root = bt.gameObject;

        PrefabUtility.SaveAsPrefabAsset(root, RagdollGetUpTreeFactory.PrefabAssetPath);
        PrefabUtility.SaveAsPrefabAsset(root, RagdollGetUpTreeFactory.ResourcesAssetPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log(
            $"[Locomotion] Saved get-up BT prefab to {RagdollGetUpTreeFactory.PrefabAssetPath} " +
            $"and Resources copy for runtime Load.");
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
