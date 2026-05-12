using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TravelAgent))]
public class TravelAgentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ta = (TravelAgent)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh discovered nodes"))
        {
            ta.RefreshDiscoveredNodes();
            EditorUtility.SetDirty(ta);
        }

        if (GUILayout.Button("Rebuild preview plan"))
        {
            ta.RebuildCachedPlan();
            EditorUtility.SetDirty(ta);
            SceneView.RepaintAll();
        }

        if (GUILayout.Button("Open Pathing Editor"))
            TravelPathingEditorWindow.Open(ta);

        if (ta.DiscoveredNodes != null && ta.DiscoveredNodes.Count > 0)
            EditorGUILayout.LabelField($"Discovered nodes (last refresh): {ta.DiscoveredNodes.Count}");
    }

    void OnSceneGUI()
    {
        var ta = (TravelAgent)target;
        if (ta == null || !ta.drawTravelGizmos)
            return;
        TravelAgentSceneHandles.DrawCachedPlan(ta);
    }
}
