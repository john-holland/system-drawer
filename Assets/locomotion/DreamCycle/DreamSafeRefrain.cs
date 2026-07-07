using System;
using UnityEngine;

namespace Locomotion.DreamCycle
{
    public enum DreamMemoryLayer
    {
        SingleDay,
        GoodDayHorizon,
        DeveloperDream
    }

    [Serializable]
    public struct DreamSafeRefrainSettings
    {
        public string[] bedAnchorLemmaIds;
        [Range(0f, 1f)] public float minNarrativeDistanceFromBed;
        [Range(0f, 1f)] public float maxAlertSeverity;
        public string refrainLabel;
        public FearProjectionMode fearProjectionMode;

        public static DreamSafeRefrainSettings Default => new DreamSafeRefrainSettings
        {
            bedAnchorLemmaIds = new[]
            {
                VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "pause"),
                VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "center"),
                VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "player"),
            },
            minNarrativeDistanceFromBed = 0.6f,
            maxAlertSeverity = 0.35f,
            refrainLabel = "dream memory (non-authoritative)",
            fearProjectionMode = FearProjectionMode.Distant
        };
    }

    public enum FearProjectionMode
    {
        Distant,
        Dissociated
    }

    /// <summary>Keeps LSTM dream recall non-authoritative; projects fears away from bed anchors.</summary>
    public static class DreamSafeRefrain
    {
        public static DreamFragment Apply(
            DreamFragment fragment,
            DreamMemoryBuffer buffer,
            DreamSafeRefrainSettings settings)
        {
            if (string.IsNullOrEmpty(settings.refrainLabel))
                settings = DreamSafeRefrainSettings.Default;

            float peak = 0f;
            if (buffer != null)
            {
                foreach (var frame in buffer.Snapshot())
                {
                    if (frame.dreamLayer == DreamMemoryLayer.GoodDayHorizon)
                        continue;
                    peak = Mathf.Max(peak, Mathf.Abs(frame.waveSample));
                }
            }

            float distance = Mathf.Clamp01(
                settings.minNarrativeDistanceFromBed + peak * (1f - settings.minNarrativeDistanceFromBed));
            float severity = Mathf.Min(settings.maxAlertSeverity, fragment.confidence * 0.5f);

            var text = fragment.narrativeText ?? "";
            if (settings.fearProjectionMode == FearProjectionMode.Distant && peak > 0.4f)
                text += " [projected to distant dreamscape, far from rest]";
            else if (settings.fearProjectionMode == FearProjectionMode.Dissociated && peak > 0.4f)
                text += " [dissociated recall, not at bedside]";

            fragment.label = settings.refrainLabel;
            fragment.narrativeText = $"{settings.refrainLabel}: {text} (bed distance {distance:F2})";
            fragment.confidence = severity;
            fragment.isDreamMemory = true;
            fragment.distanceFromBed = distance;
            return fragment;
        }
    }
}
