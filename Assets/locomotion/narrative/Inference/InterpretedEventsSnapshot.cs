using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Persisted baseline for prompt interpreter diff. Survives domain reload.
    /// </summary>
    public class InterpretedEventsSnapshot : ScriptableObject
    {
        [Tooltip("Stored events (baseline for diff).")]
        public List<InterpretedEvent> events = new List<InterpretedEvent>();

        [Tooltip("Prompt used when this snapshot was captured (optional).")]
        public string lastPrompt = "";

        [Tooltip("Model path or identifier when captured (optional, for change detection).")]
        public string modelPath = "";

        /// <summary>Capture current events into this snapshot.</summary>
        public void Capture(IReadOnlyList<InterpretedEvent> source, string prompt = null, string model = null)
        {
            events.Clear();
            if (source != null)
            {
                for (int i = 0; i < source.Count; i++)
                    events.Add(source[i]);
            }
            if (prompt != null) lastPrompt = prompt;
            if (model != null) modelPath = model;
        }
    }
}
