#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MenuRagdollServiceWizard))]
public class MenuRagdollServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (MenuRagdollServiceWizard)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Menu ragdoll is scene-specific (main menu layout). Assign menuRagdoll and menuGenerator manually.",
            MessageType.Info);
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
