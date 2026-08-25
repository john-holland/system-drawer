using System;
using UnityEngine;

/// <summary>Play / pause / stop + snapped playhead for webcam / vehicle video takes.</summary>
[Serializable]
public sealed class WebcamAnimTimeScrubber
{
    public double playheadMs;
    public bool playing;
    public double startMs;
    public double endMs = 1000;
    public WebcamAnimTimelineGranularity granularity = WebcamAnimTimelineGranularity.Millisecond;

    public double DurationMs => Math.Max(1.0, endMs - startMs);

    public void Bind(WebcamAnimRecordingAsset asset)
    {
        if (asset == null) return;
        startMs = asset.startMs;
        endMs = Math.Max(asset.endMs, asset.startMs + 1.0);
        granularity = asset.granularity;
        playheadMs = WebcamAnimTimelineGranularityUtil.SnapMs(
            Math.Max(startMs, Math.Min(endMs, playheadMs)), granularity);
    }

    public void Play() => playing = true;

    public void Pause() => playing = false;

    public void Stop()
    {
        playing = false;
        playheadMs = startMs;
    }

    public void Seek(double ms)
    {
        playheadMs = WebcamAnimTimelineGranularityUtil.SnapMs(
            Math.Max(startMs, Math.Min(endMs, ms)), granularity);
    }

    public void Tick(double deltaSec)
    {
        if (!playing) return;
        Seek(playheadMs + Math.Max(0.0, deltaSec) * 1000.0);
        if (playheadMs >= endMs - 1e-6)
            Stop();
    }

    public float Normalized01 =>
        (float)((playheadMs - startMs) / DurationMs);
}
