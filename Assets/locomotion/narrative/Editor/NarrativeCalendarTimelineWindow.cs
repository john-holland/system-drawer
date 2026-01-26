using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Locomotion.Narrative;

/// <summary>
/// Custom EditorWindow for narrative calendar day visualization with timeline scrubber and state machine graph.
/// </summary>
public class NarrativeCalendarTimelineWindow : EditorWindow
{
    private NarrativeCalendarAsset targetCalendar;
    private NarrativeDateTime startDate = new NarrativeDateTime(2025, 1, 1, 0, 0, 0);
    private NarrativeDateTime endDate = new NarrativeDateTime(2025, 1, 2, 0, 0, 0);
    private NarrativeDateTime currentTime = new NarrativeDateTime(2025, 1, 1, 12, 0, 0);
    private bool isPlaying = false;
    private Vector2 scrollPosition;
    private List<NarrativeCalendarEvent> eventsInRange = new List<NarrativeCalendarEvent>();

    [MenuItem("Window/Locomotion/Narrative Calendar Timeline")]
    public static void OpenWindow()
    {
        NarrativeCalendarTimelineWindow window = GetWindow<NarrativeCalendarTimelineWindow>();
        window.titleContent = new GUIContent("Narrative Calendar Timeline");
        window.Show();
    }

    private void OnGUI()
    {
        if (targetCalendar == null)
        {
            EditorGUILayout.HelpBox("No NarrativeCalendarAsset selected. Select one in the scene or assign it below.", MessageType.Info);
            targetCalendar = (NarrativeCalendarAsset)EditorGUILayout.ObjectField("Narrative Calendar", targetCalendar, typeof(NarrativeCalendarAsset), true);
            return;
        }

        EditorGUILayout.LabelField("Narrative Calendar Timeline", EditorStyles.boldLabel);

        // Date range selection
        DrawDateRangeSelection();

        EditorGUILayout.Space();

        // Timeline scrubber
        DrawTimelineScrubber();

        EditorGUILayout.Space();

        // Events list
        DrawEventsList();
    }

    private void DrawDateRangeSelection()
    {
        EditorGUILayout.LabelField("Date Range", EditorStyles.boldLabel);

        // Start date
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Start Date:", GUILayout.Width(100));
        startDate.year = EditorGUILayout.IntField(startDate.year, GUILayout.Width(60));
        EditorGUILayout.LabelField("/", GUILayout.Width(10));
        startDate.month = EditorGUILayout.IntField(Mathf.Clamp(startDate.month, 1, 12), GUILayout.Width(40));
        EditorGUILayout.LabelField("/", GUILayout.Width(10));
        startDate.day = EditorGUILayout.IntField(Mathf.Clamp(startDate.day, 1, 31), GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();

        // End date
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("End Date:", GUILayout.Width(100));
        endDate.year = EditorGUILayout.IntField(endDate.year, GUILayout.Width(60));
        EditorGUILayout.LabelField("/", GUILayout.Width(10));
        endDate.month = EditorGUILayout.IntField(Mathf.Clamp(endDate.month, 1, 12), GUILayout.Width(40));
        EditorGUILayout.LabelField("/", GUILayout.Width(10));
        endDate.day = EditorGUILayout.IntField(Mathf.Clamp(endDate.day, 1, 31), GUILayout.Width(40));
        EditorGUILayout.EndHorizontal();

        // Validation
        if (endDate < startDate)
        {
            EditorGUILayout.HelpBox("End date must be after start date.", MessageType.Warning);
        }

        // Quick presets
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Today"))
        {
            var today = System.DateTime.Now;
            startDate = new NarrativeDateTime(today.Year, today.Month, today.Day, 0, 0, 0);
            endDate = new NarrativeDateTime(today.Year, today.Month, today.Day, 23, 59, 59);
        }
        if (GUILayout.Button("This Week"))
        {
            var today = System.DateTime.Now;
            startDate = new NarrativeDateTime(today.Year, today.Month, today.Day, 0, 0, 0);
            endDate = startDate.AddSeconds(7 * 24 * 60 * 60);
        }
        if (GUILayout.Button("This Month"))
        {
            var today = System.DateTime.Now;
            startDate = new NarrativeDateTime(today.Year, today.Month, 1, 0, 0, 0);
            endDate = new NarrativeDateTime(today.Year, today.Month, System.DateTime.DaysInMonth(today.Year, today.Month), 23, 59, 59);
        }
        EditorGUILayout.EndHorizontal();

        // Load events in range
        if (GUILayout.Button("Load Events in Range"))
        {
            LoadCalendarEvents(targetCalendar, startDate, endDate);
        }
    }

    private void DrawTimelineScrubber()
    {
        EditorGUILayout.LabelField("Timeline", EditorStyles.boldLabel);

        // Calculate time range in seconds
        double startSeconds = startDate.ToDateTimeUtc().Subtract(System.DateTime.UnixEpoch).TotalSeconds;
        double endSeconds = endDate.ToDateTimeUtc().Subtract(System.DateTime.UnixEpoch).TotalSeconds;
        double currentSeconds = currentTime.ToDateTimeUtc().Subtract(System.DateTime.UnixEpoch).TotalSeconds;
        double rangeSeconds = endSeconds - startSeconds;

        if (rangeSeconds > 0)
        {
            // Normalize current time to 0-1 range
            float normalizedTime = (float)((currentSeconds - startSeconds) / rangeSeconds);
            normalizedTime = EditorGUILayout.Slider("Narrative Time", normalizedTime, 0f, 1f);

            // Convert back to NarrativeDateTime
            double newSeconds = startSeconds + (normalizedTime * rangeSeconds);
            currentTime = NarrativeDateTime.FromDateTimeUtc(System.DateTime.UnixEpoch.AddSeconds(newSeconds));

            EditorGUILayout.LabelField($"Current Time: {currentTime}");
        }

        // Playback controls
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(isPlaying ? "Pause" : "Play"))
        {
            isPlaying = !isPlaying;
        }
        if (GUILayout.Button("Stop"))
        {
            isPlaying = false;
            currentTime = startDate;
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawEventsList()
    {
        EditorGUILayout.LabelField("Events in Range", EditorStyles.boldLabel);

        if (eventsInRange != null && eventsInRange.Count > 0)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var evt in eventsInRange)
            {
                if (evt == null)
                    continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Event: {evt.title}", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Start: {evt.startDateTime}");
                EditorGUILayout.LabelField($"Duration: {evt.durationSeconds}s");

                if (evt.tree != null)
                {
                    EditorGUILayout.ObjectField("Behavior Tree", evt.tree, typeof(NarrativeTreeAsset), false);
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.HelpBox("No events in selected date range.", MessageType.Info);
        }
    }

    private void LoadCalendarEvents(NarrativeCalendarAsset calendar, NarrativeDateTime start, NarrativeDateTime end)
    {
        eventsInRange.Clear();

        if (calendar == null || calendar.events == null)
            return;

        foreach (var evt in calendar.events)
        {
            if (evt == null)
                continue;

            // Check if event is within date range
            if (evt.startDateTime >= start && evt.startDateTime <= end)
            {
                eventsInRange.Add(evt);
            }
        }
    }
}
