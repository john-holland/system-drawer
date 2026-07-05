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

        string[] raw = VocabularyBuiltInTokenizer.TokenizeText(phrase);
        if (raw.Length == 0) return false;
        for (int i = 0; i < raw.Length; i++)
            raw[i] = BuiltInSynonyms.CanonicalizeToken(raw[i]);

        for (int w = raw.Length; w >= 1; w--)
        {
            string sub = string.Join(" ", raw, 0, w);
            if (_byLemma.TryGetValue(sub, out descriptor))
                return true;
        }
        return false;
    }

    /// <summary>True if lemma exists as built-in term.</summary>
    public static bool TryGetByLemma(string lemma, out VocabularyBuiltInDescriptor descriptor)
    {
        EnsureInit();
        lemma = BuiltInSynonyms.CanonicalizeToken(lemma ?? "");
        return _byLemma.TryGetValue(lemma, out descriptor);
    }
}
