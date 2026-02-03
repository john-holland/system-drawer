using UnityEngine;
using UnityEditor;
using Locomotion.EditorTools;

[CustomEditor(typeof(RagdollServiceWizard))]
public class RagdollServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (RagdollServiceWizard)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Assign from System Drawer", GUILayout.Height(22)))
        {
            var service = SystemDrawerService.FindInScene();
            if (service != null)
            {
                var tr = service.Get<Transform>(RagdollServiceWizard.ServiceKey);
                if (tr != null)
                {
                    Undo.RecordObject(w, "Assign from System Drawer");
                    w.ragdollRoot = tr;
                    EditorUtility.SetDirty(w);
                }
            }
        }
        if (GUILayout.Button("Open Ragdoll Fitting Wizard", GUILayout.Height(22)))
            RagdollFittingWizardWindow.ShowWindow();
    }
}
