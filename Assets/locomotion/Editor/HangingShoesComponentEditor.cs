using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HangingShoesComponent))]
public sealed class HangingShoesComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Selected gizmos draw lace spline control points. Assign RoadSpline3D (or any spline with controlPoints) on laceSplines / Shoe.laces.", MessageType.Info);
    }
}
