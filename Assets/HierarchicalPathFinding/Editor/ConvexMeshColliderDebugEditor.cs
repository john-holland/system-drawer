#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ConvexMeshColliderDebug))]
public class ConvexMeshColliderDebugEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var dbg = (ConvexMeshColliderDebug)target;
        var mc = dbg.GetComponent<MeshCollider>();

        EditorGUILayout.Space(6);
        using (new EditorGUI.DisabledScope(mc == null))
        {
            if (GUILayout.Button("Bust cache & rebuild", GUILayout.Height(24)))
            {
                if (mc != null && mc.convex)
                {
                    Undo.RecordObject(dbg, "Bust convex mesh tree cache");
                    ConvexTreeMeshColliderService.Invalidate(mc);
                    dbg.TryRebuild(mc);
                    EditorUtility.SetDirty(dbg);
                    SceneView.RepaintAll();
                }
                else
                    Debug.LogWarning("[ConvexMeshColliderDebug] Requires convex MeshCollider.");
            }
        }

        if (mc != null && !mc.convex)
            EditorGUILayout.HelpBox("Convex mesh tree cache supports convex MeshCollider only.", MessageType.Warning);
    }
}
#endif
