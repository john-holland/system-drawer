#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Editor utilities to bake a transform hierarchy into <see cref="TravelFormationAsset"/> and import JSON.</summary>
public static class TravelFormationEditorMenu
{
    const string DefaultBakePath = "Assets/locomotion/travel/Formations";

    [MenuItem("Locomotion/Travel/Bake formation from transform", false, 200)]
    public static void BakeFormationFromSelection()
    {
        Transform root = Selection.activeTransform;
        if (root == null)
        {
            EditorUtility.DisplayDialog("Bake formation", "Select a root transform (formation anchor) in the hierarchy.", "OK");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/locomotion/travel"))
            AssetDatabase.CreateFolder("Assets/locomotion", "travel");
        if (!AssetDatabase.IsValidFolder(DefaultBakePath))
            AssetDatabase.CreateFolder("Assets/locomotion/travel", "Formations");

        string suggested = Path.Combine(DefaultBakePath, root.gameObject.name + "Formation.asset").Replace('\\', '/');
        string path = EditorUtility.SaveFilePanelInProject(
            "Save formation asset",
            root.gameObject.name + "Formation",
            "asset",
            "Choose where to save the TravelFormationAsset.",
            DefaultBakePath);

        if (string.IsNullOrEmpty(path))
            return;

        var asset = ScriptableObject.CreateInstance<TravelFormationAsset>();
        BakeHierarchyToAsset(root, asset);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"[TravelFormation] Baked {asset.SlotCount} slots from '{root.name}' to {path}");
    }

    [MenuItem("Locomotion/Travel/Import formation JSON to asset", false, 201)]
    public static void ImportJsonToAsset()
    {
        string jsonPath = EditorUtility.OpenFilePanel("Formation JSON", Application.dataPath, "json");
        if (string.IsNullOrEmpty(jsonPath))
            return;

        string json = File.ReadAllText(jsonPath);

        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save formation asset",
            Path.GetFileNameWithoutExtension(jsonPath) + "Formation",
            "asset",
            "Choose where to save the TravelFormationAsset.",
            DefaultBakePath);

        if (string.IsNullOrEmpty(savePath))
            return;

        if (!AssetDatabase.IsValidFolder(DefaultBakePath))
        {
            if (!AssetDatabase.IsValidFolder("Assets/locomotion/travel"))
                AssetDatabase.CreateFolder("Assets/locomotion", "travel");
            AssetDatabase.CreateFolder("Assets/locomotion/travel", "Formations");
        }

        var asset = ScriptableObject.CreateInstance<TravelFormationAsset>();
        if (!TravelFormationJsonLoader.TryApplyToAsset(asset, json, out string applyErr))
        {
            EditorUtility.DisplayDialog("Import formation JSON", applyErr, "OK");
            return;
        }

        AssetDatabase.CreateAsset(asset, savePath);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"[TravelFormation] Imported JSON to {savePath}");
    }

    /// <summary>Depth-first (sibling order): each descendant's position in <paramref name="root"/> local space.</summary>
    public static void BakeHierarchyToAsset(Transform root, TravelFormationAsset asset)
    {
        if (root == null || asset == null)
            return;
        asset.slots ??= new System.Collections.Generic.List<TravelFormationSlot>();
        asset.slots.Clear();

        void DepthFirst(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                Vector3 local = root.InverseTransformPoint(c.position);
                asset.slots.Add(new TravelFormationSlot { localOffset = local });
                DepthFirst(c);
            }
        }

        DepthFirst(root);
    }
}
#endif
