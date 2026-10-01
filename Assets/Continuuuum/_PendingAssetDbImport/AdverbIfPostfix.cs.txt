using System;
using System.Collections.Generic;

/// <summary>
/// Compose postfix anaphor <c>if so</c> onto a preceding adverb
/// (<c>randomly, if so</c> → <c>randomly-if-so</c>). Prefix / infix / circumfix <c>if</c>
/// stay tokens; see <see cref="IfPredicate"/>.
/// </summary>
public static class AdverbIfPostfix
{
    public const string IfSo = "if-so";
    public const string IfSuffix = "-if";
    public const string IfSoSuffix = "-if-so";

    public static bool LooksLikeAdverb(string token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        string t = token.Trim().ToLowerInvariant();
        if (t.EndsWith("ly", StringComparison.Ordinal)) return true;
        return VocabularyBuiltInLookup.TryGetByLemmaExact(t, out var d)
               && string.Equals(d.PosTag, "adverb", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>True when <c>if</c> at <paramref name="ifIndex"/> is the prefix predicate.</summary>
    public static bool IsPrefixIf(string[] tokens, int ifIndex)
    {
        return IfPredicate.TryClassify(tokens, ifIndex, out var pos)
               && pos == IfOperatorPosition.Prefix;
    }

    public static string[] ApplyToText(string text)
    {
        string[] raw = VocabularyBuiltInTokenizer.TokenizeText(text);
        for (int i = 0; i < raw.Length; i++)
            raw[i] = BuiltInSynonyms.CanonicalizeToken(raw[i]);
        return Apply(raw);
    }

    public static string[] Apply(string[] tokens)
    {
        if (tokens == null || tokens.Length == 0) return Array.Empty<string>();
        var outList = new List<string>(tokens.Length);
        for (int i = 0; i < tokens.Length; i++)
        {
            string tok = tokens[i];
            if (LooksLikeAdverb(tok) && i + 1 < tokens.Length
                && IfPredicate.TryClassify(tokens, i + 1, out var pos)
                && pos == IfOperatorPosition.Postfix
                && string.Equals(tokens[i + 1], "if", StringComparison.OrdinalIgnoreCase)
                && i + 2 < tokens.Length
                && string.Equals(tokens[i + 2], "so", StringComparison.OrdinalIgnoreCase))
            {
                outList.Add(tok.Trim().ToLowerInvariant() + IfSoSuffix);
                i += 2;
                continue;
            }
            outList.Add(tok);
        }
        return outList.ToArray();
    }

    public static bool TryStem(string lemma, out string adverb, out string postfix)
    {
        adverb = null;
        postfix = null;
        if (string.IsNullOrEmpty(lemma)) return false;
        string t = lemma.Trim().ToLowerInvariant();
        if (t.EndsWith(IfSoSuffix, StringComparison.Ordinal) && t.Length > IfSoSuffix.Length)
        {
            adverb = t.Substring(0, t.Length - IfSoSuffix.Length);
            postfix = IfSo;
            return IsSingleAdverb(adverb);
        }
        if (t.EndsWith(IfSuffix, StringComparison.Ordinal) && t.Length > IfSuffix.Length)
        {
            adverb = t.Substring(0, t.Length - IfSuffix.Length);
            postfix = "if";
            return IsSingleAdverb(adverb);
        }
        return false;
    }

    static bool IsSingleAdverb(string adverb)
    {
        if (!LooksLikeAdverb(adverb)) return false;
        if (adverb.IndexOf('-') < 0) return true;
        return VocabularyBuiltInLookup.TryGetByLemmaExact(adverb, out _);
    }
}
