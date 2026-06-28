#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RopeSystem))]
public class RopeSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var sys = (RopeSystem)target;
        if (!Application.isPlaying)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Runtime", EditorStyles.boldLabel);
        EditorGUILayout.FloatField("Normalized load", sys.NormalizedLoad);
        EditorGUILayout.FloatField("Max tension (N)", sys.MaxTensionN);
        EditorGUILayout.Toggle("Snapped", sys.IsSnapped);
    }

    void OnSceneGUI()
    {
        var sys = (RopeSystem)target;
        if (!Application.isPlaying || sys.Arc == null)
            return;

        Handles.color = Color.cyan;
        Handles.Label(sys.transform.position + Vector3.up * 0.5f,
            $"Wound {sys.Arc.WoundLengthM:F2}m / {sys.Arc.TotalLength:F2}m  load {sys.NormalizedLoad:P0}");

        var footprint = sys.GetComponent<RopePathingFootprint>();
        if (footprint != null)
        {
            footprint.RebuildSamples();
            Handles.color = new Color(1f, 0.6f, 0.1f, 0.8f);
            foreach (Vector3 p in footprint.BodySamples)
                Handles.DrawWireDisc(p, Vector3.up, footprint.SampleRadiusM);
        }

        if (sys.OverlapIndex != null)
        {
            Handles.color = Color.red;
            foreach (RopeOverlapEntry e in sys.OverlapIndex.Entries)
                Handles.DrawLine(e.contactPoint, e.contactPoint + e.normal * 0.15f);
        }
    }
}
#endif
