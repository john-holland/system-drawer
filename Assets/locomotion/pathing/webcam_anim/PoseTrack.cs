using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PoseBoneSample
{
    public string traitId;
    public float timeMs;
    public Vector3 localPosition;
    public Quaternion localRotation = Quaternion.identity;
}

/// <summary>Detector output: bone traitId samples over time.</summary>
[Serializable]
public sealed class PoseTrack
{
    public string modelSpec;
    public List<PoseBoneSample> samples = new List<PoseBoneSample>();

    public int Count => samples != null ? samples.Count : 0;

    public static PoseTrack FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new PoseTrack();
        try
        {
            var track = JsonUtility.FromJson<PoseTrack>(json);
            if (track == null)
                return new PoseTrack();
            if (track.samples == null)
                track.samples = new List<PoseBoneSample>();
            return track;
        }
        catch
        {
            return new PoseTrack();
        }
    }

    public string ToJson() => JsonUtility.ToJson(this);

    /// <summary>Rewrite sample trait ids using source→target pairs. Unmapped ids stay as-is.</summary>
    public PoseTrack RemapTraitIds(IDictionary<string, string> sourceToTarget)
    {
        var next = new PoseTrack { modelSpec = modelSpec };
        if (samples == null)
            return next;
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            if (s == null)
                continue;
            string id = s.traitId ?? "";
            if (sourceToTarget != null && sourceToTarget.TryGetValue(id, out var mapped) && !string.IsNullOrEmpty(mapped))
                id = mapped;
            next.samples.Add(new PoseBoneSample
            {
                traitId = id,
                timeMs = s.timeMs,
                localPosition = s.localPosition,
                localRotation = s.localRotation
            });
        }
        return next;
    }

    public void CollectTraitIds(List<string> dest)
    {
        if (dest == null || samples == null)
            return;
        var seen = new HashSet<string>();
        for (int i = 0; i < samples.Count; i++)
        {
            var id = samples[i] != null ? samples[i].traitId : null;
            if (string.IsNullOrEmpty(id) || !seen.Add(id))
                continue;
            dest.Add(id);
        }
    }

    public bool TrySample(string traitId, float timeMs, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;
        if (samples == null || string.IsNullOrEmpty(traitId))
            return false;

        int lo = -1;
        int hi = -1;
        float loT = float.NegativeInfinity;
        float hiT = float.PositiveInfinity;
        for (int i = 0; i < samples.Count; i++)
        {
            var s = samples[i];
            if (s == null || s.traitId != traitId)
                continue;
            if (s.timeMs <= timeMs && s.timeMs >= loT)
            {
                lo = i;
                loT = s.timeMs;
            }
            if (s.timeMs >= timeMs && s.timeMs <= hiT)
            {
                hi = i;
                hiT = s.timeMs;
            }
        }

        if (lo < 0 && hi < 0)
            return false;
        if (lo < 0)
        {
            position = samples[hi].localPosition;
            rotation = samples[hi].localRotation;
            return true;
        }
        if (hi < 0 || lo == hi)
        {
            position = samples[lo].localPosition;
            rotation = samples[lo].localRotation;
            return true;
        }

        float span = hiT - loT;
        float u = span > 1e-6f ? Mathf.Clamp01((timeMs - loT) / span) : 0f;
        position = Vector3.Lerp(samples[lo].localPosition, samples[hi].localPosition, u);
        rotation = Quaternion.Slerp(samples[lo].localRotation, samples[hi].localRotation, u);
        return true;
    }
}
