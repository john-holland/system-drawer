#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Locomotion.Narrative;
using Locomotion.Narrative.EditorTools;

[CustomEditor(typeof(NarrativePromptServiceWizard))]
public class NarrativePromptServiceWizardEditor : Editor
{
    private SerializedProperty promptInterpreterProp;
    private SerializedProperty summarizerProp;
    private SerializedProperty calendarAssetProp;

    private void OnEnable()
    {
        promptInterpreterProp = serializedObject.FindProperty("promptInterpreter");
        summarizerProp = serializedObject.FindProperty("summarizer");
        calendarAssetProp = serializedObject.FindProperty("calendarAsset");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        var w = (NarrativePromptServiceWizard)target;

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Narrative LSTM Prompt", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Prompt interpreter turns natural language into narrative events; summarizer produces a short \"what's going on\" from the calendar. " +
            "Use the buttons below to wire references or create a full rig.",
            MessageType.Info);
        EditorGUILayout.Space(2);

        DrawDefaultInspector();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Service", EditorStyles.miniBoldLabel);
        if (GUILayout.Button("Assign from System Drawer", GUILayout.Height(22)))
        {
            var service = SystemDrawerService.FindInScene();
            if (service != null)
            {
                Undo.RecordObject(w, "Assign from System Drawer");
                if (w.TryCompleteFromService())
                    EditorUtility.SetDirty(w);
                else
                    EditorUtility.DisplayDialog("Narrative Prompt Wizard", "No Narrative LSTM or Calendar registered in System Drawer. Create an LSTM rig first, or add a Calendar wizard.", "OK");
            }
            else
                EditorUtility.DisplayDialog("Narrative Prompt Wizard", "No SystemDrawerService in scene.", "OK");
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Setup", EditorStyles.miniBoldLabel);
        WizardStandardAssetsUi.DrawSetupSection(w,
            "Creates LSTM rig components, ensures a narrative calendar exists, and wires cross-references.");
        if (GUILayout.Button("Export for LSTM training...", GUILayout.Height(22)))
        {
            EditorApplication.ExecuteMenuItem("Locomotion/Narrative/Export for LSTM training...");
        }

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Quick select", EditorStyles.miniBoldLabel);
        if (w.promptInterpreter != null && GUILayout.Button("Select Prompt Interpreter", GUILayout.Height(20)))
            Selection.activeGameObject = w.promptInterpreter.gameObject;
        if (w.summarizer != null && GUILayout.Button("Select Summarizer", GUILayout.Height(20)))
            Selection.activeGameObject = w.summarizer.gameObject;

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
