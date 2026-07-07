using Locomotion.Narrative;
using SystemDrawer.DreamCycle;
using SystemDrawer.Quest;
using UnityEditor;
using UnityEngine;

internal static class QuestStandardAssets
{
    internal static WizardSetupReport Setup(QuestServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        var hub = WizardStandardAssetsCore.ResolveHubRoot(wizard);
        var sceneRoot = WizardStandardAssetsCore.EnsureStandardSceneRoot(hub, report);
        var parent = sceneRoot != null ? sceneRoot : hub;

        QuestRunner runner = wizard.runner;
        if (runner == null)
            runner = WizardStandardAssetsCore.FindFirstInScene<QuestRunner>();
        if (runner == null)
        {
            var go = WizardStandardAssetsCore.FindOrCreateChild(parent, "QuestRunner", report);
            runner = WizardStandardAssetsCore.FindOrAddComponent<QuestRunner>(go, report);
        }
        else
            report.Skipped.Add("QuestRunner");

        QuestMapRenderer map = wizard.mapRenderer;
        if (map == null)
            map = WizardStandardAssetsCore.FindFirstInScene<QuestMapRenderer>();
        if (map == null)
        {
            var go = WizardStandardAssetsCore.FindOrCreateChild(parent, "QuestMapRenderer", report);
            map = WizardStandardAssetsCore.FindOrAddComponent<QuestMapRenderer>(go, report);
        }
        else
            report.Skipped.Add("QuestMapRenderer");

        if (map.questRunner != runner)
        {
            Undo.RecordObject(map, "Link quest runner");
            map.questRunner = runner;
            EditorUtility.SetDirty(map);
            report.Linked.Add("QuestMapRenderer.questRunner");
        }

        var bundle = WizardStandardAssetsCore.FindOrCreateAsset(
            WizardStandardAssetsPaths.Quest.BehaviorTreeBundle,
            () => ScriptableObject.CreateInstance<QuestBehaviorTreeBundle>(),
            report,
            "QuestBehaviorTreeBundle");

        if (wizard.runner != runner)
        {
            Undo.RecordObject(wizard, "Assign quest runner");
            wizard.runner = runner;
            report.Linked.Add("QuestServiceWizard.runner");
        }

        if (wizard.mapRenderer != map)
        {
            Undo.RecordObject(wizard, "Assign quest map renderer");
            wizard.mapRenderer = map;
            report.Linked.Add("QuestServiceWizard.mapRenderer");
        }

        EditorUtility.SetDirty(wizard);
        WizardStandardAssetsCore.MarkSceneDirty(wizard);
        return report;
    }
}

internal static class DreamCycleStandardAssets
{
    internal static WizardSetupReport Setup(DreamCycleServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        var hub = WizardStandardAssetsCore.ResolveHubRoot(wizard);
        var sceneRoot = WizardStandardAssetsCore.EnsureStandardSceneRoot(hub, report);
        var parent = sceneRoot != null ? sceneRoot : hub;
        var host = WizardStandardAssetsCore.FindOrCreateChild(parent, "DreamCycle", report);

        var registry = WizardStandardAssetsCore.FindOrCreateAsset(
            WizardStandardAssetsPaths.DreamCycle.NeedAspectRegistry,
            () =>
            {
                var r = ScriptableObject.CreateInstance<NeedAspectRegistry>();
                r.aspects = NeedAspectRegistry.DefaultAspects();
                return r;
            },
            report,
            "NeedAspectRegistry");

        var simProfile = WizardStandardAssetsCore.FindOrCreateAsset(
            WizardStandardAssetsPaths.DreamCycle.SimulationProfile,
            () => ScriptableObject.CreateInstance<DreamDaySimulationProfile>(),
            report,
            "DreamDaySimulationProfile");

        var day = wizard.dayRunner ?? host.GetComponent<DreamDayCycleRunner>();
        if (day == null)
            day = WizardStandardAssetsCore.FindFirstInScene<DreamDayCycleRunner>();
        if (day == null)
            day = WizardStandardAssetsCore.FindOrAddComponent<DreamDayCycleRunner>(host, report);
        else
            report.Skipped.Add("DreamDayCycleRunner");

        var night = wizard.nightRunner ?? host.GetComponent<DreamNightCycleRunner>();
        if (night == null)
            night = WizardStandardAssetsCore.FindFirstInScene<DreamNightCycleRunner>();
        if (night == null)
            night = WizardStandardAssetsCore.FindOrAddComponent<DreamNightCycleRunner>(host, report);
        else
            report.Skipped.Add("DreamNightCycleRunner");

        var sleep = wizard.sleepRenderer ?? host.GetComponent<SleepWaveStatRenderer>();
        if (sleep == null)
            sleep = WizardStandardAssetsCore.FindFirstInScene<SleepWaveStatRenderer>();
        if (sleep == null)
            sleep = WizardStandardAssetsCore.FindOrAddComponent<SleepWaveStatRenderer>(host, report);
        else
            report.Skipped.Add("SleepWaveStatRenderer");

        if (day.registry != registry)
        {
            Undo.RecordObject(day, "Assign need registry");
            day.registry = registry;
            EditorUtility.SetDirty(day);
            report.Linked.Add("DreamDayCycleRunner.registry");
        }

        if (day.profile != simProfile)
        {
            Undo.RecordObject(day, "Assign dream simulation profile");
            day.profile = simProfile;
            EditorUtility.SetDirty(day);
            report.Linked.Add("DreamDayCycleRunner.profile");
        }

        if (night.dayRunner != day)
        {
            Undo.RecordObject(night, "Assign day runner");
            night.dayRunner = day;
            EditorUtility.SetDirty(night);
            report.Linked.Add("DreamNightCycleRunner.dayRunner");
        }

        if (night.sleepRenderer != sleep)
        {
            Undo.RecordObject(night, "Assign sleep renderer");
            night.sleepRenderer = sleep;
            EditorUtility.SetDirty(night);
            report.Linked.Add("DreamNightCycleRunner.sleepRenderer");
        }

        Undo.RecordObject(wizard, "Assign dream cycle refs");
        wizard.dayRunner = day;
        wizard.nightRunner = night;
        wizard.sleepRenderer = sleep;
        EditorUtility.SetDirty(wizard);
        report.Linked.Add("DreamCycleServiceWizard refs");

        WizardStandardAssetsCore.MarkSceneDirty(wizard);
        return report;
    }
}
