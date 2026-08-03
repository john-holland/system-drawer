using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Inventory tag for a physical/virtual keycard.</summary>
[Serializable]
public sealed class KeycardItem
{
    public string keycardId;
    public string label;
    public string holderActorId;
}

/// <summary>Door / SG4D node lock gated by KeycardAccessRegistry.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Keycard Lock")]
public sealed class KeycardLock : MonoBehaviour
{
    public string nodeStableId;
    public bool locked = true;
    public bool defaultLocked = true;

    void Awake()
    {
        if (string.IsNullOrEmpty(nodeStableId))
            nodeStableId = gameObject.name;
        locked = defaultLocked;
    }

    public bool TryUnlock(string keycardId, KeycardAccessRegistry registry = null)
    {
        registry ??= KeycardAccessRegistry.Instance;
        if (registry == null || string.IsNullOrEmpty(keycardId)) return false;
        if (!registry.Allows(keycardId, nodeStableId)) return false;
        locked = false;
        return true;
    }

    public void Relock() => locked = defaultLocked;
}

/// <summary>Maps keycard id → allowed node stable ids + actors present at node.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Keycard Access Registry")]
public sealed class KeycardAccessRegistry : MonoBehaviour
{
    public static KeycardAccessRegistry Instance { get; private set; }

    [Serializable]
    public sealed class Entry
    {
        public string keycardId;
        public List<string> allowedNodeIds = new List<string>();
        public List<string> actorIdsAtNode = new List<string>();
        public string boundNodeId;
    }

    public List<Entry> entries = new List<Entry>();

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Entry GetOrCreate(string keycardId)
    {
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].keycardId == keycardId)
                return entries[i];
        var e = new Entry { keycardId = keycardId };
        entries.Add(e);
        return e;
    }

    public bool Allows(string keycardId, string nodeStableId)
    {
        var e = Find(keycardId);
        if (e == null) return false;
        if (e.allowedNodeIds == null) return false;
        for (int i = 0; i < e.allowedNodeIds.Count; i++)
            if (e.allowedNodeIds[i] == nodeStableId)
                return true;
        return e.boundNodeId == nodeStableId;
    }

    public Entry Find(string keycardId)
    {
        if (string.IsNullOrEmpty(keycardId)) return null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i] != null && entries[i].keycardId == keycardId)
                return entries[i];
        return null;
    }

    public void Bind(string keycardId, string nodeStableId, IList<string> actorIds = null)
    {
        var e = GetOrCreate(keycardId);
        e.boundNodeId = nodeStableId;
        if (!e.allowedNodeIds.Contains(nodeStableId))
            e.allowedNodeIds.Add(nodeStableId);
        if (actorIds != null)
        {
            e.actorIdsAtNode.Clear();
            for (int i = 0; i < actorIds.Count; i++)
                if (!string.IsNullOrEmpty(actorIds[i]))
                    e.actorIdsAtNode.Add(actorIds[i]);
        }
    }
}
