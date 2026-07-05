using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Runtime cache for prompt spans, clause bindings, and lemma properties used to resolve Non-IK playback.
/// </summary>
[AddComponentMenu("Locomotion/Animation Playback Policy Context")]
public sealed class AnimationPlaybackPolicyContext : MonoBehaviour
{
    [Tooltip("Optional narrative prompt asset supplying active script text.")]
    public NarrativePromptAsset activePrompt;

    [Tooltip("Continuuuum draft script fallback when no prompt asset is set.")]
    [TextArea(2, 6)]
    public string activeScriptText = "";

    [Tooltip("Draft episode id for loading clause bindings from Continuuuum API at Start.")]
    public string draftEpisodeId = "";

    [Tooltip("Active travel/narrative phrase or event title for phrase-scoped policy.")]
    public string activePhrase = "";

    [Tooltip("Active event index (-1 when unused).")]
    public int activeEventIndex = -1;

    [SerializeField] LocalizationClauseBindingRecord[] clauseBindings = Array.Empty<LocalizationClauseBindingRecord>();
    [SerializeField] ThesaurusEntryPropertyRecord[] lemmaProperties = Array.Empty<ThesaurusEntryPropertyRecord>();
    [SerializeField] PlaybackPhraseBinding[] activePhraseBindings = Array.Empty<PlaybackPhraseBinding>();
    [SerializeField] LemmaConsumptionRegistry consumptionRegistry = new LemmaConsumptionRegistry();

    public IReadOnlyList<LocalizationClauseBindingRecord> ClauseBindings => clauseBindings;
    public IReadOnlyList<ThesaurusEntryPropertyRecord> LemmaProperties => lemmaProperties;
    public LemmaConsumptionRegistry ConsumptionRegistry => consumptionRegistry;

    public bool IsPhraseConsumed(string phrase, int eventIndex) =>
        consumptionRegistry != null && consumptionRegistry.IsConsumed(phrase, eventIndex);

    public void MarkPhraseConsumed(string phrase, int eventIndex) =>
        consumptionRegistry?.MarkConsumed(phrase, eventIndex);

    public string GetActiveScriptText()
    {
        if (activePrompt != null)
        {
            string t = activePrompt.GetActivePromptText();
            if (!string.IsNullOrEmpty(t))
                return t;
        }
        return activeScriptText ?? "";
    }

    public void SetClauseBindings(IEnumerable<LocalizationClauseBindingRecord> bindings)
    {
        clauseBindings = bindings != null
            ? new List<LocalizationClauseBindingRecord>(bindings).ToArray()
            : Array.Empty<LocalizationClauseBindingRecord>();
    }

    public void SetLemmaProperties(IEnumerable<ThesaurusEntryPropertyRecord> properties)
    {
        lemmaProperties = properties != null
            ? new List<ThesaurusEntryPropertyRecord>(properties).ToArray()
            : Array.Empty<ThesaurusEntryPropertyRecord>();
    }

    public void SetPhraseBindings(IEnumerable<PlaybackPhraseBinding> bindings)
    {
        activePhraseBindings = bindings != null
            ? new List<PlaybackPhraseBinding>(bindings).ToArray()
            : Array.Empty<PlaybackPhraseBinding>();
    }

    public void ApplyFromPromptAsset(NarrativePromptAsset asset, IEnumerable<PlaybackPhraseBinding> bindings)
    {
        activePrompt = asset;
        if (bindings != null)
            SetPhraseBindings(bindings);
    }

    public void ApplyFromPromptAsset(NarrativePromptAsset asset)
    {
        activePrompt = asset;
    }

    public IReadOnlyList<PromptSegment> GetAllSegments() =>
        PromptSpanParser.Parse(GetActiveScriptText());

    public IReadOnlyList<PromptSegment> GetSegmentsForActivePhrase()
    {
        var all = GetAllSegments();
        if (string.IsNullOrWhiteSpace(activePhrase))
            return Array.Empty<PromptSegment>();

        var scoped = new List<PromptSegment>();
        string phrase = activePhrase.Trim();
        foreach (PromptSegment seg in all)
        {
            if (seg == null || !seg.isPlaceholder)
                continue;
            if (PhraseMatchesPlaceholder(phrase, seg.placeholderName))
                scoped.Add(seg);
        }
        return scoped;
    }

    public IReadOnlyList<LocalizationClauseBindingRecord> GetBindingsForActivePhrase()
    {
        if (clauseBindings == null || clauseBindings.Length == 0)
            return Array.Empty<LocalizationClauseBindingRecord>();

        if (string.IsNullOrWhiteSpace(activePhrase))
            return clauseBindings;

        var scoped = new List<LocalizationClauseBindingRecord>();
        string phrase = activePhrase.Trim();
        foreach (var b in clauseBindings)
        {
            if (b == null)
                continue;
            if (!string.IsNullOrEmpty(b.selectionText) &&
                string.Equals(b.selectionText.Trim(), phrase, StringComparison.OrdinalIgnoreCase))
            {
                scoped.Add(b);
                continue;
            }
            if (!string.IsNullOrEmpty(b.promptPlaceholderName) &&
                PhraseMatchesPlaceholder(phrase, b.promptPlaceholderName))
                scoped.Add(b);
        }
        return scoped.Count > 0 ? scoped : clauseBindings;
    }

    public bool TryGetLemmaBool(string entryId, string key, out bool value)
    {
        value = false;
        if (string.IsNullOrEmpty(entryId) || string.IsNullOrEmpty(key) || lemmaProperties == null)
            return false;

        foreach (var p in lemmaProperties)
        {
            if (p == null)
                continue;
            if (string.Equals(p.entryId, entryId, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(p.propertyKey, key, StringComparison.OrdinalIgnoreCase) &&
                AnimationPlaybackPolicyResolver.TryParseBool(p.propertyValue, out value))
                return true;
        }
        return false;
    }

    public bool TryGetLemmaBoolForActivePhrase(string key, out bool value)
    {
        value = false;
        if (string.IsNullOrEmpty(key))
            return false;

        foreach (var binding in activePhraseBindings)
        {
            if (!string.IsNullOrEmpty(binding.phrase) &&
                !string.IsNullOrWhiteSpace(activePhrase) &&
                !string.Equals(binding.phrase.Trim(), activePhrase.Trim(), StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(binding.resolvedOrmKey) &&
                TryGetLemmaBool(binding.resolvedOrmKey, key, out value))
                return true;

            if (!string.IsNullOrEmpty(binding.builtInEntryId) &&
                TryGetLemmaBool(binding.builtInEntryId, key, out value))
                return true;
        }

        if (activeEventIndex >= 0)
        {
            foreach (var binding in activePhraseBindings)
            {
                if (binding.eventIndex != activeEventIndex)
                    continue;
                if (!string.IsNullOrEmpty(binding.resolvedOrmKey) &&
                    TryGetLemmaBool(binding.resolvedOrmKey, key, out value))
                    return true;
                if (!string.IsNullOrEmpty(binding.builtInEntryId) &&
                    TryGetLemmaBool(binding.builtInEntryId, key, out value))
                    return true;
            }
        }

        return false;
    }

    /// <summary>Resolve bool via prompt → clause → lemma properties → spec default.</summary>
    public bool GetEffectiveBool(string propertyKey, string specDefault = "false", bool skipPromptAndClause = false)
    {
        if (!skipPromptAndClause)
        {
            if (TryGetLemmaBoolForActivePhrase(propertyKey, out bool phraseLemma))
                return phraseLemma;
            return AnimationPlaybackPolicyResolver.ResolveEffectiveBool(
                propertyKey,
                GetSegmentsForActivePhrase(),
                GetBindingsForActivePhrase(),
                lemmaProperties,
                specDefault);
        }

        return AnimationPlaybackPolicyResolver.ResolveEffectiveBool(
            propertyKey,
            Array.Empty<PromptSegment>(),
            Array.Empty<LocalizationClauseBindingRecord>(),
            lemmaProperties,
            specDefault);
    }

    static bool PhraseMatchesPlaceholder(string phrase, string placeholderName)
    {
        if (string.IsNullOrEmpty(phrase) || string.IsNullOrEmpty(placeholderName))
            return false;
        return string.Equals(phrase.Trim(), placeholderName.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerable<PlaybackPhraseBinding> GetPhraseBindingsForEvent(int eventIndex)
    {
        if (activePhraseBindings == null)
            yield break;
        foreach (var b in activePhraseBindings)
        {
            if (b.eventIndex == eventIndex)
                yield return b;
        }
    }

    async void Start()
    {
        if (string.IsNullOrEmpty(draftEpisodeId))
            return;
        try
        {
            var client = ContinuuuumLocalizationServices.GetClient();
            var bindings = await client.GetClauseBindingsAsync(draftEpisodeId);
            SetClauseBindings(bindings);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[AnimationPlaybackPolicyContext] Failed to load clause bindings: {ex.Message}");
        }
    }
}
