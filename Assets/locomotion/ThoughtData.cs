using UnityEngine;

/// <summary>
/// Brain-to-brain communication data structure.
/// </summary>
[System.Serializable]
public class ThoughtData
{
    [Tooltip("Sender brain")]
    public Brain sender;

    [Tooltip("Receiver brain")]
    public Brain receiver;

    [Tooltip("Type of thought message")]
    public ThoughtType messageType;

    [Tooltip("Thought data payload")]
    public object data;

    [Tooltip("Timestamp when thought was created")]
    public float timestamp;

    public ThoughtData(Brain sender, Brain receiver, ThoughtType type, object data = null)
    {
        this.sender = sender;
        this.receiver = receiver;
        this.messageType = type;
        this.data = data;
        this.timestamp = Time.time;
    }
}

/// <summary>
/// Types of thought messages between brains.
/// </summary>
public enum ThoughtType
{
    Decision,
    Query,
    Response,
    Alert,
    BehaviorTree,
    RequestPrune
}
