using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SpatialGeneratorBase), true)]
public class SpatialGeneratorBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SpatialGeneratorBase gen = (SpatialGeneratorBase)target;
        string typeName = gen is SpatialGenerator4D ? "4D" : "3D";
        EditorGUILayout.LabelField("Generator type", typeName, EditorStyles.boldLabel);
        if (GUILayout.Button("Find orchestrator in hierarchy", GUILayout.Height(20)))
        {
            var orch = gen.GetComponentInParent<SpatialGenerator4DOrchestrator>();
            if (orch == null)
                orch = gen.GetComponentInChildren<SpatialGenerator4DOrchestrator>();
            if (orch != null)
                Selection.activeObject = orch;
            else
                Debug.Log("[SpatialGeneratorBaseEditor] No SpatialGenerator4DOrchestrator found in hierarchy.");
        }
        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
