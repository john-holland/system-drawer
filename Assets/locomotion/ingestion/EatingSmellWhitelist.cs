using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Senses;

/// <summary>Whitelist of smell signatures that may stink out after eating (garlic, cumin, …).</summary>
[CreateAssetMenu(fileName = "EatingSmellWhitelist", menuName = "Locomotion/Ingestion/Eating Smell Whitelist")]
public sealed class EatingSmellWhitelist : ScriptableObject
{
    public List<string> allowedSignatures = new List<string> { "garlic", "cumin", "onion", "fish" };

    public bool IsAllowed(string signature)
    {
        if (string.IsNullOrEmpty(signature) || allowedSignatures == null) return false;
        for (int i = 0; i < allowedSignatures.Count; i++)
            if (string.Equals(allowedSignatures[i], signature, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    public void ApplyWhitelistedSmells(GameObject actor, IList<FoodSmellTag> tags, float durationSeconds = 120f)
    {
        if (actor == null || tags == null) return;
        for (int i = 0; i < tags.Count; i++)
        {
            var tag = tags[i];
            if (tag == null || !IsAllowed(tag.signature)) continue;
            var emitter = actor.GetComponent<SmellEmitter>() ?? actor.AddComponent<SmellEmitter>();
            emitter.signature = tag.signature;
            emitter.intensity = tag.intensity;
            emitter.emissionMultiplier = 1f;
            var timed = actor.GetComponent<EatingSmellTimedClear>() ?? actor.AddComponent<EatingSmellTimedClear>();
            timed.Schedule(tag.signature, durationSeconds);
        }
    }
}

/// <summary>Clears eating smell intensity after duration.</summary>
public sealed class EatingSmellTimedClear : MonoBehaviour
{
    readonly Dictionary<string, float> _until = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

    public void Schedule(string signature, float durationSeconds)
    {
        _until[signature ?? ""] = Time.unscaledTime + Mathf.Max(0.1f, durationSeconds);
    }

    void Update()
    {
        if (_until.Count == 0) return;
        var emitters = GetComponents<SmellEmitter>();
        var remove = new List<string>();
        foreach (var kv in _until)
        {
            if (Time.unscaledTime < kv.Value) continue;
            remove.Add(kv.Key);
            for (int i = 0; i < emitters.Length; i++)
            {
                if (emitters[i] != null &&
                    string.Equals(emitters[i].signature, kv.Key, StringComparison.OrdinalIgnoreCase))
                    emitters[i].emissionMultiplier = 0f;
            }
        }
        for (int i = 0; i < remove.Count; i++)
            _until.Remove(remove[i]);
    }
}
