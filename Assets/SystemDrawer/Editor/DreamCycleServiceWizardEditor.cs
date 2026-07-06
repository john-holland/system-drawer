#if UNITY_EDITOR
using SystemDrawer.DreamCycle;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DreamCycleServiceWizard))]
public class DreamCycleServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var w = (DreamCycleServiceWizard)target;
        EditorGUILayout.Space();
        WizardStandardAssetsUi.DrawSetupSection(w,
            "Creates DreamCycle runners, sleep renderer, DefaultNeedAspectRegistry asset, and wires wizard refs.");
    }
}
#endif
