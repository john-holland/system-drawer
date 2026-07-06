using UnityEditor;
using UnityEngine;

internal static class UscStandardAssets
{
    internal static WizardSetupReport Setup(UscBuildServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        var manifest = UscBuildManifest.CreateDefault();
        manifest.tenantId = wizard.tenantId;
        manifest.languageVersion = wizard.languageVersion;
        manifest.sourceDbPath = wizard.sourceDbPath;
        manifest.mode = wizard.buildMode;
        manifest.notes = "Default USC build manifest (Setup Standard Assets).";

        var json = UscBuildManifest.ToJson(manifest, true);
        var textAsset = WizardStandardAssetsCore.FindOrCreateTextAsset(
            WizardStandardAssetsPaths.Usc.DefaultManifest,
            json,
            report,
            "USC build manifest");

        if (wizard.manifestJson != textAsset)
        {
            Undo.RecordObject(wizard, "Assign USC manifest");
            wizard.manifestJson = textAsset;
            wizard.ClearManifestCache();
            EditorUtility.SetDirty(wizard);
            report.Linked.Add("UscBuildServiceWizard.manifestJson");
        }

        return report;
    }
}
