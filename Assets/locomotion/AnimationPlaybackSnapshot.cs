using System;
using UnityEngine;

/// <summary>
/// Read-only view of one animation layer's playback state for UI and debugging.
/// </summary>
[Serializable]
public struct AnimationPlaybackSnapshot
{
    public string treeName;
    public string activeNodeName;
    public float weight;
    public int layerIndex;
    public float normalizedTime;
    public int registeredInstanceId;

    public static AnimationPlaybackSnapshot Empty =>
        new AnimationPlaybackSnapshot
        {
            treeName = "",
            activeNodeName = "",
            weight = 0f,
            layerIndex = -1,
            normalizedTime = 0f,
            registeredInstanceId = -1
        };
}
