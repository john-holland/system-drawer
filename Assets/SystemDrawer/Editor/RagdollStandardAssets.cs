using UnityEditor;
using UnityEngine;

internal static class RagdollStandardAssets
{
    internal static WizardSetupReport Setup(RagdollServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        Transform root = wizard.ragdollRoot;
        if (root == null)
        {
            var hub = WizardStandardAssetsCore.ResolveHubRoot(wizard);
            var sceneRoot = WizardStandardAssetsCore.EnsureStandardSceneRoot(hub, report);
            var parent = sceneRoot != null ? sceneRoot : hub;
            var go = WizardStandardAssetsCore.FindOrCreateChild(parent, "RagdollRoot", report);
            root = go.transform;
        }
        else
        {
            report.Skipped.Add("RagdollRoot");
        }

        if (wizard.ragdollRoot != root)
        {
            Undo.RecordObject(wizard, "Assign ragdoll root");
            wizard.ragdollRoot = root;
            EditorUtility.SetDirty(wizard);
            report.Linked.Add("RagdollServiceWizard.ragdollRoot");
        }

        report.Warnings.Add("Placeholder only — open Ragdoll Fitting Wizard to fit a character mesh.");
        WizardStandardAssetsCore.MarkSceneDirty(wizard);
        return report;
    }
}
