#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VehicleRoadCenterSpline))]
public sealed class VehicleRoadCenterSplineEditor : Editor
{
    void OnSceneGUI()
    {
        var spline = (VehicleRoadCenterSpline)target;
        if (spline.controlPoints == null || spline.controlPoints.Count == 0)
            return;

        Undo.RecordObject(spline, "Vehicle road center spline");
        bool changed = false;
        for (int i = 0; i < spline.controlPoints.Count; i++)
        {
            Vector3 p = spline.controlPoints[i];
            float size = HandleUtility.GetHandleSize(p) * (i == 0 || i == spline.controlPoints.Count - 1 ? 0.18f : 0.12f);
            Handles.color = i == 0 ? Color.green : (i == spline.controlPoints.Count - 1 ? Color.red : new Color(1f, 0.7f, 0.2f));
            EditorGUI.BeginChangeCheck();
            Vector3 next = Handles.PositionHandle(p, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                spline.controlPoints[i] = next;
                changed = true;
            }
            string label = i == 0 ? "Start" : (i == spline.controlPoints.Count - 1 ? "End" : "Ctrl " + i);
            Handles.Label(p + Vector3.up * size * 2f, label);
        }

        if (changed)
        {
            spline.RebuildLengthTable();
            EditorUtility.SetDirty(spline);
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var spline = (VehicleRoadCenterSpline)target;
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add control point"))
        {
            Undo.RecordObject(spline, "Add spline control");
            if (spline.controlPoints == null)
                spline.controlPoints = new System.Collections.Generic.List<Vector3>();
            Vector3 last = spline.controlPoints.Count > 0
                ? spline.controlPoints[spline.controlPoints.Count - 1]
                : spline.transform.position;
            spline.controlPoints.Add(last + Vector3.forward * 4f);
            spline.RebuildLengthTable();
            EditorUtility.SetDirty(spline);
        }
        if (GUILayout.Button("Rebuild length") && spline.controlPoints != null)
        {
            spline.RebuildLengthTable();
            EditorUtility.SetDirty(spline);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Length", spline.GetTotalLength().ToString("0.00") + " m");
    }
}
#endif
