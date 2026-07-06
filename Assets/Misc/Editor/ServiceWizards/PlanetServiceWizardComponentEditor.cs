#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlanetServiceWizardComponent))]
public class PlanetServiceWizardComponentEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (PlanetServiceWizardComponent)target;
        EditorGUILayout.Space();
        WizardStandardAssetsUi.DrawSetupSection(w,
            "Creates PlanetSystem with PlanetBody (Little Prince preset), composition library asset, and wires planetSystemObject.");
        EditorGUILayout.Space();
        if (GUILayout.Button("Assign from System Drawer", GUILayout.Height(22)))
        {
            var service = SystemDrawerService.FindInScene();
            if (service != null)
            {
                Undo.RecordObject(w, "Assign from System Drawer");
                if (w.TryCompleteFromService())
                    EditorUtility.SetDirty(w);
            }
        }
        if (GUILayout.Button("Spawn asteroid belt around planet", GUILayout.Height(22)))
        {
            var host = w.SpawnAsteroidBeltAroundPlanet();
            if (host != null)
                Selection.activeGameObject = host;
        }
    }
}
#endif
