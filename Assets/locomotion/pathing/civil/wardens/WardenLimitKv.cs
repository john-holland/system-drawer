using System;
using UnityEngine;

/// <summary>Key/value row for ConversationBus / LawTravelAgent <c>new+!</c> limit tables.</summary>
[Serializable]
public sealed class WardenLimitKv
{
    public string key;
    [Range(0f, 1f)] public float value01 = 0.5f;
}
