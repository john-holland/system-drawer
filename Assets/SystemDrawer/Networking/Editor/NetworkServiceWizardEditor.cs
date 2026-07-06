#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NetworkServiceWizard))]
public class NetworkServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (NetworkServiceWizard)target;
        EditorGUILayout.Space();
        WizardStandardAssetsUi.DrawSetupSection(w,
            "Creates _Networking with ClientOrchestrator and ServerOrchestrator plus DefaultNetworkSettings asset.");
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
    }
}
#endif
