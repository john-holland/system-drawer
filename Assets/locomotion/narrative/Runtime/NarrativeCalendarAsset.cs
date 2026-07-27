using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    [Serializable]
    public class NarrativeCausalLink
    {
        [Tooltip("Event id that enables the other (A enables B).")]
        public string fromEventId;
        [Tooltip("Event id that is enabled (B).")]
        public string toEventId;
    }

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

        [Header("4D Placement (optional)")]
        [Tooltip("When set, this event is placed in (region, time_window) for 4D spatial generator and spatial+temporal triggers. When null, only startDateTime/durationSeconds apply.")]
        public Bounds4? spatiotemporalVolume;
        [Tooltip("When set and non-empty, these keys are used for spatiotemporal region check instead of the scheduler's positionKeys. Empty or null = use scheduler default.")]
        public List<string> positionKeys;
    }

    [AddComponentMenu("Locomotion/Narrative/Narrative Calendar")]
    public class NarrativeCalendarAsset : MonoBehaviour
    {
        [Header("Schema")]
        public int schemaVersion = 1;

        [Header("Events")]
        public List<NarrativeCalendarEvent> events = new List<NarrativeCalendarEvent>();

        [Header("4D Causal Links")]
        [Tooltip("Causal links: fromEventId enables toEventId. Used for causal overlay in editor.")]
        public List<NarrativeCausalLink> causalLinks = new List<NarrativeCausalLink>();
        [Tooltip("When true, editor gizmos draw causal links between event volumes/centers.")]
        public bool showCausalOverlay = false;
        [Tooltip("Gizmo color for causal link lines.")]
        public Color causalLinkGizmoColor = new Color(1f, 0.8f, 0.2f, 0.8f);

        [Header("BioRhythm Overlay")]
        [Tooltip("When true, Calendar Wizard draws health (blue), love (pink→red), political (purple) biorhythm bars.")]
        public bool showBioRhythmEvents = false;
        public Color bioRhythmHealthColor = new Color(0.3f, 0.6f, 1f, 0.9f);
        public Color bioRhythmLovePink = new Color(0.95f, 0.45f, 0.65f, 0.9f);
        public Color bioRhythmLoveRed = new Color(0.9f, 0.2f, 0.25f, 0.9f);
        public Color bioRhythmPoliticalColor = new Color(0.6f, 0.4f, 0.9f, 0.9f);

        private void OnDrawGizmosSelected()
        {
            if (!showCausalOverlay || causalLinks == null || events == null)
                return;
            for (int i = 0; i < causalLinks.Count; i++)
            {
                var link = causalLinks[i];
                if (string.IsNullOrEmpty(link.fromEventId) || string.IsNullOrEmpty(link.toEventId))
                    continue;
                Vector3 fromCenter = Vector3.zero;
                Vector3 toCenter = Vector3.zero;
                bool foundFrom = false, foundTo = false;
                foreach (var e in events)
                {
                    if (e == null) continue;
                    if (e.id == link.fromEventId)
                    {
                        foundFrom = true;
                        if (e.spatiotemporalVolume.HasValue)
                            fromCenter = e.spatiotemporalVolume.Value.center;
                    }
                    if (e.id == link.toEventId)
                    {
                        foundTo = true;
                        if (e.spatiotemporalVolume.HasValue)
                            toCenter = e.spatiotemporalVolume.Value.center;
                    }
                }
                if (!foundFrom || !foundTo)
                    continue;
                Gizmos.color = causalLinkGizmoColor;
                Gizmos.DrawLine(fromCenter, toCenter);
            }
        }
    }
}

