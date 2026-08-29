using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkinnedMeshLoopSplitBounds))]
public sealed class SkinnedMeshLoopSplitBoundsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var box = (SkinnedMeshLoopSplitBounds)target;
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sectionAsset"), new GUIContent("Section Asset"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("meshPrefab"), new GUIContent("Mesh Prefab"));
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loopId"), new GUIContent("Loop Id"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("loopName"), new GUIContent("Loop Name"));
        EditorGUI.EndDisabledGroup();
        serializedObject.ApplyModifiedProperties();
        if (box.sectionAsset != null && !string.IsNullOrEmpty(box.loopId))
        {
            var loop = box.sectionAsset.GetLoop(box.loopId);
            if (loop != null && loop.displayName != box.loopName)
            {
                Undo.RecordObject(box, "Sync loop name");
                box.Associate(box.loopId, loop.displayName, box.sectionAsset, box.meshPrefab);
                EditorUtility.SetDirty(box);
            }
        }

        EditorGUILayout.Space();
        bool canUpdate = SkinnedMeshLoopSectionWindow.CanUpdateLoopTrianglesFromBounds(box);
        using (new EditorGUI.DisabledScope(!canUpdate))
        {
            if (GUILayout.Button("Update Loop Triangles"))
            {
                if (SkinnedMeshLoopSectionWindow.TryUpdateLoopTrianglesFromBounds(box, out string result))
                    Debug.Log("[SkinnedMeshLoopSplitBounds] Updated " + result + " for " + box.loopName, box);
                else
                    EditorUtility.DisplayDialog("Split Bounds", result ?? "Could not update loop triangles.", "OK");
            }
        }
        if (SkinnedMeshLoopSectionWindow.FindOpen() == null)
            EditorGUILayout.HelpBox(
                "Open Skinned Loop Section for this mesh to update assigned triangles from the current bounds pose.",
                MessageType.Info);
        else if (!canUpdate)
            EditorGUILayout.HelpBox(
                "The open loop editor is not targeting this bounds.",
                MessageType.Warning);
    }
}
