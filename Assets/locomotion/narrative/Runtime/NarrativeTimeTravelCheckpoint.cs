using System;
using System.Collections.Generic;

namespace Locomotion.Narrative
{
    [Serializable]
    public sealed class NarrativeTimeTravelCheckpoint
    {
        public float narrativeTime;
        public string weatherFrameJson;
        public List<string> triggeredEventIds = new List<string>();
        public string activeEventId;
        public List<string> nodeStack = new List<string>();
        public List<int> childIndexStack = new List<int>();
        public List<NarrativeExecutionLedgerEntry> executionLedger = new List<NarrativeExecutionLedgerEntry>();
        public long rewindSeq;
        public string authorityClientId;
    }
}
