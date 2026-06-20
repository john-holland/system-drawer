using System;
using System.Collections.Generic;
using Locomotion.Narrative;

/// <summary>
/// Resolves whether Non-IK kinematic playback should be used for a clip/layer.
/// </summary>
public static class AnimationPlaybackPolicyResolver
{
    public const string NonIkAnimationKey = "non-ik-animation";

    public static bool ResolveNonIkAnimation(
        IReadOnlyList<PromptSegment> promptSegments,
        IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings = null,
        ABTClipConfig clipConfig = null,
        RagdollAnimationSet animationSet = null,
        bool travelAgentPreferNonIk = false)
    {
        if (TryGetBoolFromPrompt(promptSegments, NonIkAnimationKey, out bool fromPrompt))
            return fromPrompt;

        if (clauseBindings != null)
        {
            foreach (var b in clauseBindings)
            {
                if (b != null &&
                    string.Equals(b.propertyKey, NonIkAnimationKey, System.StringComparison.OrdinalIgnoreCase) &&
                    TryParseBool(b.propertyValue, out bool v) && v)
                    return true;
            }
        }

        if (clipConfig != null && clipConfig.nonIkAnimation)
            return true;

        if (animationSet != null && animationSet.preferNonIkPlayback)
            return true;

        return travelAgentPreferNonIk;
    }

    /// <summary>
    /// Phrase-scoped resolution: prompt/clause/lemma checks use active-phrase segments first, then fall back to clip/set/travel flags.
    /// </summary>
    public static bool ResolveNonIkForActivePhrase(
        string activePhrase,
        IReadOnlyList<PromptSegment> scopedPromptSegments,
        IReadOnlyList<LocalizationClauseBindingRecord> scopedBindings,
        ABTClipConfig clipConfig = null,
        RagdollAnimationSet animationSet = null,
        bool travelAgentPreferNonIk = false)
    {
        if (!string.IsNullOrWhiteSpace(activePhrase))
        {
            if (TryGetBoolFromPrompt(scopedPromptSegments, NonIkAnimationKey, out bool fromPrompt))
                return fromPrompt;

            if (scopedBindings != null)
            {
                foreach (var b in scopedBindings)
                {
                    if (b != null &&
                        string.Equals(b.propertyKey, NonIkAnimationKey, StringComparison.OrdinalIgnoreCase) &&
                        TryParseBool(b.propertyValue, out bool v) && v)
                        return true;
                }
            }
        }

        if (clipConfig != null && clipConfig.nonIkAnimation)
            return true;

        if (animationSet != null && animationSet.preferNonIkPlayback)
            return true;

        return travelAgentPreferNonIk;
    }

    public static bool TryGetBoolFromPrompt(IReadOnlyList<PromptSegment> segments, string key, out bool value)
    {
        value = false;
        if (segments == null || string.IsNullOrEmpty(key))
            return false;

        foreach (PromptSegment seg in segments)
        {
            if (seg == null || !seg.isPlaceholder || seg.placeholderParams == null)
                continue;
            if (seg.placeholderParams.TryGetValue(key, out string raw) && TryParseBool(raw, out value))
                return true;
        }
        return false;
    }

    public static bool TryParseBool(string raw, out bool value)
    {
        value = false;
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        raw = raw.Trim();
        if (raw == "1" || raw.Equals("true", System.StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("yes", System.StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }
        if (raw == "0" || raw.Equals("false", System.StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("no", System.StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }
        return false;
    }

    /// <summary>Resolve bool: prompt → clause property binding → lemma entry property → default.</summary>
    public static bool ResolveEffectiveBool(
        string propertyKey,
        IReadOnlyList<PromptSegment> promptSegments,
        IReadOnlyList<LocalizationClauseBindingRecord> clauseBindings,
        IReadOnlyList<ThesaurusEntryPropertyRecord> lemmaProperties,
        string specDefault,
        int charStart = -1,
        int charEnd = -1)
    {
        if (TryGetBoolFromPrompt(promptSegments, propertyKey, out bool fromPrompt))
            return fromPrompt;

        if (clauseBindings != null)
        {
            foreach (var b in clauseBindings)
            {
                if (b == null || !string.Equals(b.propertyKey, propertyKey, StringComparison.OrdinalIgnoreCase))
                    continue;
                var kind = b.bindingKind ?? LocalizationBindingKinds.Property;
                if (kind != LocalizationBindingKinds.Property && kind != LocalizationBindingKinds.Localization)
                    continue;
                if (charStart >= 0 && charEnd > charStart)
                {
                    if (b.charEnd <= charStart || b.charStart >= charEnd)
                        continue;
                }
                if (TryParseBool(b.propertyValue, out bool v))
                    return v;
            }
        }

        if (lemmaProperties != null)
        {
            foreach (var p in lemmaProperties)
            {
                if (p != null && string.Equals(p.propertyKey, propertyKey, StringComparison.OrdinalIgnoreCase) &&
                    TryParseBool(p.propertyValue, out bool v))
                    return v;
            }
        }

        if (TryParseBool(specDefault, out bool fromDefault))
            return fromDefault;

        return false;
    }
}
