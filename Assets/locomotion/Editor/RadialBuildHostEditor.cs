#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RadialBuildHost))]
public sealed class RadialBuildHostEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var host = (RadialBuildHost)target;
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("centerPost"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("customSide"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("customAngle"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("customAngleObject"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pieceSize"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("spec"), true);
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(4f);
        if (GUILayout.Button("Create Anchor Objects"))
        {
            Undo.RecordObject(host, "Create radial anchors");
            host.CreateAnchorObjects();
            EditorUtility.SetDirty(host);
            if (host.centerPost != null)
                Selection.activeGameObject = host.StartPostBounds != null
                    ? host.StartPostBounds.gameObject
                    : host.centerPost;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Snap startPostAnchor from bounds"))
        {
            Undo.RecordObject(host, "Snap start post");
            host.SnapStartPostFromBounds();
            EditorUtility.SetDirty(host);
        }
        if (GUILayout.Button("Refresh solved joints"))
        {
            host.RefreshSolved();
            EditorUtility.SetDirty(host);
        }
        EditorGUILayout.EndHorizontal();

        var labels = host.PreviewLabels();
        if (labels.Length == 0)
        {
            EditorGUILayout.HelpBox("Preview configuration: none (no start-post match or no working joints).", MessageType.Info);
            return;
        }

        int next = EditorGUILayout.Popup("Preview configuration", host.previewConfigIndex, labels);
        if (next != host.previewConfigIndex)
        {
            Undo.RecordObject(host, "Select radial preview");
            host.previewConfigIndex = next;
            if (next >= 0 && next < host.solvedConfigs.Count && host.spec != null)
                host.spec.ApplySolved(host.solvedConfigs[next]);
            EditorUtility.SetDirty(host);
        }
    }

    void OnSceneGUI()
    {
        var host = (RadialBuildHost)target;
        var bounds = host.StartPostBounds;
        if (bounds == null)
            return;
        Handles.color = new Color(1f, 0.55f, 0.1f, 1f);
        Vector3 p = bounds.transform.position;
        Vector3 f = bounds.FacingVector();
        Handles.ArrowHandleCap(0, p, Quaternion.LookRotation(f.sqrMagnitude > 1e-8f ? f : Vector3.forward),
            HandleUtility.GetHandleSize(p) * 0.45f, EventType.Repaint);
    }
}
#endif
