using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Onset/pitch-ish feature extract from AudioClip samples → score events.</summary>
    public static class AudioDspFeatureExtract
    {
        public static ScoreDocument Extract(AudioClip clip, string proxyVoiceId = "audio-0", float threshold = 0.15f)
        {
            var doc = new ScoreDocument { title = clip != null ? clip.name : "audio", bpm = 120f };
            if (clip == null) return doc;

            int len = clip.samples * clip.channels;
            var samples = new float[len];
            clip.GetData(samples, 0);
            int hop = Mathf.Max(1, clip.frequency / 20);
            var events = new List<ScoreEvent>();
            float prevEnergy = 0f;
            for (int i = 0; i + hop < samples.Length; i += hop)
            {
                float energy = 0f;
                for (int j = 0; j < hop; j++)
                    energy += samples[i + j] * samples[i + j];
                energy = Mathf.Sqrt(energy / hop);
                if (energy > threshold && energy > prevEnergy * 1.4f)
                {
                    float t = i / (float)(clip.frequency * clip.channels);
                    float zc = ZeroCrossRate(samples, i, hop);
                    int midi = Mathf.Clamp(Mathf.RoundToInt(69f + 12f * Mathf.Log(Mathf.Max(20f, zc * clip.frequency) / 440f, 2f)), 24, 96);
                    events.Add(new ScoreEvent
                    {
                        timeSec = t,
                        midiNote = midi,
                        velocity01 = Mathf.Clamp01(energy),
                        proxyVoiceId = proxyVoiceId,
                        family = InstrumentFamily.Generic
                    });
                }
                prevEnergy = energy;
            }
            doc.events = events.ToArray();
            return doc;
        }

        static float ZeroCrossRate(float[] s, int start, int hop)
        {
            int crosses = 0;
            for (int i = start + 1; i < start + hop && i < s.Length; i++)
            {
                if (s[i - 1] >= 0f && s[i] < 0f) crosses++;
                else if (s[i - 1] < 0f && s[i] >= 0f) crosses++;
            }
            return crosses / (float)hop;
        }
    }
}
