using UnityEditor;
using UnityEngine;

namespace Roads.Editor
{
    [CustomEditor(typeof(RoadSpline4D))]
    public class RoadSpline4DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var rs4d = (RoadSpline4D)target;
            if (GUILayout.Button("Export Snapshot"))
            {
                var snap = rs4d.ExportSnapshot();
                Debug.Log($"Road snapshot: {snap.roadSegmentId}, {snap.controlPoints.Count} points");
            }
            if (GUILayout.Button("Bake To 3D"))
            {
                var rs3d = rs4d.GetComponent<RoadSpline3D>() ?? rs4d.gameObject.AddComponent<RoadSpline3D>();
                rs4d.BakeTo3D(rs3d);
                EditorUtility.SetDirty(rs3d);
            }
        }
    }
}
