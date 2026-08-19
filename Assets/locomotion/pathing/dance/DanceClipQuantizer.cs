using System.Collections.Generic;
using Locomotion.Audio;
using Locomotion.Narrative.Music;
using UnityEngine;

/// <summary>Collects AudioClips from the running mixer / sound-machine rack and snaps them onto the beat grid.</summary>
public static class DanceClipQuantizer
{
    public static List<DanceMediaSpan> FromSources(
        CausalityMusicBridge bridge,
        DigitalEffectsMachine machine,
        float bpm,
        int subdivision,
        float quantize01,
        WebcamAnimTimelineGranularity granularity)
    {
        var clips = new List<AudioClip>();
        if (bridge != null)
        {
            AddClip(clips, bridge.backgroundSource);
            AddClip(clips, bridge.fontSource);
            AddClip(clips, bridge.accentSource);
            if (bridge.LastPlan != null && bridge.LastPlan.stemSlots != null)
            {
                for (int i = 0; i < bridge.LastPlan.stemSlots.Count; i++)
                {
                    var slot = bridge.LastPlan.stemSlots[i];
                    if (slot.clip != null && !clips.Contains(slot.clip))
                        clips.Add(slot.clip);
                }
            }
        }

        if (machine != null)
        {
            var sources = machine.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
                AddClip(clips, sources[i]);
        }

        var spans = new List<DanceMediaSpan>(clips.Count);
        double cursor = 0;
        for (int i = 0; i < clips.Count; i++)
        {
            var clip = clips[i];
            double durMs = clip.length * 1000.0;
            if (durMs < 1)
                durMs = 1000;
            var span = new DanceMediaSpan
            {
                startMs = cursor,
                endMs = cursor + durMs,
                label = clip.name,
                clip = clip
            };
            span.Snap(bpm, subdivision, quantize01, granularity);
            cursor = span.endMs;
            spans.Add(span);
        }
        return spans;
    }

    static void AddClip(List<AudioClip> clips, AudioSource source)
    {
        if (source == null || source.clip == null)
            return;
        if (!clips.Contains(source.clip))
            clips.Add(source.clip);
    }
}
