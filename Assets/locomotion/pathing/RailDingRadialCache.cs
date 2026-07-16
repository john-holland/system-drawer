using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RailDingCacheEntry
{
    public string railingId;
    public int azimuthIndex;
    public int listenerBand;
    public string binarySampleId;
    public float transmission01 = 1f;
}

/// <summary>
/// Prebaked radial ding samples for stairwell railings. Playback supports DING DONG chains.
/// </summary>
[CreateAssetMenu(fileName = "RailDingRadialCache", menuName = "Locomotion/Rail Ding Radial Cache", order = 122)]
public sealed class RailDingRadialCache : ScriptableObject
{
    public int azimuthBins = 8;
    public int listenerBands = 3;
    public List<RailDingCacheEntry> entries = new List<RailDingCacheEntry>();

    readonly Dictionary<string, RailDingCacheEntry> _index = new Dictionary<string, RailDingCacheEntry>();

    public void RebuildIndex()
    {
        _index.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e == null || string.IsNullOrEmpty(e.railingId)) continue;
            _index[Key(e.railingId, e.azimuthIndex, e.listenerBand)] = e;
        }
    }

    public static string Key(string railingId, int azimuth, int band)
        => railingId + "|" + azimuth + "|" + band;

    public bool TryGet(string railingId, int azimuthIndex, int listenerBand, out RailDingCacheEntry entry)
    {
        if (_index.Count == 0) RebuildIndex();
        return _index.TryGetValue(Key(railingId, azimuthIndex, listenerBand), out entry);
    }

    public void Upsert(RailDingCacheEntry entry)
    {
        if (entry == null || string.IsNullOrEmpty(entry.railingId)) return;
        string k = Key(entry.railingId, entry.azimuthIndex, entry.listenerBand);
        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (e != null && Key(e.railingId, e.azimuthIndex, e.listenerBand) == k)
            {
                entries[i] = entry;
                RebuildIndex();
                return;
            }
        }
        entries.Add(entry);
        RebuildIndex();
    }

    /// <summary>Editor/runtime prebake stub: fills azimuth × band with placeholder binary ids + transmission falloff.</summary>
    public void PrebakeRailing(string railingId, string sampleIdPrefix = "rail_ding")
    {
        for (int a = 0; a < Mathf.Max(1, azimuthBins); a++)
        {
            for (int b = 0; b < Mathf.Max(1, listenerBands); b++)
            {
                float transmission = Mathf.Clamp01(1f - b * 0.28f - a * 0.02f);
                Upsert(new RailDingCacheEntry
                {
                    railingId = railingId,
                    azimuthIndex = a,
                    listenerBand = b,
                    binarySampleId = sampleIdPrefix + "_" + railingId + "_a" + a + "_b" + b,
                    transmission01 = transmission
                });
            }
        }
    }

    public int AzimuthIndex(Vector3 railingPos, Vector3 strikePos)
    {
        var flat = strikePos - railingPos;
        flat.y = 0f;
        if (flat.sqrMagnitude < 1e-6f) return 0;
        float ang = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
        if (ang < 0f) ang += 360f;
        int bins = Mathf.Max(1, azimuthBins);
        return Mathf.Clamp(Mathf.FloorToInt(ang / (360f / bins)), 0, bins - 1);
    }
}

/// <summary>Plays DING DONG chains from a radial cache (procedural timing).</summary>
public sealed class RailDingChainPlayer : MonoBehaviour
{
    public RailDingRadialCache cache;
    public AudioSource audioSource;
    public float intervalSeconds = 0.18f;

    readonly Queue<(string sampleId, float transmission)> _queue = new Queue<(string, float)>();
    float _nextPlayTime;
    bool _playing;

    public void PlayDingChain(string railingId, Vector3 railingPos, Vector3 strikePos, int count, int listenerBand = 0)
    {
        if (cache == null || count <= 0) return;
        int az = cache.AzimuthIndex(railingPos, strikePos);
        for (int i = 0; i < count; i++)
        {
            if (cache.TryGet(railingId, az, listenerBand, out var entry) && entry != null)
                _queue.Enqueue((entry.binarySampleId, entry.transmission01));
            else
                _queue.Enqueue(("rail_ding_fallback", 1f));
        }
        _playing = true;
        _nextPlayTime = Time.unscaledTime;
    }

    void Update()
    {
        if (!_playing || _queue.Count == 0)
        {
            _playing = _queue.Count > 0;
            return;
        }
        if (Time.unscaledTime < _nextPlayTime) return;
        var (sampleId, transmission) = _queue.Dequeue();
        if (audioSource != null)
        {
            audioSource.volume = Mathf.Clamp01(transmission);
            // Placeholder one-shot: pitch wobble encodes DING vs DONG.
            audioSource.pitch = _queue.Count % 2 == 0 ? 1.05f : 0.92f;
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        Debug.Log($"[RailDing] {sampleId} t={transmission:0.00}");
        _nextPlayTime = Time.unscaledTime + intervalSeconds;
    }
}
