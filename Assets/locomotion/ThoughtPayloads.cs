using System;
using UnityEngine;

/// <summary>
/// Channel flags for <see cref="QueryThoughtPayload"/>.
/// </summary>
[Flags]
public enum QueryChannel
{
    None = 0,
    Goals = 1 << 0,
    Filters = 1 << 1,
    BehaviorTreeSummary = 1 << 2,
    All = Goals | Filters | BehaviorTreeSummary
}

/// <summary>
/// Payload for <see cref="ThoughtType.Decision"/> — propose merging goals / conviction.
/// </summary>
[Serializable]
public class DecisionThoughtPayload
{
    public string proposedGoalName;
    [Range(0f, 1f)] public float conviction = 0.5f;
    public Vector3 optionalTargetPosition;
    [Tooltip("Optional narrative tags for similarity / lie policy.")]
    public string[] semanticTags = Array.Empty<string>();
}

/// <summary>
/// Payload for <see cref="ThoughtType.Query"/>.
/// </summary>
[Serializable]
public class QueryThoughtPayload
{
    public string queryId;
    public QueryChannel channels = QueryChannel.All;
}

/// <summary>
/// Payload for <see cref="ThoughtType.Response"/>.
/// </summary>
[Serializable]
public class ResponseThoughtPayload
{
    public string queryId;
    [TextArea(1, 4)] public string answerText;
    public string[] structuredTags = Array.Empty<string>();
}

/// <summary>
/// Payload for <see cref="ThoughtType.Alert"/>.
/// </summary>
[Serializable]
public class AlertThoughtPayload
{
    [Range(0f, 1f)] public float severity = 0.5f;
    public string message;
}

/// <summary>
/// Payload for <see cref="ThoughtType.BehaviorTree"/> — optional merge hint (MVP: reference only when same rig).
/// </summary>
[Serializable]
public class BehaviorTreeThoughtPayload
{
    [Tooltip("When true, receiver may mirror sender behavior tree root reference if on compatible actor.")]
    public bool suggestMirrorSenderTree;
}

/// <summary>
/// Payload for <see cref="ThoughtType.RequestPrune"/>.
/// </summary>
[Serializable]
public class RequestPruneThoughtPayload
{
    public bool fullTree;
}
