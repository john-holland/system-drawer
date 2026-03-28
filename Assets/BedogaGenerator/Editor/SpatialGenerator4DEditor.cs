#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;
using Locomotion.Narrative.EditorTools;

[CustomEditor(typeof(SpatialGenerator4D))]
public class SpatialGenerator4DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SpatialGenerator4D gen = (SpatialGenerator4D)target;
        EditorGUILayout.LabelField("Generator type", "4D", EditorStyles.boldLabel);

        if (GUILayout.Button("Find orchestrator in hierarchy", GUILayout.Height(20)))
        {
            var orch = gen.GetComponentInParent<SpatialGenerator4DOrchestrator>();
            if (orch == null)
                orch = gen.GetComponentInChildren<SpatialGenerator4DOrchestrator>();
            if (orch != null)
                Selection.activeObject = orch;
            else
                Debug.Log("[SpatialGenerator4DEditor] No SpatialGenerator4DOrchestrator found in hierarchy.");
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prompt Tree Inspector", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Open Prompt Tree Inspector", GUILayout.Height(22)))
        {
            PromptTreeInspectorWindow.ShowWindow(gen);
        }

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
#endif
