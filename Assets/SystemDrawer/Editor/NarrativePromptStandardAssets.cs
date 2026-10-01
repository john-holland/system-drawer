using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;

internal static class NarrativePromptStandardAssets
{
    internal static WizardSetupReport Setup(NarrativePromptServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        var calWizard = wizard.GetComponentInParent<CalendarServiceWizard>();
        if (calWizard == null)
            calWizard = Object.FindAnyObjectByType<CalendarServiceWizard>();
        if (calWizard != null)
            report.Merge(CalendarStandardAssets.Setup(calWizard));

        Undo.RecordObject(wizard.gameObject, "Create LSTM rig");
        var interp = wizard.GetComponent<NarrativeLSTMPromptInterpreter>();
        if (interp == null)
        {
            interp = Undo.AddComponent<NarrativeLSTMPromptInterpreter>(wizard.gameObject);
            report.Created.Add("NarrativeLSTMPromptInterpreter");
        }
        else
            report.Skipped.Add("NarrativeLSTMPromptInterpreter");

        var sum = wizard.GetComponent<NarrativeLSTMSummarizer>();
        if (sum == null)
        {
            sum = Undo.AddComponent<NarrativeLSTMSummarizer>(wizard.gameObject);
            report.Created.Add("NarrativeLSTMSummarizer");
        }
        else
            report.Skipped.Add("NarrativeLSTMSummarizer");

        var ui = wizard.GetComponent<NarrativeLSTMUI>();
        if (ui == null)
        {
            ui = Undo.AddComponent<NarrativeLSTMUI>(wizard.gameObject);
            report.Created.Add("NarrativeLSTMUI");
        }
        else
            report.Skipped.Add("NarrativeLSTMUI");

        ui.summarizer = sum;
        ui.promptInterpreter = interp;

        NarrativeCalendarAsset calendar = null;
        if (wizard.calendarAsset is NarrativeCalendarAsset cal)
            calendar = cal;
        if (calendar == null)
            calendar = Object.FindAnyObjectByType<NarrativeCalendarAsset>();

        if (wizard.promptInterpreter != interp)
        {
            Undo.RecordObject(wizard, "Assign prompt interpreter");
            wizard.promptInterpreter = interp;
            report.Linked.Add("NarrativePromptServiceWizard.promptInterpreter");
        }

        if (wizard.summarizer != sum)
        {
            Undo.RecordObject(wizard, "Assign summarizer");
            wizard.summarizer = sum;
            report.Linked.Add("NarrativePromptServiceWizard.summarizer");
        }

        if (calendar != null)
        {
            if (wizard.calendarAsset != calendar)
            {
                Undo.RecordObject(wizard, "Assign calendar");
                wizard.calendarAsset = calendar;
                report.Linked.Add("NarrativePromptServiceWizard.calendarAsset");
            }

            sum.calendar = calendar;
            interp.calendar = calendar;
            EditorUtility.SetDirty(sum);
            EditorUtility.SetDirty(interp);
        }

        // Health inpaint event catalog + runner (collated with LSTM Standard Assets)
        var catalog = AssetDatabase.LoadAssetAtPath<HealthInpaintEventCatalog>(
            WizardStandardAssetsPaths.Stat.DefaultHealthInpaintEventCatalog);
        if (catalog == null)
        {
            WizardStandardAssetsCore.EnsureFolder(WizardStandardAssetsPaths.Stat.Folder);
            catalog = ScriptableObject.CreateInstance<HealthInpaintEventCatalog>();
            var runtime = HealthInpaintEventCatalog.CreateDefaultRuntime();
            catalog.events = runtime.events;
            AssetDatabase.CreateAsset(catalog, WizardStandardAssetsPaths.Stat.DefaultHealthInpaintEventCatalog);
            report.Created.Add("DefaultHealthInpaintEventCatalog");
        }
        else
            report.Skipped.Add("DefaultHealthInpaintEventCatalog");

        var sceneRoot = GameObject.Find("_StandardScene");
        if (sceneRoot == null)
        {
            sceneRoot = new GameObject("_StandardScene");
            Undo.RegisterCreatedObjectUndo(sceneRoot, "Create _StandardScene");
            report.Created.Add("_StandardScene");
        }
        var runnerGo = sceneRoot.transform.Find("HealthInpaintEventRunner");
        HealthInpaintEventRunner runner;
        if (runnerGo == null)
        {
            var go = new GameObject("HealthInpaintEventRunner");
            Undo.RegisterCreatedObjectUndo(go, "Create HealthInpaintEventRunner");
            go.transform.SetParent(sceneRoot.transform, false);
            runner = Undo.AddComponent<HealthInpaintEventRunner>(go);
            report.Created.Add("HealthInpaintEventRunner");
        }
        else
        {
            runner = runnerGo.GetComponent<HealthInpaintEventRunner>();
            if (runner == null)
                runner = Undo.AddComponent<HealthInpaintEventRunner>(runnerGo.gameObject);
            report.Skipped.Add("HealthInpaintEventRunner");
        }
        if (runner.catalog != catalog)
        {
            Undo.RecordObject(runner, "Assign health inpaint catalog");
            runner.catalog = catalog;
            report.Linked.Add("HealthInpaintEventRunner.catalog");
        }
        if (runner.interpreter != interp)
        {
            Undo.RecordObject(runner, "Assign LSTM interpreter");
            runner.interpreter = interp;
            report.Linked.Add("HealthInpaintEventRunner.interpreter");
        }
        EditorUtility.SetDirty(runner);

        EditorUtility.SetDirty(wizard);
        EditorUtility.SetDirty(ui);
        return report;
    }
}
