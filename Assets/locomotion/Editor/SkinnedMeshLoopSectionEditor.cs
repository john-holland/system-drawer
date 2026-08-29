using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkinnedMeshLoopSection))]
public sealed class SkinnedMeshLoopSectionEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var section = (SkinnedMeshLoopSection)target;
        DrawDefaultInspector();
        section.RefreshMeshUpdated();

        if (section.meshUpdated && !section.useCached)
            EditorGUILayout.HelpBox(
                "Mesh or textures changed (meshUpdated). Loops will not apply until you check useCached or overwrite the saved cache.",
                MessageType.Error);
        else if (section.meshUpdated && section.useCached)
            EditorGUILayout.HelpBox("use cached — authored loops stay on the original hashes until overwrite.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(!section.CanSetUseCached))
        {
            bool next = EditorGUILayout.Toggle("Use Cached", section.useCached);
            if (next != section.useCached)
            {
                Undo.RecordObject(section, "Use Cached");
                if (next)
                    section.ApplyUseCachedSnapshot();
                else
                    section.useCached = false;
                EditorUtility.SetDirty(section);
                if (section.sectionAsset != null)
                    EditorUtility.SetDirty(section.sectionAsset);
            }
        }

        if (GUILayout.Button("Open Skinned Loop Section Window"))
            SkinnedMeshLoopSectionWindow.Open(section);
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.Active | GizmoType.NonSelected)]
    static void DrawLoopGizmo(SkinnedMeshLoopSection section, GizmoType type)
    {
        if (section == null || section.sectionAsset == null)
            return;
        var smr = section.Renderer;
        if (smr == null)
            return;
        if (!section.TryGetWorkingMesh(out var mesh) || mesh == null)
            return;
        Vector3[] verts = mesh.vertices;
        Matrix4x4 l2w = smr.transform.localToWorldMatrix;
        var loops = section.sectionAsset.loops;
        if (loops == null)
            return;
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.8f);
        for (int li = 0; li < loops.Count; li++)
        {
            var loop = loops[li];
            if (loop == null || loop.vertexIndices == null || loop.vertexIndices.Count < 2)
                continue;
            for (int i = 0; i < loop.vertexIndices.Count; i++)
            {
                int a = loop.vertexIndices[i];
                int b = loop.vertexIndices[(i + 1) % loop.vertexIndices.Count];
                if (a < 0 || b < 0 || a >= verts.Length || b >= verts.Length)
                    continue;
                Gizmos.DrawLine(l2w.MultiplyPoint3x4(verts[a]), l2w.MultiplyPoint3x4(verts[b]));
            }
        }
    }
}
