using Locomotion.Narrative;
using UnityEditor;
using UnityEngine;

internal static class CalendarStandardAssets
{
    internal static WizardSetupReport Setup(CalendarServiceWizard wizard)
    {
        var report = new WizardSetupReport();
        if (wizard == null)
            return report;

        NarrativeCalendarAsset calendar = null;
        if (wizard.calendarAsset is NarrativeCalendarAsset existing)
            calendar = existing;
        if (calendar == null)
            calendar = WizardStandardAssetsCore.FindFirstInScene<NarrativeCalendarAsset>();

        if (calendar == null)
        {
            var hub = WizardStandardAssetsCore.ResolveHubRoot(wizard);
            var sceneRoot = WizardStandardAssetsCore.EnsureStandardSceneRoot(hub, report);
            var parent = sceneRoot != null ? sceneRoot : hub;
            var go = WizardStandardAssetsCore.FindOrCreateChild(parent, "NarrativeCalendar", report);
            calendar = WizardStandardAssetsCore.FindOrAddComponent<NarrativeCalendarAsset>(go, report);
            if (calendar.events == null || calendar.events.Count == 0)
            {
                calendar.events.Add(new NarrativeCalendarEvent
                {
                    title = "Morning briefing",
                    startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0),
                    durationSeconds = 1800,
                    notes = "Starter event created by Setup Standard Assets."
                });
                calendar.events.Add(new NarrativeCalendarEvent
                {
                    title = "Afternoon quest hook",
                    startDateTime = new NarrativeDateTime(2025, 1, 1, 14, 0, 0),
                    durationSeconds = 3600,
                    notes = "Starter event created by Setup Standard Assets."
                });
                EditorUtility.SetDirty(calendar);
            }
        }
        else
        {
            report.Skipped.Add("NarrativeCalendarAsset in scene");
        }

        if (wizard.calendarAsset != calendar)
        {
            Undo.RecordObject(wizard, "Assign calendar");
            wizard.calendarAsset = calendar;
            EditorUtility.SetDirty(wizard);
            report.Linked.Add("CalendarServiceWizard.calendarAsset");
        }

        return report;
    }
}
