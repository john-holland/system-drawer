using UnityEditor;
using UnityEngine;

public static class WizardStandardAssetsUi
{
    public static void DrawSetupSection(Component wizard, string helpText)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Standard Assets", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox(helpText, MessageType.Info);
        if (GUILayout.Button("Setup Standard Assets", GUILayout.Height(28)))
        {
            var report = WizardStandardAssetsFacade.SetupForWizard(wizard);
            EditorUtility.DisplayDialog("Setup Standard Assets", report.Summary, "OK");
        }
    }
}
