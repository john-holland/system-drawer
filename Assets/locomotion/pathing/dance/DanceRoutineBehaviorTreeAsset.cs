using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DanceRoutine", menuName = "Locomotion/Animation/Dance Routine")]
public sealed class DanceRoutineBehaviorTreeAsset : ScriptableObject
{
    static readonly DanceMediaSpan[] EmptySpans = Array.Empty<DanceMediaSpan>();

    public string routineId;
    public string displayName = "New Dance";
    public List<int> moveAnimationIndices = new List<int>();
    public List<DancePairing> pairings = new List<DancePairing>();
    public bool allowIntersect;
    public string catalogModeId = "bar_sway";
    public WebcamAnimRecordingAsset webcamTake;

    public bool containsDialog;
    public bool containsSong;
    public List<DanceMediaSpan> songSpans = new List<DanceMediaSpan>();
    public List<DanceMediaSpan> dialogSpans = new List<DanceMediaSpan>();
    public string dialogAnalysisModelSpec = "whisper@base";
    public string songAnalysisModelSpec = "music_analysis@stub";
    public float bpm = 120f;
    public int beatsPerBar = 4;
    public int subdivision = 4;
    [Range(0f, 1f)] public float quantize01 = 1f;

    /// <summary>Dialog spans when the checkbox is on; empty otherwise (lists are kept).</summary>
    public IReadOnlyList<DanceMediaSpan> ActiveDialogSpans =>
        containsDialog && dialogSpans != null ? dialogSpans : EmptySpans;

    /// <summary>Song spans when the checkbox is on; empty otherwise (lists are kept).</summary>
    public IReadOnlyList<DanceMediaSpan> ActiveSongSpans =>
        containsSong && songSpans != null ? songSpans : EmptySpans;

    public WebcamAnimTimelineGranularity TimelineGranularity =>
        webcamTake != null ? webcamTake.granularity : WebcamAnimTimelineGranularity.Millisecond;

    void OnValidate()
    {
        if (string.IsNullOrEmpty(routineId))
            routineId = name;
        if (string.IsNullOrEmpty(displayName))
            displayName = name;
        if (moveAnimationIndices == null)
            moveAnimationIndices = new List<int>();
        if (pairings == null)
            pairings = new List<DancePairing>();
        if (songSpans == null)
            songSpans = new List<DanceMediaSpan>();
        if (dialogSpans == null)
            dialogSpans = new List<DanceMediaSpan>();
        if (bpm < 1f)
            bpm = 120f;
        if (beatsPerBar < 1)
            beatsPerBar = 4;
        if (subdivision < 1)
            subdivision = 1;
        quantize01 = Mathf.Clamp01(quantize01);
        if (!allowIntersect)
            PruneIntersectingPairings();
        SnapAllSpans();
    }

    public void SnapAllSpans()
    {
        var g = TimelineGranularity;
        SnapList(songSpans, g);
        SnapList(dialogSpans, g);
    }

    void SnapList(List<DanceMediaSpan> list, WebcamAnimTimelineGranularity g)
    {
        if (list == null)
            return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] == null)
                continue;
            list[i].Snap(bpm, subdivision, quantize01, g);
        }
    }

    public double TimelineDurationMs()
    {
        if (webcamTake != null)
        {
            if (webcamTake.userDurationLimitMs > 0.0)
                return webcamTake.startMs + webcamTake.userDurationLimitMs;
            if (!webcamTake.userSetTimeline && webcamTake.cachedVideoDurationMs > webcamTake.endMs)
                return webcamTake.startMs + webcamTake.cachedVideoDurationMs;
            if (webcamTake.endMs > webcamTake.startMs)
                return webcamTake.endMs;
        }
        double max = 0;
        MaxEnd(ActiveSongSpans, ref max);
        MaxEnd(ActiveDialogSpans, ref max);
        return max > 0 ? max : 60_000;
    }

    static void MaxEnd(IReadOnlyList<DanceMediaSpan> list, ref double max)
    {
        if (list == null)
            return;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] != null && list[i].endMs > max)
                max = list[i].endMs;
        }
    }

    public void PruneIntersectingPairings()
    {
        if (pairings == null || pairings.Count < 2)
            return;
        var kept = new List<DancePairing>(pairings.Count);
        for (int i = 0; i < pairings.Count; i++)
        {
            var c = pairings[i];
            if (c == null)
                continue;
            if (!DanceMirrorMap.IsBlockedByIntersect(kept.ToArray(), c, allowIntersect: false))
                kept.Add(c);
        }
        if (kept.Count != pairings.Count)
        {
            pairings.Clear();
            pairings.AddRange(kept);
        }
    }

    public bool TryAddPairing(DancePairing candidate, out string error)
    {
        error = null;
        if (candidate == null)
        {
            error = "pairing required";
            return false;
        }
        if (pairings == null)
            pairings = new List<DancePairing>();
        if (DanceMirrorMap.IsBlockedByIntersect(pairings.ToArray(), candidate, allowIntersect))
        {
            error = "pairing intersects an existing map line (enable allowIntersect to keep it)";
            return false;
        }
        pairings.Add(candidate);
        return true;
    }
}
