using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single entry in the scene object registry. Identity key, reference, cloneable flag, and synonyms for scene-graph LSTM.
/// </summary>
[Serializable]
public class SceneObjectEntry
{
    [Tooltip("Identity key for this object (e.g. 'ball', 'target_ball').")]
    public string key;

    [Tooltip("Reference to the GameObject or Transform. For cloneable, may be prefab or scene instance.")]
    public GameObject reference;

    [Tooltip("If true, runtime can instantiate from this (e.g. projectiles, spawns). If false, use as scene reference only.")]
    public bool isCloneable;

    [Tooltip("Optional prefab for cloneable entries so runtime can instantiate.")]
    public GameObject prefabForClone;

    [Tooltip("Synonyms for lookup (e.g. 'ball', 'target_ball'). Used by Resolve(key) and for future scene-graph LSTM.")]
    public List<string> synonyms = new List<string>();

    public SceneObjectEntry() { }

    public SceneObjectEntry(string key, GameObject reference, bool isCloneable, List<string> synonyms = null)
    {
        this.key = key ?? "";
        this.reference = reference;
        this.isCloneable = isCloneable;
        this.synonyms = synonyms ?? new List<string>();
    }
}

/// <summary>
/// Central map of scene/prefab object references for behavior trees and sequences.
/// Cloneable vs references; synonym strings for Resolve and future scene-graph LSTM.
/// </summary>
public class SceneObjectRegistry : MonoBehaviour
{
    [Header("Cloneable (e.g. projectiles, spawns)")]
    [Tooltip("Entries that can be instantiated at runtime.")]
    public List<SceneObjectEntry> cloneable = new List<SceneObjectEntry>();

    [Header("References (e.g. scene targets, tools)")]
    [Tooltip("Scene object references only; not instantiated.")]
    public List<SceneObjectEntry> references = new List<SceneObjectEntry>();

    private Dictionary<string, SceneObjectEntry> _keyToCloneable;
    private Dictionary<string, SceneObjectEntry> _keyToReference;
    private Dictionary<string, string> _synonymToKey;
    private bool _dirty = true;

    private void BuildLookups()
    {
        if (!_dirty) return;
        _keyToCloneable = new Dictionary<string, SceneObjectEntry>(StringComparer.OrdinalIgnoreCase);
        _keyToReference = new Dictionary<string, SceneObjectEntry>(StringComparer.OrdinalIgnoreCase);
        _synonymToKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void AddEntry(SceneObjectEntry e, Dictionary<string, SceneObjectEntry> dict)
        {
            if (e == null || string.IsNullOrEmpty(e.key)) return;
            var k = e.key.Trim();
            if (string.IsNullOrEmpty(k)) return;
            dict[k] = e;
            _synonymToKey[k] = k;
            if (e.synonyms != null)
            {
                foreach (var s in e.synonyms)
                {
                    var sym = (s ?? "").Trim();
                    if (!string.IsNullOrEmpty(sym))
                        _synonymToKey[sym] = k;
                }
            }
        }

        if (cloneable != null)
        {
            foreach (var e in cloneable)
                AddEntry(e, _keyToCloneable);
        }
        if (references != null)
        {
            foreach (var e in references)
                AddEntry(e, _keyToReference);
        }
        _dirty = false;
    }

    private void OnValidate()
    {
        _dirty = true;
    }

    /// <summary>
    /// Resolve key or synonym to the canonical registry key. Returns null if not found. Use for ORM fill and preprocessor vocabulary.
    /// </summary>
    public string ResolveKey(string keyOrSynonym)
    {
        if (string.IsNullOrWhiteSpace(keyOrSynonym)) return null;
        BuildLookups();
        var key = keyOrSynonym.Trim();
        return _synonymToKey != null && _synonymToKey.TryGetValue(key, out var resolvedKey) ? resolvedKey : null;
    }

    /// <summary>
    /// Get all keys and synonyms for preprocessor allowed vocabulary. Returns a new list each time.
    /// </summary>
    public List<string> GetAllKeysAndSynonyms()
    {
        BuildLookups();
        var list = new List<string>();
        if (_synonymToKey == null) return list;
        foreach (var k in _synonymToKey.Keys)
            if (!string.IsNullOrEmpty(k)) list.Add(k);
        return list;
    }

    /// <summary>
    /// Resolve by identity key or synonym. Returns the GameObject reference (from either cloneable or references).
    /// Does not instantiate; use GetCloneable + Instantiate for cloneable entries.
    /// </summary>
    public GameObject Resolve(string keyOrSynonym)
    {
        if (string.IsNullOrWhiteSpace(keyOrSynonym)) return null;
        BuildLookups();
        var key = keyOrSynonym.Trim();
        if (_synonymToKey.TryGetValue(key, out var resolvedKey))
        {
            if (_keyToCloneable != null && _keyToCloneable.TryGetValue(resolvedKey, out var ce))
                return ce.reference;
            if (_keyToReference != null && _keyToReference.TryGetValue(resolvedKey, out var re))
                return re.reference;
        }
        return null;
    }

    /// <summary>
    /// Get cloneable entry by key or synonym. Use entry.reference or entry.prefabForClone to instantiate.
    /// </summary>
    public SceneObjectEntry GetCloneable(string keyOrSynonym)
    {
        if (string.IsNullOrWhiteSpace(keyOrSynonym)) return null;
        BuildLookups();
        var key = keyOrSynonym.Trim();
        if (_synonymToKey.TryGetValue(key, out var resolvedKey) && _keyToCloneable != null && _keyToCloneable.TryGetValue(resolvedKey, out var e))
            return e;
        return null;
    }

    /// <summary>
    /// Get reference-only entry by key or synonym.
    /// </summary>
    public SceneObjectEntry GetReference(string keyOrSynonym)
    {
        if (string.IsNullOrWhiteSpace(keyOrSynonym)) return null;
        BuildLookups();
        var key = keyOrSynonym.Trim();
        if (_synonymToKey.TryGetValue(key, out var resolvedKey) && _keyToReference != null && _keyToReference.TryGetValue(resolvedKey, out var e))
            return e;
        return null;
    }

    /// <summary>
    /// Register an entry at runtime. If isCloneable, adds to cloneable; otherwise to references.
    /// Invalidates lookups until next Resolve/Get.
    /// </summary>
    public void Register(string key, GameObject obj, bool isCloneable, List<string> synonyms = null)
    {
        if (string.IsNullOrWhiteSpace(key) || obj == null) return;
        var entry = new SceneObjectEntry(key.Trim(), obj, isCloneable, synonyms);
        if (isCloneable)
        {
            if (cloneable == null) cloneable = new List<SceneObjectEntry>();
            cloneable.Add(entry);
        }
        else
        {
            if (references == null) references = new List<SceneObjectEntry>();
            references.Add(entry);
        }
        _dirty = true;
    }
}
