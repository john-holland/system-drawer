using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Spatial4DServiceWizard))]
public class Spatial4DServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (Spatial4DServiceWizard)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Assign from System Drawer", GUILayout.Height(22)))
        {
            var service = SystemDrawerService.FindInScene();
            if (service != null)
            {
                var orch = service.Get<SpatialGenerator4DOrchestrator>(Spatial4DServiceWizard.ServiceKey);
                if (orch != null)
                {
                    Undo.RecordObject(w, "Assign from System Drawer");
                    w.orchestrator = orch;
                    EditorUtility.SetDirty(w);
                }
            }
        }
        if (w.orchestrator != null)
        {
            if (GUILayout.Button("Select Orchestrator", GUILayout.Height(22)))
                Selection.activeGameObject = w.orchestrator.gameObject;
            if (GUILayout.Button("Add 3D generator", GUILayout.Height(22)))
            {
                var child = new GameObject("SpatialGenerator3D");
                child.transform.SetParent(w.orchestrator.transform);
                child.AddComponent<SpatialGenerator>();
                w.orchestrator.spatialGenerators.Add(child.GetComponent<SpatialGeneratorBase>());
                Selection.activeGameObject = child;
                EditorUtility.SetDirty(w.orchestrator);
            }
            if (GUILayout.Button("Add 4D generator", GUILayout.Height(22)))
            {
                var child = new GameObject("SpatialGenerator4D");
                child.transform.SetParent(w.orchestrator.transform);
                child.AddComponent<SpatialGenerator4D>();
                w.orchestrator.spatialGenerators.Add(child.GetComponent<SpatialGeneratorBase>());
                Selection.activeGameObject = child;
                EditorUtility.SetDirty(w.orchestrator);
            }
        }
    }
}
