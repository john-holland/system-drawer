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

        EditorUtility.SetDirty(wizard);
        EditorUtility.SetDirty(ui);
        return report;
    }
}
