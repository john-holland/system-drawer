#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PrimitiveAssetStore), true)]
public class PrimitiveAssetStoreEditor : Editor
{
    public const string PrimitivesFolder = "Assets/Generated/Primitives";
    public const string StoreAssetName = "PrimitiveAssetStore.asset";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space(4);
        if (GUILayout.Button("Ensure Primitives folder and store", GUILayout.Height(22)))
        {
            EnsurePrimitivesFolderAndStore();
        }
    }

    public static void EnsurePrimitivesFolderAndStore()
    {
        if (!AssetDatabase.IsValidFolder("Assets")) return;
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
            AssetDatabase.CreateFolder("Assets", "Generated");
        if (!AssetDatabase.IsValidFolder("Assets/Generated/Primitives"))
            AssetDatabase.CreateFolder("Assets/Generated", "Primitives");

        string storePath = PrimitivesFolder + "/" + StoreAssetName;
        var existing = AssetDatabase.LoadAssetAtPath<PrimitiveAssetStore>(storePath);
        if (existing == null)
        {
            var store = CreateInstance<PrimitiveAssetStore>();
            store.primitivesFolder = PrimitivesFolder;
            AssetDatabase.CreateAsset(store, storePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[PrimitiveAssetStore] Created store at " + storePath);
        }
    }
}
#endif
