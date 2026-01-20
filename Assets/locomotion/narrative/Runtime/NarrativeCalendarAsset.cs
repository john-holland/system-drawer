using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    [Serializable]
    public class NarrativeCalendarEvent
    {
        public string id = Guid.NewGuid().ToString("N");
        public string title = "New Event";
        [TextArea] public string notes;

        public NarrativeDateTime startDateTime = new NarrativeDateTime(2025, 1, 1, 9, 0, 0);

        [Tooltip("Duration in narrative seconds (0 = instantaneous).")]
        public int durationSeconds = 0;

        public List<string> tags = new List<string>();

        [Tooltip("Optional narrative tree to execute for this event.")]
        public NarrativeTreeAsset tree;

        [Tooltip("Optional direct queue of actions (executed after tree if both are set).")]
        [SerializeReference]
        public List<NarrativeActionSpec> actions = new List<NarrativeActionSpec>();
    }

    [CreateAssetMenu(menuName = "Locomotion/Narrative/Narrative Calendar", fileName = "NarrativeCalendar")]
    public class NarrativeCalendarAsset : ScriptableObject
    {
        [Header("Schema")]
        public int schemaVersion = 1;

        [Header("Events")]
        public List<NarrativeCalendarEvent> events = new List<NarrativeCalendarEvent>();
    }
}

