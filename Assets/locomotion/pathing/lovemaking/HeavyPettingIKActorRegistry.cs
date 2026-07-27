using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One actor entry for kiss / heavy-petting IK: mouth, section masks, optional open/close.</summary>
[Serializable]
public sealed class HeavyPettingIKActorEntry
{
    [Tooltip("Narrative / lemma actor key.")]
    public string actorKey;
    public GameObject actor;
    public RagdollSystem ragdoll;
    public MouthInteriorRuntime mouth;
    public string[] enabledRagdollSections;
    public string[] disabledRagdollSections;
    [Tooltip("Optional OpenCloseTopologyAsset (Locomotion.Open) — stored as ScriptableObject to avoid asmdef cycles.")]
    public ScriptableObject openCloseTopology;
    public GameObject openCloseRootTarget;

    public void AutoResolve()
    {
        if (actor == null) return;
        if (ragdoll == null)
            ragdoll = actor.GetComponent<RagdollSystem>() ?? actor.GetComponentInChildren<RagdollSystem>();
        if (mouth == null)
            mouth = actor.GetComponentInChildren<MouthInteriorRuntime>(true);
        if (openCloseRootTarget == null && openCloseTopology != null)
            openCloseRootTarget = actor;
    }
}

/// <summary>
/// Scene registry of kiss / heavy-petting actors (TravelAgentRegistry-style live set + key lookup).
/// Optional open/close topologies share the same actor keys; bake still uses OpenCloseTopologyBtBuilder.
/// </summary>
[AddComponentMenu("Locomotion/Love Making/Heavy Petting IK Actor Registry")]
public sealed class HeavyPettingIKActorRegistry : MonoBehaviour
{
    public List<HeavyPettingIKActorEntry> entries = new List<HeavyPettingIKActorEntry>();

    static readonly List<HeavyPettingIKActorRegistry> s_live = new List<HeavyPettingIKActorRegistry>(4);

    void OnEnable()
    {
        if (!s_live.Contains(this))
            s_live.Add(this);
        AutoResolveAll();
    }

    void OnDisable() => s_live.Remove(this);

    public void AutoResolveAll()
    {
        if (entries == null) return;
        for (int i = 0; i < entries.Count; i++)
            entries[i]?.AutoResolve();
    }

    public static HeavyPettingIKActorRegistry FindActive()
    {
        for (int i = s_live.Count - 1; i >= 0; i--)
        {
            if (s_live[i] != null && s_live[i].isActiveAndEnabled)
                return s_live[i];
            s_live.RemoveAt(i);
        }
        return UnityEngine.Object.FindAnyObjectByType<HeavyPettingIKActorRegistry>();
    }

    public bool TryGet(string actorKey, out HeavyPettingIKActorEntry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(actorKey) || entries == null) return false;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || string.IsNullOrEmpty(e.actorKey)) continue;
            if (string.Equals(e.actorKey, actorKey, StringComparison.OrdinalIgnoreCase))
            {
                e.AutoResolve();
                entry = e;
                return true;
            }
        }
        return false;
    }

    public bool TryGet(GameObject actor, out HeavyPettingIKActorEntry entry)
    {
        entry = null;
        if (actor == null || entries == null) return false;
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || e.actor == null) continue;
            if (e.actor == actor || e.actor.transform.IsChildOf(actor.transform) ||
                actor.transform.IsChildOf(e.actor.transform))
            {
                e.AutoResolve();
                entry = e;
                return true;
            }
        }
        return false;
    }

    /// <summary>Ephemeral entry when actor is not registered (mouth/ragdoll auto-filled).</summary>
    public static HeavyPettingIKActorEntry InventEphemeral(GameObject actor, string key = null)
    {
        var e = new HeavyPettingIKActorEntry
        {
            actorKey = key ?? (actor != null ? actor.name : ""),
            actor = actor
        };
        e.AutoResolve();
        return e;
    }

    public Vector3 ResolveLipMidpointWorld(HeavyPettingIKActorEntry entry)
    {
        if (entry == null) return Vector3.zero;
        entry.AutoResolve();
        if (entry.mouth != null)
            return entry.mouth.GetLipLoopMidpointWorld();
        if (entry.ragdoll != null)
        {
            var head = entry.ragdoll.GetBoneTransform("Head");
            if (head != null) return head.position;
        }
        return entry.actor != null ? entry.actor.transform.position : Vector3.zero;
    }

    public Transform ResolveLipMidAnchor(HeavyPettingIKActorEntry entry)
    {
        if (entry == null) return null;
        entry.AutoResolve();
        if (entry.mouth != null)
            return entry.mouth.EnsureLipMidAnchor();
        if (entry.ragdoll != null)
            return entry.ragdoll.GetBoneTransform("Head") ?? entry.ragdoll.GetBoneTransform("Jaw");
        return entry.actor != null ? entry.actor.transform : null;
    }

    public void ResolveSectionMask(HeavyPettingIKActorEntry entry, HashSet<string> enabledOut, HashSet<string> disabledOut)
    {
        enabledOut?.Clear();
        disabledOut?.Clear();
        if (entry == null) return;
        if (entry.enabledRagdollSections != null)
            for (int i = 0; i < entry.enabledRagdollSections.Length; i++)
                if (!string.IsNullOrEmpty(entry.enabledRagdollSections[i]))
                    enabledOut?.Add(entry.enabledRagdollSections[i]);
        if (entry.disabledRagdollSections != null)
            for (int i = 0; i < entry.disabledRagdollSections.Length; i++)
                if (!string.IsNullOrEmpty(entry.disabledRagdollSections[i]))
                    disabledOut?.Add(entry.disabledRagdollSections[i]);
    }
}
