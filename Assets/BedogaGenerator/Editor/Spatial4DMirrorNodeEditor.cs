using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;
using Locomotion.Narrative.EditorTools;

[CustomEditor(typeof(Spatial4DMirrorNode))]
public class Spatial4DMirrorNodeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var node = (Spatial4DMirrorNode)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Copy Bounds4 to clipboard", GUILayout.Height(22)))
        {
            var b = node.Bounds4Value;
            string text = string.Format("center=({0:F2},{1:F2},{2:F2}) size=({3:F2},{4:F2},{5:F2}) tMin={6:F1} tMax={7:F1} label={8}",
                b.center.x, b.center.y, b.center.z, b.size.x, b.size.y, b.size.z, b.tMin, b.tMax, node.payloadLabel ?? "");
            EditorGUIUtility.systemCopyBuffer = text;
        }
        if (node.narrativeTreeAsset != null)
        {
            var treeAsset = node.narrativeTreeAsset as NarrativeTreeAsset;
            if (treeAsset != null && GUILayout.Button("Open in tree editor", GUILayout.Height(22)))
                NarrativeTreeEditorWindow.ShowWindow(treeAsset);
        }
    }
}
