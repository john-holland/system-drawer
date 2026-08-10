#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>Locomotion menu — create parkour fall BT prefab (procedural fall curve + limb placement).</summary>
public static class ParkourFallBehaviorTreePrefabMenu
{
    [MenuItem("Locomotion/Create Parkour Fall Behavior Tree Prefab")]
    public static void CreatePrefab()
    {
        EnsureFolder("Assets/locomotion", "Prefabs");
        EnsureFolder("Assets/locomotion/Prefabs", "ActorRagdolls");
        EnsureFolder("Assets/locomotion", "Resources");

        BehaviorTree bt = ParkourFallTreeFactory.Build();
        GameObject root = bt.gameObject;

        // Materialize limb target children so the prefab ships ready for IK wiring.
        var place = root.GetComponentInChildren<ParkourFallLimbPlacementNode>();
        if (place != null)
            place.OnEnter(bt);

        PrefabUtility.SaveAsPrefabAsset(root, ParkourFallTreeFactory.PrefabAssetPath);
        PrefabUtility.SaveAsPrefabAsset(root, ParkourFallTreeFactory.ResourcesAssetPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var saved = AssetDatabase.LoadAssetAtPath<GameObject>(ParkourFallTreeFactory.PrefabAssetPath);
        if (saved != null)
            Selection.activeObject = saved;

        Debug.Log(
            "[Locomotion] Saved parkour fall BT prefab to " + ParkourFallTreeFactory.PrefabAssetPath +
            " and Resources copy for runtime Load. Tree: PrepareLandAnimation → FallLimbPlacement (procedural curves).");
    }

    [MenuItem("Locomotion/Add Parkour Fall Behavior Tree To Selection")]
    public static void AddToSelection()
    {
        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog(
                "Parkour Fall BT",
                "Select a GameObject (ragdoll / actor) to attach the parkour fall behavior tree.",
                "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(go, "Add Parkour Fall BT");
        BehaviorTree bt = ParkourFallTreeFactory.Build(go.transform);
        bt.gameObject.name = ParkourFallTreeFactory.DefaultTreeName;
        Selection.activeGameObject = bt.gameObject;
        EditorUtility.SetDirty(go);
        Debug.Log("[Locomotion] Added " + ParkourFallTreeFactory.DefaultTreeName + " under " + go.name);
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
