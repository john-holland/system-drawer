using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TravelAgent))]
public class TravelAgentEditor : Editor
{
    static class Tips
    {
        public static readonly GUIContent RefreshDiscoveredNodes = new GUIContent(
            "Refresh discovered nodes",
            "Scan the actor hierarchy for BehaviorTreeNode components and cache snapshots for the Pathing Editor.");

        public static readonly GUIContent RebuildPreviewPlan = new GUIContent(
            "Rebuild preview plan",
            "Run traversibility and multibody solvers and refresh cached path gizmos in the Scene view.");

        public static readonly GUIContent OpenPathingEditor = new GUIContent(
            "Open Pathing Editor",
            "Open the Travel Pathing window focused on this TravelAgent.");

        public static readonly GUIContent Multibody = new GUIContent(
            "Multibody",
            "Convoy spacing, formation offsets, and peer avoidance applied after the base planner path.");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var ta = (TravelAgent)target;

        EditorGUILayout.Space();
        if (GUILayout.Button(Tips.RefreshDiscoveredNodes))
        {
            ta.RefreshDiscoveredNodes();
            EditorUtility.SetDirty(ta);
        }

        if (GUILayout.Button(Tips.RebuildPreviewPlan))
        {
            ta.RebuildCachedPlan();
            EditorUtility.SetDirty(ta);
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(Tips.Multibody, EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "When Multibody travel is enabled, rebuild runs TravelMultibodyPathAdjuster against peers (optionally limited to the same multibodyFormationGroupId) and dynamic colliders. With a formation asset + non-empty group id, waypoints are offset first (wrap rows default Back).",
            MessageType.None);

        if (GUILayout.Button(Tips.OpenPathingEditor))
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
