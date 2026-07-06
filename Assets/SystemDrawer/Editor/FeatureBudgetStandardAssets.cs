using UnityEditor;
using UnityEngine;

internal static class FeatureBudgetStandardAssets
{
    internal static WizardSetupReport Setup(FeatureBudgetRuntime runtime)
    {
        var report = new WizardSetupReport();
        if (runtime == null)
            return report;

        var profile = runtime.profile;
        if (profile == null)
            profile = WizardStandardAssetsCore.LoadAssetAtPath<FeatureBudgetProfile>(
                WizardStandardAssetsPaths.FeatureBudget.DefaultProfile);

        if (profile == null)
        {
            profile = ScriptableObject.CreateInstance<FeatureBudgetProfile>();
            profile.EnsureDefaults();
            WizardStandardAssetsCore.EnsureFolder(WizardStandardAssetsPaths.Root + "/FeatureBudget");
            AssetDatabase.CreateAsset(profile, WizardStandardAssetsPaths.FeatureBudget.DefaultProfile);
            AssetDatabase.SaveAssets();
            report.Created.Add("DefaultFeatureBudgetProfile");
        }
        else
        {
            report.Skipped.Add("FeatureBudgetProfile");
        }

        profile.EnsureDefaults();
        EditorUtility.SetDirty(profile);

        if (runtime.profile != profile)
        {
            Undo.RecordObject(runtime, "Assign feature budget profile");
            runtime.profile = profile;
            EditorUtility.SetDirty(runtime);
            report.Linked.Add("FeatureBudgetRuntime.profile");
        }

        FeatureBudgetEditorUtility.SyncProfileFromScenePlanet(profile);
        report.Linked.Add("Feature budget ratio sync from scene planet (if present)");

        return report;
    }

    internal static WizardSetupReport SetupFromHub(GameObject hubRoot)
    {
        if (hubRoot == null)
            return new WizardSetupReport();
        var runtime = hubRoot.GetComponent<FeatureBudgetRuntime>();
        if (runtime == null)
            runtime = Undo.AddComponent<FeatureBudgetRuntime>(hubRoot);
        return Setup(runtime);
    }
}
