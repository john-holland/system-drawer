#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UscBuildServiceWizard))]
public class UscBuildServiceWizardEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var wizard = (UscBuildServiceWizard)target;

        EditorGUILayout.Space();
        if (GUILayout.Button("Assign from System Drawer", GUILayout.Height(22)))
        {
            var service = SystemDrawerService.FindInScene();
            if (service != null)
            {
                Undo.RecordObject(wizard, "Assign USC build wizard from drawer");
                if (wizard.TryCompleteFromService())
                    EditorUtility.SetDirty(wizard);
            }
        }

        if (GUILayout.Button("Open Continuum Build Manager", GUILayout.Height(22)))
            ContinuumBuildManagerWindow.ShowWindow();

        if (GUILayout.Button("Preview packed-publish CLI stub", GUILayout.Height(22)))
        {
            var cmd = wizard.BuildPackedPublishStubCommand();
            EditorUtility.DisplayDialog("Packed publish stub command", cmd, "OK");
        }

        if (GUILayout.Button("Resolve sample asset (debug)", GUILayout.Height(22)))
        {
            var result = wizard.ResolveAsset("sample-asset");
            EditorUtility.DisplayDialog(
                "USC resolve debug",
                $"success: {result.success}\nsource: {result.source}\nresolvedPath: {result.resolvedPath}",
                "OK");
        }
    }
}
#endif
