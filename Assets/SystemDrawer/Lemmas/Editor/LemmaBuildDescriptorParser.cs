#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Validates composition child entry IDs against built-in vocabulary.</summary>
public static class LemmaBuildBuiltinValidator
{
    static readonly Dictionary<string, string> AliasSuggestions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "subject", VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "player") },
        { "object", VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "object") },
        { "player", VocabularyLanguageEncoding.FormatBuiltInUrn("en", "noun", "player") },
    };

    public static bool IsKnownEntryId(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return false;
        if (VocabularyBuiltInRegistry.TryGetById(entryId) != null)
            return true;
        return AliasSuggestions.ContainsKey(entryId.Trim());
    }

    public static string ResolveEntryId(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId))
            return entryId;
        var trimmed = entryId.Trim();
        if (VocabularyBuiltInRegistry.TryGetById(trimmed) != null)
            return trimmed;
        if (AliasSuggestions.TryGetValue(trimmed, out var urn))
            return urn;
        return trimmed;
    }

    public static string[] ValidateCompositionEntryIds(LemmaCompositionChildPutDto[] children)
    {
        if (children == null || children.Length == 0)
            return Array.Empty<string>();
        var warnings = new List<string>();
        foreach (var child in children)
        {
            if (child == null || string.IsNullOrWhiteSpace(child.entryId))
                continue;
            var trimmed = child.entryId.Trim();
            if (VocabularyBuiltInRegistry.TryGetById(trimmed) != null)
                continue;
            if (AliasSuggestions.TryGetValue(trimmed, out var urn))
            {
                warnings.Add($"Bare alias '{trimmed}' — use builtin URN '{urn}'.");
                continue;
            }
            var suggestion = SuggestBuiltin(trimmed);
            warnings.Add(string.IsNullOrEmpty(suggestion)
                ? $"Unknown entryId '{trimmed}' — not in VocabularyBuiltInRegistry."
                : $"Unknown entryId '{trimmed}' — consider '{suggestion}'.");
        }
        return warnings.ToArray();
    }

    static string SuggestBuiltin(string entryId)
    {
        if (AliasSuggestions.TryGetValue(entryId.Trim(), out var urn))
            return urn;
        var lower = entryId.Trim().ToLowerInvariant();
        foreach (var d in VocabularyBuiltInRegistry.All)
        {
            if (d.Term.Equals(lower, StringComparison.OrdinalIgnoreCase))
                return d.Id;
        }
        return null;
    }
}

/// <summary>Parses LemmaMechanismDescriptor JSON from assistant chat output.</summary>
public static class LemmaBuildDescriptorParser
{
    public const string FenceTag = "lemma-mechanism-descriptor";

    static readonly Regex FenceRegex = new Regex(
        @"```(?:json\s+)?" + FenceTag + @"\s*([\s\S]*?)```",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool TryParseFromAssistantText(string assistantText, out LemmaMechanismDescriptor descriptor)
    {
        descriptor = null;
        if (string.IsNullOrWhiteSpace(assistantText))
            return false;

        var fenceMatch = FenceRegex.Match(assistantText);
        if (fenceMatch.Success)
            return TryParseJson(fenceMatch.Groups[1].Value.Trim(), out descriptor);

        var genericFence = Regex.Match(
            assistantText,
            @"```json\s*([\s\S]*?)```",
            RegexOptions.IgnoreCase);
        if (genericFence.Success && TryParseJson(genericFence.Groups[1].Value.Trim(), out descriptor))
            return true;

        var braceStart = assistantText.IndexOf('{');
        var braceEnd = assistantText.LastIndexOf('}');
        if (braceStart >= 0 && braceEnd > braceStart)
            return TryParseJson(assistantText.Substring(braceStart, braceEnd - braceStart + 1), out descriptor);

        return false;
    }

    public static bool TryParseJson(string json, out LemmaMechanismDescriptor descriptor)
    {
        descriptor = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            var parsed = JsonUtility.FromJson<LemmaMechanismDescriptor>(json);
            if (parsed == null || string.IsNullOrEmpty(parsed.lemma))
                return false;
            if (string.IsNullOrEmpty(parsed.posTag) || string.IsNullOrEmpty(parsed.mechanicalRole))
                return false;
            descriptor = parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool HasRequiredFields(LemmaMechanismDescriptor descriptor)
    {
        return descriptor != null
               && !string.IsNullOrWhiteSpace(descriptor.lemma)
               && !string.IsNullOrWhiteSpace(descriptor.posTag)
               && !string.IsNullOrWhiteSpace(descriptor.mechanicalRole);
    }
}
#endif
