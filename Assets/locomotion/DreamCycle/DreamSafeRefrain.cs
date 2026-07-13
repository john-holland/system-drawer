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

    /// <summary>Play-within-dream unwrap mode (string-equivalent; floats stay floats).</summary>
    public enum DreamUnwrapMode
    {
        None,
        EscapismPreview,
        PlayThoughtUnpack
    }

    [Serializable]
    public struct PlayImprobabilityAudit
    {
        [Range(0f, 1f)] public float inductionPrior;
        [Range(0f, 1f)] public float reproductionCoeff;
        [Range(0f, 1f)] public float fidelity01;
        [Range(0f, 1f)] public float success01;
        [Range(0f, 1f)] public float improbability01;
        public DreamUnwrapMode unwrapMode;
        public string refrainLabel;
        public string memoryMode;
        public bool foundationClamped;

        public static PlayImprobabilityAudit FromApiFields(
            float inductionPrior,
            float reproductionCoeff,
            float fidelity01,
            float success01,
            float improbability01,
            string unwrapMode,
            string refrainLabel,
            string memoryMode,
            bool foundationClamped)
        {
            return new PlayImprobabilityAudit
            {
                inductionPrior = Mathf.Clamp01(inductionPrior),
                reproductionCoeff = Mathf.Clamp01(reproductionCoeff),
                fidelity01 = Mathf.Clamp01(fidelity01),
                success01 = Mathf.Clamp01(success01),
                improbability01 = Mathf.Clamp01(improbability01),
                unwrapMode = ParseUnwrapMode(unwrapMode),
                refrainLabel = refrainLabel,
                memoryMode = memoryMode,
                foundationClamped = foundationClamped
            };
        }

        public static DreamUnwrapMode ParseUnwrapMode(string mode)
        {
            if (string.Equals(mode, "play_thought_unpack", StringComparison.OrdinalIgnoreCase))
                return DreamUnwrapMode.PlayThoughtUnpack;
            if (string.Equals(mode, "escapism_preview", StringComparison.OrdinalIgnoreCase))
                return DreamUnwrapMode.EscapismPreview;
            return DreamUnwrapMode.None;
        }

        public static string UnwrapModeLabel(DreamUnwrapMode mode)
        {
            switch (mode)
            {
                case DreamUnwrapMode.PlayThoughtUnpack:
                    return "play thought unpack (non-authoritative)";
                case DreamUnwrapMode.EscapismPreview:
                    return "escapism preview (non-authoritative)";
                default:
                    return DreamSafeRefrainSettings.Default.refrainLabel;
            }
        }
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
            return Apply(fragment, buffer, settings, default);
        }

        public static DreamFragment Apply(
            DreamFragment fragment,
            DreamMemoryBuffer buffer,
            DreamSafeRefrainSettings settings,
            PlayImprobabilityAudit playAudit)
        {
            if (string.IsNullOrEmpty(settings.refrainLabel))
                settings = DreamSafeRefrainSettings.Default;

            if (playAudit.unwrapMode != DreamUnwrapMode.None)
            {
                if (!string.IsNullOrEmpty(playAudit.refrainLabel))
                    settings.refrainLabel = playAudit.refrainLabel;
                else
                    settings.refrainLabel = PlayImprobabilityAudit.UnwrapModeLabel(playAudit.unwrapMode);
            }

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
            if (playAudit.unwrapMode == DreamUnwrapMode.EscapismPreview)
            {
                text += " [escapism preview; improbability=1]";
                // Preview-density: keep narrative, never gate developer story.
                if (playAudit.foundationClamped)
                    fragment.improbability01 = 1f;
            }
            else if (playAudit.unwrapMode == DreamUnwrapMode.PlayThoughtUnpack)
            {
                text += " [play thought unpack]";
            }

            if (settings.fearProjectionMode == FearProjectionMode.Distant && peak > 0.4f)
                text += " [projected to distant dreamscape, far from rest]";
            else if (settings.fearProjectionMode == FearProjectionMode.Dissociated && peak > 0.4f)
                text += " [dissociated recall, not at bedside]";

            fragment.label = settings.refrainLabel;
            fragment.narrativeText = $"{settings.refrainLabel}: {text} (bed distance {distance:F2})";
            fragment.confidence = severity;
            fragment.isDreamMemory = true;
            fragment.distanceFromBed = distance;
            fragment.unwrapMode = playAudit.unwrapMode;
            if (playAudit.unwrapMode != DreamUnwrapMode.None && !playAudit.foundationClamped)
                fragment.improbability01 = playAudit.improbability01;
            else if (playAudit.foundationClamped)
                fragment.improbability01 = 1f;
            fragment.fidelity01 = playAudit.fidelity01;
            return fragment;
        }
    }
}
