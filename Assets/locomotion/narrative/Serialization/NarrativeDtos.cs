using System;
using System.Collections.Generic;

namespace Locomotion.Narrative.Serialization
{
    [Serializable]
    public class NarrativeCalendarDto
    {
        public int schemaVersion = 1;
        public List<NarrativeCalendarEventDto> events = new List<NarrativeCalendarEventDto>();
    }

    [Serializable]
    public class NarrativeCalendarEventDto
    {
        public string id;
        public string title;
        public string notes;
        public Locomotion.Narrative.NarrativeDateTime startDateTime;
        public int durationSeconds;
        public List<string> tags = new List<string>();

        public string treeAssetGuid;
        public List<NarrativeActionDto> actions = new List<NarrativeActionDto>();
    }

    [Serializable]
    public class NarrativeTreeDto
    {
        public int schemaVersion = 1;
        public string rootAssetGuid;
        public NarrativeNodeDto root;
    }

    [Serializable]
    public class NarrativeNodeDto
    {
        public string type;
        public string id;
        public string title;
        public Locomotion.Narrative.NarrativeContingency contingency;

        public List<NarrativeNodeDto> children;
        public NarrativeActionDto action;
    }

    [Serializable]
    public class NarrativeActionDto
    {
        public string type;
        public Locomotion.Narrative.NarrativeContingency contingency;

        // Common/union payload
        public string targetKey;
        public string componentTypeName;
        public string memberName;
        public string methodName;
        public Locomotion.Narrative.NarrativeValue value;
        public Locomotion.Narrative.NarrativeValue[] args;

        // Prefab / tree links
        public string prefabGuid;

        // Spawn extras
        public string parentKey;
        public UnityEngine.Vector3 localPosition;
        public UnityEngine.Vector3 localEulerAngles;
        public bool worldSpace;

        // BT goal
        public string actorKey;
        public Locomotion.Narrative.BehaviorTreeGoalSpec goal;
    }
}

