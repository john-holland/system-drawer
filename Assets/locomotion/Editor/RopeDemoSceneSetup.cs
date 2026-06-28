#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RopeDemoSceneSetup
{
    [MenuItem("GameObject/Locomotion/Rope System Demo", false, 10)]
    public static void CreateDemo()
    {
        var root = new GameObject("RopeSystemDemo");
        var head = new GameObject("HeadAnchor");
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 5f, 0f);

        var spool = new GameObject("SpoolAnchor");
        spool.transform.SetParent(root.transform);
        spool.transform.localPosition = new Vector3(0f, 5f, 0f);
        spool.tag = "RopeSpool";

        var anchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        anchor.name = "GrappleAnchor";
        anchor.transform.SetParent(root.transform);
        anchor.transform.localPosition = new Vector3(3f, 1f, 0f);
        anchor.transform.localScale = Vector3.one * 0.3f;
        anchor.tag = "RopeAnchor";

        var rope = root.AddComponent<RopeSystem>();
        var serialized = new SerializedObject(rope);
        serialized.FindProperty("headAnchor").objectReferenceValue = head.transform;
        serialized.FindProperty("spoolAnchor").objectReferenceValue = spool.transform;
        serialized.FindProperty("unwindDirection").vector3Value = Vector3.down;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        root.AddComponent<RopePathingFootprint>();
        root.AddComponent<ConsiderRopeCards>();

        var meshGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        meshGo.name = "RopeVisual";
        meshGo.transform.SetParent(root.transform);
        meshGo.transform.localScale = new Vector3(0.08f, 5f, 0.08f);
        Object.DestroyImmediate(meshGo.GetComponent<Collider>());
        var renderer = meshGo.GetComponent<MeshRenderer>();
        var mat = new Material(Shader.Find("Locomotion/RopeStrainRadial"));
        renderer.sharedMaterial = mat;
        serialized.FindProperty("ropeMaterial").objectReferenceValue = mat;
        serialized.FindProperty("ropeMeshRenderer").objectReferenceValue = renderer;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = root;
        Undo.RegisterCreatedObjectUndo(root, "Create Rope Demo");

        string prefabDir = "Assets/locomotion/rope/Prefabs";
        if (!AssetDatabase.IsValidFolder("Assets/locomotion/rope/Prefabs"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/locomotion/rope"))
                AssetDatabase.CreateFolder("Assets/locomotion", "rope");
            AssetDatabase.CreateFolder("Assets/locomotion/rope", "Prefabs");
        }
        PrefabUtility.SaveAsPrefabAsset(root, prefabDir + "/RopeSystemDemo.prefab");
    }
}
#endif
