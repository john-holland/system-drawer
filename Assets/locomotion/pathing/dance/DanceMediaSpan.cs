using System;
using Locomotion.Audio;
using UnityEngine;

/// <summary>Start/stop window on the dance timeline (song or dialogue).</summary>
[Serializable]
public sealed class DanceMediaSpan
{
    public double startMs;
    public double endMs = 1000;
    public string label = "";
    public string audioRef = "";
    public string dialogueSetId = "";
    public AudioClip clip;

    public void Snap(float bpm, int subdivision, float quantize01, WebcamAnimTimelineGranularity granularity)
    {
        startMs = SnapOne(startMs, bpm, subdivision, quantize01, granularity);
        endMs = SnapOne(endMs, bpm, subdivision, quantize01, granularity);
        if (endMs < startMs)
            endMs = startMs;
    }

    public static double SnapOne(
        double ms,
        float bpm,
        int subdivision,
        float quantize01,
        WebcamAnimTimelineGranularity granularity)
    {
        float sec = (float)(ms / 1000.0);
        sec = PlayerInteractionQuantizer.QuantizeTime(sec, bpm, quantize01, Mathf.Max(1, subdivision));
        return WebcamAnimTimelineGranularityUtil.SnapMs(sec * 1000.0, granularity);
    }
}
