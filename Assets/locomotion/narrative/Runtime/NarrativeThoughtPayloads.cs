using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>Thought kinds mirrored from Locomotion.Runtime ThoughtType ordinals.</summary>
    public enum NarrativeThoughtType
    {
        Decision = 0,
        Query = 1,
        Response = 2,
        Alert = 3,
        BehaviorTree = 4,
        RequestPrune = 5
    }

    [Flags]
    public enum NarrativeQueryChannel
    {
        None = 0,
        Goals = 1 << 0,
        Filters = 1 << 1,
        BehaviorTreeSummary = 1 << 2,
        All = Goals | Filters | BehaviorTreeSummary
    }

    [Serializable]
    public class NarrativeDecisionThoughtPayload
    {
        public string proposedGoalName;
        [Range(0f, 1f)] public float conviction = 0.5f;
        public Vector3 optionalTargetPosition;
        public string[] semanticTags = Array.Empty<string>();
    }

    [Serializable]
    public class NarrativeQueryThoughtPayload
    {
        public string queryId;
        public NarrativeQueryChannel channels = NarrativeQueryChannel.All;
    }

    [Serializable]
    public class NarrativeAlertThoughtPayload
    {
        [Range(0f, 1f)] public float severity = 0.5f;
        public string message;
    }

    [Serializable]
    public class NarrativeBehaviorTreeThoughtPayload
    {
        public bool suggestMirrorSenderTree;
    }

    [Serializable]
    public class NarrativeRequestPruneThoughtPayload
    {
        public bool fullTree;
    }
}
