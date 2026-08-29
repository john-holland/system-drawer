using System;
using System.Collections.Generic;

/// <summary>
/// Fast resolution of prompt/event titles to built-in descriptors (longest token-prefix match).
/// </summary>
public static class VocabularyBuiltInLookup
{
    private static Dictionary<string, VocabularyBuiltInDescriptor> _byLemma;
    private static readonly object Gate = new object();

    private static void EnsureInit()
    {
        lock (Gate)
        {
            if (_byLemma != null) return;
            var d = new Dictionary<string, VocabularyBuiltInDescriptor>(StringComparer.OrdinalIgnoreCase);
            foreach (var x in VocabularyBuiltInRegistry.All)
                d[x.Term] = x;
            _byLemma = d;
        }
    }

    /// <summary>Clear caches if registry ever becomes hot-reloadable (editor).</summary>
    public static void ResetCacheForTests()
    {
        lock (Gate)
        {
            _byLemma = null;
        }
    }

    /// <summary>
    /// Try resolve full phrase: longest-first joint token sequence (same strategy as ORM substring loop).
    /// Applies <see cref="BuiltInSynonyms.CanonicalizeToken"/> per token before lookup.
    /// </summary>
    public static bool TryResolvePhrase(string phrase, out VocabularyBuiltInDescriptor descriptor)
    {
        descriptor = default;
        if (string.IsNullOrWhiteSpace(phrase)) return false;
        EnsureInit();

        string[] raw = AdverbIfPostfix.ApplyToText(phrase);
        if (raw.Length == 0) return false;
        // Leading if (prefix or if-then circumfix) is always the conjunction.
        if (IfPredicate.TryClassify(raw, 0, out var ifPos)
            && (ifPos == IfOperatorPosition.Prefix || ifPos == IfOperatorPosition.Circumfix)
            && TryGetByLemmaExact("if", out descriptor))
            return true;

        for (int w = raw.Length; w >= 1; w--)
        {
            var slice = new string[w];
            Array.Copy(raw, 0, slice, 0, w);
            string multi = BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(slice);
            if (!string.IsNullOrEmpty(multi) && TryGetByLemma(multi, out descriptor))
                return true;
            string hyphen = string.Join("-", slice);
            if (TryGetByLemma(hyphen, out descriptor))
                return true;
            string sub = string.Join(" ", slice);
            if (_byLemma.TryGetValue(sub, out descriptor))
                return true;
        }
        return false;
    }

    /// <summary>Exact registry term only (no adverb-if composition). Used by <see cref="AdverbIfPostfix"/>.</summary>
    public static bool TryGetByLemmaExact(string lemma, out VocabularyBuiltInDescriptor descriptor)
    {
        EnsureInit();
        lemma = BuiltInSynonyms.CanonicalizeToken(lemma ?? "");
        return _byLemma.TryGetValue(lemma, out descriptor);
    }

    /// <summary>True if lemma exists as built-in term, or a greedy adverb+if / if-so composition.</summary>
    public static bool TryGetByLemma(string lemma, out VocabularyBuiltInDescriptor descriptor)
    {
        EnsureInit();
        lemma = BuiltInSynonyms.CanonicalizeToken(lemma ?? "");
        if (_byLemma.TryGetValue(lemma, out descriptor))
            return true;
        if (AdverbIfPostfix.TryStem(lemma, out string adverb, out string postfix))
        {
            string id = VocabularyLanguageEncoding.FormatBuiltInUrn("en", "adv", lemma);
            descriptor = new VocabularyBuiltInDescriptor(
                id, "en", lemma, "adverb", VocabularyBuiltInCategory.DiscourseCausality,
                new[] { "civil", "vote", "queue", postfix });
            return true;
        }
        return false;
    }
}
