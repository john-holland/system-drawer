#if UNITY_EDITOR
using SystemDrawer.Quest;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QuestServiceWizard))]
public class QuestServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (QuestServiceWizard)target;
        EditorGUILayout.Space();
        WizardStandardAssetsUi.DrawSetupSection(w,
            "Creates QuestRunner and QuestMapRenderer under _StandardScene and wires wizard references.");
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
