using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Client runtime cache sharing positional/velocity/aesthetic lemma state across Continuuuum dimensions.
/// Prefer share over cold reload of systems like TravelAgent.
/// </summary>
[DefaultExecutionOrder(-200)]
public sealed class SharedDimensionalGenericCache : MonoBehaviour
{
    public static SharedDimensionalGenericCache Instance { get; private set; }

    readonly Dictionary<string, DimensionalCacheEntry> _entries = new Dictionary<string, DimensionalCacheEntry>();
    readonly HashSet<DimensionalLemmaBinding> _bindings = new HashSet<DimensionalLemmaBinding>();
    readonly Dictionary<string, DimensionalActorPolicy> _policyByLemma = new Dictionary<string, DimensionalActorPolicy>(StringComparer.Ordinal);

    public string ActiveGameSlug { get; set; } = "main";
    public int ActiveDimIndex { get; set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static SharedDimensionalGenericCache Ensure()
    {
        if (Instance != null)
            return Instance;
        var existing = FindAnyObjectByType<SharedDimensionalGenericCache>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }
        var go = new GameObject("SharedDimensionalGenericCache");
        return go.AddComponent<SharedDimensionalGenericCache>();
    }

    public void Register(DimensionalLemmaBinding binding)
    {
        if (binding != null)
            _bindings.Add(binding);
    }

    public void Unregister(DimensionalLemmaBinding binding)
    {
        if (binding != null)
            _bindings.Remove(binding);
    }

    public void SetPolicy(string lemmaEntryId, DimensionalActorPolicy policy)
    {
        if (string.IsNullOrEmpty(lemmaEntryId))
            return;
        _policyByLemma[lemmaEntryId] = policy;
    }

    public DimensionalActorPolicy GetPolicy(string lemmaEntryId)
    {
        if (!string.IsNullOrEmpty(lemmaEntryId) && _policyByLemma.TryGetValue(lemmaEntryId, out var p))
            return p;
        return DimensionalActorPolicy.KeepAlive;
    }

    public static DimensionalActorPolicy ParsePolicy(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return DimensionalActorPolicy.KeepAlive;
        switch (raw.Trim().ToLowerInvariant())
        {
            case "aesthetic-only":
            case "aestheticonly":
            case "aesthetic":
                return DimensionalActorPolicy.AestheticOnly;
            case "replace":
            case "replace-actor":
            case "replaceactor":
                return DimensionalActorPolicy.ReplaceActor;
            default:
                return DimensionalActorPolicy.KeepAlive;
        }
    }

    public void Upsert(DimensionalCacheEntry entry)
    {
        if (entry == null)
            return;
        _entries[entry.key.Compact] = entry;
    }

    public bool TryGet(DimensionalCacheKey key, out DimensionalCacheEntry entry) =>
        _entries.TryGetValue(key.Compact, out entry);

    public void CaptureFromScene(string game, int dim)
    {
        ActiveGameSlug = game ?? "main";
        ActiveDimIndex = dim;
        foreach (var binding in _bindings)
        {
            if (binding == null)
                continue;
            var entry = binding.CaptureSlot(ActiveGameSlug, dim);
            if (entry != null)
                Upsert(entry);
        }
    }

    /// <summary>Copy positional (+ optional velocity) from one dim to another for matching instance keys.</summary>
    public int CopySharedPositional(int fromDim, int toDim, Func<DimensionalCacheKey, bool> keyFilter = null)
    {
        var game = ActiveGameSlug ?? "main";
        var snapshot = new List<DimensionalCacheEntry>(_entries.Values);
        int n = 0;
        foreach (var src in snapshot)
        {
            if (src == null || src.key.dim != fromDim)
                continue;
            if (!string.Equals(src.key.game, game, StringComparison.Ordinal))
                continue;
            if (keyFilter != null && !keyFilter(src.key))
                continue;
            var destKey = new DimensionalCacheKey(game, toDim, src.key.lemmaEntryId, src.key.instanceStableId);
            DimensionalCacheEntry dest;
            if (!_entries.TryGetValue(destKey.Compact, out dest) || dest == null)
            {
                dest = new DimensionalCacheEntry
                {
                    key = destKey,
                    policy = src.policy,
                    aesthetic = src.aesthetic != null
                        ? new DimensionalAestheticSlot
                        {
                            skinKey = src.aesthetic.skinKey,
                            paintKeys = src.aesthetic.paintKeys,
                            dimensionalActorPolicy = src.aesthetic.dimensionalActorPolicy
                        }
                        : new DimensionalAestheticSlot()
                };
            }
            dest.positional = ClonePositional(src.positional);
            Upsert(dest);
            n++;
        }
        return n;
    }

    public void ApplyKeepAliveBindings(string game, int dim)
    {
        ActiveGameSlug = game ?? "main";
        ActiveDimIndex = dim;
        foreach (var binding in _bindings)
        {
            if (binding == null)
                continue;
            var policy = binding.ResolvedPolicy;
            if (policy == DimensionalActorPolicy.ReplaceActor)
            {
                binding.ApplyReplace(game, dim);
                continue;
            }
            binding.ApplyKeepAlive(game, dim);
        }
    }

    public IEnumerable<DimensionalLemmaBinding> Bindings => _bindings;

    public void PersistToPlayerPrefs(string prefsKey = "Continuuuum.SharedDimensionalGenericCache")
    {
        // Compact: count + serialized essentials
        var lines = new List<string>();
        foreach (var e in _entries.Values)
        {
            if (e?.positional == null)
                continue;
            var p = e.positional;
            lines.Add(string.Join(";",
                e.key.Compact,
                p.worldPos.x, p.worldPos.y, p.worldPos.z,
                p.worldRot.x, p.worldRot.y, p.worldRot.z, p.worldRot.w,
                p.lossyScale.x, p.lossyScale.y, p.lossyScale.z,
                p.hasVelocity ? 1 : 0,
                p.linearVelocity.x, p.linearVelocity.y, p.linearVelocity.z,
                (int)e.policy));
        }
        PlayerPrefs.SetString(prefsKey, string.Join("\n", lines));
        PlayerPrefs.Save();
    }

    static DimensionalPositionalSlot ClonePositional(DimensionalPositionalSlot s)
    {
        if (s == null)
            return new DimensionalPositionalSlot();
        return new DimensionalPositionalSlot
        {
            worldPos = s.worldPos,
            worldRot = s.worldRot,
            lossyScale = s.lossyScale,
            hasVelocity = s.hasVelocity,
            linearVelocity = s.linearVelocity,
            angularVelocity = s.angularVelocity
        };
    }
}
