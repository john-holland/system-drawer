using UnityEngine;

/// <summary>Registers a lemma-bound instance with SharedDimensionalGenericCache.</summary>
[AddComponentMenu("Continuuuum/Dimensions/Dimensional Lemma Binding")]
public sealed class DimensionalLemmaBinding : MonoBehaviour
{
    public string lemmaEntryId;
    [Tooltip("Stable id across dims; defaults to scene path hash.")]
    public string instanceStableId;
    public DimensionalActorPolicy policy = DimensionalActorPolicy.KeepAlive;
    [Tooltip("When set, overrides policy from string (keep-alive|aesthetic-only|replace).")]
    public string policyOverrideProperty;

    public GameObject replacePrefab;
    [Tooltip("Optional aesthetic paint keys captured/restored without Locomotion asm coupling.")]
    public string[] paintKeys;
    DimensionalLemmaPosition _position;
    DimensionalLemmaVelocityBridge _velocity;

    public DimensionalActorPolicy ResolvedPolicy
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(policyOverrideProperty))
                return SharedDimensionalGenericCache.ParsePolicy(policyOverrideProperty);
            var cache = SharedDimensionalGenericCache.Instance;
            if (cache != null && !string.IsNullOrEmpty(lemmaEntryId))
            {
                var fromCache = cache.GetPolicy(lemmaEntryId);
                if (policy != DimensionalActorPolicy.KeepAlive)
                    return policy;
                return fromCache;
            }
            return policy;
        }
    }

    void Awake()
    {
        if (string.IsNullOrEmpty(instanceStableId))
            instanceStableId = $"{gameObject.scene.name}:{GetHierarchyPath()}";
        _position = GetComponent<DimensionalLemmaPosition>() ?? GetComponentInChildren<DimensionalLemmaPosition>();
        _velocity = GetComponent<DimensionalLemmaVelocityBridge>() ?? GetComponentInChildren<DimensionalLemmaVelocityBridge>();
    }

    void OnEnable()
    {
        SharedDimensionalGenericCache.Ensure().Register(this);
    }

    void OnDisable()
    {
        SharedDimensionalGenericCache.Instance?.Unregister(this);
    }

    public DimensionalCacheEntry CaptureSlot(string game, int dim)
    {
        EnsureRefs();
        var key = new DimensionalCacheKey(game, dim, lemmaEntryId, instanceStableId);
        var entry = new DimensionalCacheEntry
        {
            key = key,
            policy = ResolvedPolicy,
            positional = new DimensionalPositionalSlot(),
            aesthetic = new DimensionalAestheticSlot()
        };
        if (_position != null)
            _position.WriteTo(entry.positional);
        else
        {
            entry.positional.worldPos = transform.position;
            entry.positional.worldRot = transform.rotation;
            entry.positional.lossyScale = transform.lossyScale;
        }
        if (_velocity != null)
            _velocity.WriteTo(entry.positional);
        entry.aesthetic.paintKeys = paintKeys ?? System.Array.Empty<string>();
        entry.aesthetic.dimensionalActorPolicy = policy.ToString();
        return entry;
    }

    void EnsureRefs()
    {
        if (_position == null)
            _position = GetComponent<DimensionalLemmaPosition>() ?? GetComponentInChildren<DimensionalLemmaPosition>();
        if (_velocity == null)
            _velocity = GetComponent<DimensionalLemmaVelocityBridge>() ?? GetComponentInChildren<DimensionalLemmaVelocityBridge>();
    }

    public void ApplyKeepAlive(string game, int dim)
    {
        EnsureRefs();
        var cache = SharedDimensionalGenericCache.Ensure();
        // Prefer shared positional from previous dim, then this dim
        DimensionalCacheEntry entry = null;
        var key = new DimensionalCacheKey(game, dim, lemmaEntryId, instanceStableId);
        if (!cache.TryGet(key, out entry) || entry?.positional == null)
        {
            var prev = new DimensionalCacheKey(game, cache.ActiveDimIndex, lemmaEntryId, instanceStableId);
            cache.TryGet(prev, out entry);
        }
        if (entry?.positional == null)
            return;
        if (_position != null)
            _position.ApplyFrom(entry.positional);
        else
        {
            transform.SetPositionAndRotation(entry.positional.worldPos, entry.positional.worldRot);
        }
        if (_velocity != null && entry.positional.hasVelocity)
            _velocity.ApplyFrom(entry.positional);
        if (entry.aesthetic?.paintKeys != null && entry.aesthetic.paintKeys.Length > 0)
            paintKeys = entry.aesthetic.paintKeys;
        TryPaintSpatialDescription(paintKeys);
    }

    void TryPaintSpatialDescription(string[] keys)
    {
        if (keys == null || keys.Length == 0)
            return;
        // Optional Locomotion SpatialDescriptionComponent via reflection (no asm cycle).
        foreach (var mb in GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb == null || mb.GetType().Name != "SpatialDescriptionComponent")
                continue;
            var method = mb.GetType().GetMethod("PaintFromModifiers");
            method?.Invoke(mb, new object[] { keys });
            break;
        }
    }

    public void ApplyReplace(string game, int dim)
    {
        var cache = SharedDimensionalGenericCache.Ensure();
        var key = new DimensionalCacheKey(game, dim, lemmaEntryId, instanceStableId);
        cache.TryGet(key, out var entry);
        if (entry == null)
        {
            var prev = new DimensionalCacheKey(game, cache.ActiveDimIndex, lemmaEntryId, instanceStableId);
            cache.TryGet(prev, out entry);
        }
        if (replacePrefab == null)
        {
            Debug.LogWarning($"[DimensionalLemmaBinding] ReplaceActor but no replacePrefab on {name}");
            ApplyKeepAlive(game, dim);
            return;
        }
        var spawned = Instantiate(replacePrefab, transform.position, transform.rotation);
        if (entry?.positional != null)
        {
            spawned.transform.SetPositionAndRotation(entry.positional.worldPos, entry.positional.worldRot);
            var vel = spawned.GetComponent<DimensionalLemmaVelocityBridge>()
                      ?? spawned.GetComponentInChildren<DimensionalLemmaVelocityBridge>();
            if (vel != null && entry.positional.hasVelocity)
                vel.ApplyFrom(entry.positional);
        }
        var bind = spawned.GetComponent<DimensionalLemmaBinding>() ?? spawned.AddComponent<DimensionalLemmaBinding>();
        bind.lemmaEntryId = lemmaEntryId;
        bind.instanceStableId = instanceStableId;
        bind.policy = DimensionalActorPolicy.KeepAlive;
        gameObject.SetActive(false);
    }

    string GetHierarchyPath()
    {
        var t = transform;
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
