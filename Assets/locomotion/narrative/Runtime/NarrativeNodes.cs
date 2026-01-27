using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    public enum NarrativeNodeType
    {
        Sequence,
        Selector,
        Action
    }

    /// <summary>
    /// Guest book entry for tracking narrative event attendance.
    /// </summary>
    [Serializable]
    public class NarrativeGuestBookEntry
    {
        [Tooltip("When the event was attended")]
        public NarrativeDateTime dateTime;

        [Tooltip("Fun info/notes about the visit")]
        public string notes = "";

        [Tooltip("Additional metadata (e.g., ChatGPT/Stable Diffusion parameters)")]
        public Dictionary<string, string> metadata = new Dictionary<string, string>();

        public NarrativeGuestBookEntry()
        {
            dateTime = new NarrativeDateTime();
        }

        public NarrativeGuestBookEntry(NarrativeDateTime dt, string notesText = "")
        {
            dateTime = dt;
            notes = notesText;
            metadata = new Dictionary<string, string>();
        }
    }

    [Serializable]
    public abstract class NarrativeNode
    {
        public string id = Guid.NewGuid().ToString("N");
        public string title = "Node";
        public NarrativeContingency contingency = new NarrativeContingency();
        public abstract NarrativeNodeType NodeType { get; }

        [Header("Attendance Tracking")]
        [Tooltip("Marks if this event has been attended")]
        public bool attended = false;

        [Tooltip("Prevents automatic reentry after execution")]
        public bool noReentry = false;

        [Header("Guest Book")]
        [Tooltip("Stack of narrative dates with fun info")]
        public List<NarrativeGuestBookEntry> guestBook = new List<NarrativeGuestBookEntry>();

        /// <summary>
        /// Manually allows reentry (sets attended = false).
        /// </summary>
        public void Reenter()
        {
            attended = false;
        }

        /// <summary>
        /// Add an entry to the guest book.
        /// </summary>
        public void AddGuestBookEntry(NarrativeDateTime dateTime, string notes = "", Dictionary<string, string> metadata = null)
        {
            NarrativeGuestBookEntry entry = new NarrativeGuestBookEntry(dateTime, notes);
            if (metadata != null)
            {
                entry.metadata = new Dictionary<string, string>(metadata);
            }
            guestBook.Add(entry);
        }

        /// <summary>
        /// Get guest book entries within a date range.
        /// </summary>
        public List<NarrativeGuestBookEntry> GetGuestBookEntries(NarrativeDateTime startDate, NarrativeDateTime endDate)
        {
            List<NarrativeGuestBookEntry> entries = new List<NarrativeGuestBookEntry>();
            foreach (var entry in guestBook)
            {
                if (entry.dateTime.CompareTo(startDate) >= 0 && entry.dateTime.CompareTo(endDate) <= 0)
                {
                    entries.Add(entry);
                }
            }
            return entries;
        }
    }

    [Serializable]
    public class NarrativeSequenceNode : NarrativeNode
    {
        public override NarrativeNodeType NodeType => NarrativeNodeType.Sequence;

        [SerializeReference]
        public List<NarrativeNode> children = new List<NarrativeNode>();
    }

    [Serializable]
    public class NarrativeSelectorNode : NarrativeNode
    {
        public override NarrativeNodeType NodeType => NarrativeNodeType.Selector;

        [SerializeReference]
        public List<NarrativeNode> children = new List<NarrativeNode>();
    }

    [Serializable]
    public class NarrativeActionNode : NarrativeNode
    {
        public override NarrativeNodeType NodeType => NarrativeNodeType.Action;

        [SerializeReference]
        public NarrativeActionSpec action;
    }
}

