using System;
using System.Collections.Generic;

/// <summary>
/// Register built-in vocabulary locally and merge API/DB rows to enrich definitions without
/// replacing built-in identity (id, term, pos for built-ins).
/// </summary>
public static class VocabularyBuiltInRegistrar
{
    /// <summary>Build a dictionary keyed by entry id (built-in URNs and any API ids after merge).</summary>
    public static Dictionary<string, VocabularyThesaurusEntryView> RegisterLocal()
    {
        var map = new Dictionary<string, VocabularyThesaurusEntryView>(StringComparer.Ordinal);
        foreach (var d in VocabularyBuiltInRegistry.All)
        {
            var v = new VocabularyThesaurusEntryView
            {
                Id = d.Id,
                Term = d.Term,
                PosTag = d.PosTag,
                LanguageCode = d.LanguageCode,
                BuiltInCategory = d.Category,
                Tags = new List<string>(d.Tags)
            };
            map[d.Id] = v;
        }
        return map;
    }

    /// <summary>
    /// Merge API entries: match by <see cref="VocabularyApiThesaurusEntry.Id"/>, then by
    /// (languageCode, term, pos). Enrichment fields only; built-in id/term/pos are not replaced.
    /// </summary>
    public static Dictionary<string, VocabularyThesaurusEntryView> MergeApiEnrichment(
        IReadOnlyDictionary<string, VocabularyThesaurusEntryView> local,
        IEnumerable<VocabularyApiThesaurusEntry> apiEntries,
        StringComparer idComparer = null)
    {
        idComparer = idComparer ?? StringComparer.Ordinal;
        var result = new Dictionary<string, VocabularyThesaurusEntryView>(idComparer);
        foreach (var kv in local)
            result[kv.Key] = CloneView(kv.Value);

        if (apiEntries == null)
            return result;

        foreach (var api in apiEntries)
        {
            if (api == null)
                continue;

            VocabularyThesaurusEntryView target = null;

            if (!string.IsNullOrEmpty(api.Id) && result.TryGetValue(api.Id, out var byId))
                target = byId;
            else
            {
                string lc = VocabularyLanguageEncoding.NormalizeLanguageCode(
                    !string.IsNullOrEmpty(api.LanguageCode) ? api.LanguageCode : "en");
                foreach (var kv in result)
                {
                    var e = kv.Value;
                    if (e.Term == api.Term && e.PosTag == api.PosTag && e.LanguageCode == lc)
                    {
                        target = e;
                        break;
                    }
                }
            }

            if (target != null)
            {
                if (!string.IsNullOrEmpty(api.Definition))
                    target.Definition = api.Definition;
                if (!string.IsNullOrEmpty(api.Version))
                    target.Version = api.Version;
                if (api.Alternatives != null && api.Alternatives.Length > 0)
                {
                    if (target.Alternatives == null)
                        target.Alternatives = new List<string>();
                    for (int i = 0; i < api.Alternatives.Length; i++)
                    {
                        string a = api.Alternatives[i];
                        if (!string.IsNullOrEmpty(a) && !target.Alternatives.Contains(a))
                            target.Alternatives.Add(a);
                    }
                }
                if (!string.IsNullOrEmpty(api.LanguageId))
                    target.LanguageId = api.LanguageId;
            }
            else if (!string.IsNullOrEmpty(api.Id) && !result.ContainsKey(api.Id))
            {
                var v = new VocabularyThesaurusEntryView
                {
                    Id = api.Id,
                    Term = api.Term,
                    PosTag = api.PosTag,
                    LanguageCode = VocabularyLanguageEncoding.NormalizeLanguageCode(api.LanguageCode ?? "en"),
                    LanguageId = api.LanguageId,
                    Definition = api.Definition,
                    Version = api.Version,
                    BuiltInCategory = null
                };
                if (api.Alternatives != null)
                    v.Alternatives = new List<string>(api.Alternatives);
                result[api.Id] = v;
            }
        }

        return result;
    }

    private static VocabularyThesaurusEntryView CloneView(VocabularyThesaurusEntryView s)
    {
        return new VocabularyThesaurusEntryView
        {
            Id = s.Id,
            Term = s.Term,
            PosTag = s.PosTag,
            LanguageCode = s.LanguageCode,
            LanguageId = s.LanguageId,
            Definition = s.Definition,
            Version = s.Version,
            BuiltInCategory = s.BuiltInCategory,
            Tags = s.Tags != null ? new List<string>(s.Tags) : null,
            Alternatives = s.Alternatives != null ? new List<string>(s.Alternatives) : null
        };
    }
}
