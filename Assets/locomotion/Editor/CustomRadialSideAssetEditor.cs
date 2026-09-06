#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomRadialSideAsset))]
public sealed class CustomRadialSideAssetEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var asset = (CustomRadialSideAsset)target;
        DrawDefaultInspector();
        EditorGUILayout.Space(4f);
        var piece = (GameObject)EditorGUILayout.ObjectField(
            "Recognize from piece", asset.recognizedFromPiece, typeof(GameObject), true);
        if (piece != asset.recognizedFromPiece)
        {
            Undo.RecordObject(asset, "Set recognize piece");
            asset.recognizedFromPiece = piece;
            EditorUtility.SetDirty(asset);
        }
        if (GUILayout.Button("Recognize and resize JointMiddle / FlyAway"))
        {
            Mesh mesh = MeshFromPiece(asset.recognizedFromPiece);
            if (mesh == null)
            {
                EditorGUILayout.HelpBox("Assign a piece with MeshFilter or SkinnedMeshRenderer.", MessageType.Warning);
                return;
            }
            Undo.RecordObject(asset, "Recognize custom radial side");
            CustomRadialSideRecognizer.AutoResize(asset, mesh);
            EditorUtility.SetDirty(asset);
        }
    }

    static Mesh MeshFromPiece(GameObject go)
    {
        if (go == null)
            return null;
        var skin = go.GetComponentInChildren<SkinnedMeshRenderer>();
        if (skin != null && skin.sharedMesh != null)
            return skin.sharedMesh;
        var mf = go.GetComponentInChildren<MeshFilter>();
        return mf != null ? mf.sharedMesh : null;
    }
}
#endif
