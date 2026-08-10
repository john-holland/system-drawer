#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlanarSplinePathLocomotion))]
public sealed class PlanarSplinePathLocomotionEditor : Editor
{
    int _gizmoSection = -1;
    Vector3 _snapPos, _snapEuler, _snapScale;
    bool _gizmosActive;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var path = (PlanarSplinePathLocomotion)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Rebuild planes"))
        {
            path.Rebuild();
            EditorUtility.SetDirty(path);
        }

        EditorGUILayout.LabelField("Custom section gizmos", EditorStyles.boldLabel);
        if (path.customSections == null || path.customSections.Count == 0)
        {
            EditorGUILayout.HelpBox("Add custom sections to enable gizmos.", MessageType.Info);
            return;
        }

        _gizmoSection = Mathf.Clamp(
            EditorGUILayout.IntSlider("Section", _gizmoSection < 0 ? 0 : _gizmoSection, 0,
                path.customSections.Count - 1),
            0, path.customSections.Count - 1);

        if (!_gizmosActive)
        {
            if (GUILayout.Button("Show transform gizmos"))
            {
                EnsureGizmo(path, _gizmoSection);
                var cs = path.customSections[_gizmoSection];
                _snapPos = cs.gizmoLocalPosition;
                _snapEuler = cs.gizmoLocalEuler;
                _snapScale = cs.gizmoLocalScale;
                _gizmosActive = true;
                Selection.activeTransform = cs.gizmoTransform;
            }
        }
        else
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save"))
                {
                    path.ApplyGizmoSave(_gizmoSection);
                    EditorUtility.SetDirty(path);
                    _gizmosActive = false;
                }
                if (GUILayout.Button("Revert"))
                {
                    path.ApplyGizmoRevert(_gizmoSection, _snapPos, _snapEuler, _snapScale);
                    EditorUtility.SetDirty(path);
                    _gizmosActive = false;
                }
            }
        }
    }

    static void EnsureGizmo(PlanarSplinePathLocomotion path, int index)
    {
        var cs = path.customSections[index];
        if (cs.gizmoTransform == null)
        {
            var go = new GameObject("PlanarSplineSectionGizmo_" + index);
            go.transform.SetParent(path.transform, false);
            cs.gizmoTransform = go.transform;
        }
        cs.gizmoTransform.localPosition = cs.gizmoLocalPosition;
        cs.gizmoTransform.localEulerAngles = cs.gizmoLocalEuler;
        cs.gizmoTransform.localScale = cs.gizmoLocalScale.sqrMagnitude < 1e-6f ? Vector3.one : cs.gizmoLocalScale;
    }
}
#endif
