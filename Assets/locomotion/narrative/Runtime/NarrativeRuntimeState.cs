using System;
using System.Collections.Generic;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Minimal runtime persistence state for narrative scheduling/execution.
    /// This is designed to be JSON/YAML-friendly (no UnityEngine.Object references).
    /// </summary>
    [Serializable]
    public class NarrativeRuntimeState
    {
        public int schemaVersion = 1;

        // Scheduling
        public List<string> triggeredEventIds = new List<string>();

        // Execution
        public string activeEventId;
        public bool isExecuting;

        // Best-effort cursor (MVP): sequence of node ids and child indices.
        public List<string> nodeStack = new List<string>();
        public List<int> childIndexStack = new List<int>();
    }
}

