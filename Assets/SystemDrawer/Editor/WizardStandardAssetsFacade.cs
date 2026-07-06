using System;
using System.Reflection;
using SystemDrawer.DreamCycle;
using SystemDrawer.Quest;
using UnityEngine;

public static class WizardStandardAssetsFacade
{
    public static WizardSetupReport SetupForWizard(Component wizard)
    {
        if (wizard == null)
            return new WizardSetupReport();

        switch (wizard)
        {
            case CalendarServiceWizard c:
                return CalendarStandardAssets.Setup(c);
            case NarrativePromptServiceWizard n:
                return NarrativePromptStandardAssets.Setup(n);
            case UscBuildServiceWizard u:
                return UscStandardAssets.Setup(u);
            case PlanetServiceWizardComponent p:
                return PlanetStandardAssets.Setup(p);
            case FeatureBudgetRuntime f:
                return FeatureBudgetStandardAssets.Setup(f);
            case WeatherServiceWizardComponent w:
                return WeatherStandardAssetsBridge.Setup(w);
            case RagdollServiceWizard r:
                return RagdollStandardAssets.Setup(r);
            case QuestServiceWizard q:
                return QuestStandardAssets.Setup(q);
            case DreamCycleServiceWizard d:
                return DreamCycleStandardAssets.Setup(d);
            default:
                return SetupLooseByTypeName(wizard);
        }
    }

    static WizardSetupReport SetupLooseByTypeName(Component wizard)
    {
        var name = wizard.GetType().Name;
        if (name == "Spatial4DServiceWizard")
            return Spatial4DStandardAssetsBridge.SetupLoose(wizard);
        if (name == "NetworkServiceWizard")
            return NetworkingStandardAssetsBridge.Setup(wizard);
        if (name == "MenuRagdollServiceWizard")
        {
            return new WizardSetupReport
            {
                Warnings = { "Menu ragdoll is scene-specific; assign manually." }
            };
        }

        return new WizardSetupReport
        {
            Warnings = { "No standard-assets handler for " + name }
        };
    }

    internal static WizardSetupReport SetupAllForFacilitator(SystemDrawerFacilitator facilitator)
    {
        var merged = new WizardSetupReport();
        if (facilitator == null)
            return merged;

        facilitator.EnsureWizardReferencesFilled();

        Run(merged, facilitator.GetComponentInChildren<CalendarServiceWizard>(true), CalendarStandardAssets.Setup);
        Run(merged, facilitator.GetComponentInChildren<NarrativePromptServiceWizard>(true), NarrativePromptStandardAssets.Setup);
        Run(merged, facilitator.GetComponentInChildren<PlanetServiceWizardComponent>(true), PlanetStandardAssets.Setup);
        Run(merged, facilitator.GetComponentInChildren<WeatherServiceWizardComponent>(true), w => WeatherStandardAssetsBridge.Setup(w));
        RunByTypeName(merged, facilitator, "Spatial4DServiceWizard", Spatial4DStandardAssetsBridge.Setup);
        Run(merged, facilitator.GetComponentInChildren<QuestServiceWizard>(true), QuestStandardAssets.Setup);
        Run(merged, facilitator.GetComponentInChildren<DreamCycleServiceWizard>(true), DreamCycleStandardAssets.Setup);
        Run(merged, facilitator.GetComponentInChildren<UscBuildServiceWizard>(true), UscStandardAssets.Setup);
        RunByTypeName(merged, facilitator, "NetworkServiceWizard", NetworkingStandardAssetsBridge.Setup);
        merged.Merge(FeatureBudgetStandardAssets.SetupFromHub(facilitator.gameObject));
        Run(merged, facilitator.GetComponentInChildren<RagdollServiceWizard>(true), RagdollStandardAssets.Setup);

        return merged;
    }

    static void Run<T>(WizardSetupReport merged, T wizard, Func<T, WizardSetupReport> setup) where T : Component
    {
        if (wizard == null)
            return;
        merged.Merge(setup(wizard));
    }

    static void RunByTypeName(
        WizardSetupReport merged,
        SystemDrawerFacilitator facilitator,
        string typeName,
        Func<Component, WizardSetupReport> setup)
    {
        foreach (var mb in facilitator.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == typeName)
            {
                merged.Merge(setup(mb));
                return;
            }
        }
    }
}
