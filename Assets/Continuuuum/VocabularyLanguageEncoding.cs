using System;
using System.Text;

/// <summary>
/// Built-in vocabulary URNs and language-code conventions. Server ETL must not mint IDs with
/// <see cref="BuiltInUrnPrefix"/> (operational rule; matches continuuuum <c>thesaurus_entries.id</c> TEXT PK).
/// </summary>
public static class VocabularyLanguageEncoding
{
    /// <summary>Operational rule: continuuuum ETL / API must not emit primary keys with this prefix.</summary>
    public const string BuiltInUrnPrefix = "urn:unity:continuuuum:builtin:v1:";

    public const string DefaultLanguageCode = "en";

    /// <summary>Normalize BCP-47-ish codes for URNs (default <c>en</c>).</summary>
    public static string NormalizeLanguageCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return DefaultLanguageCode;
        return code.Trim().ToLowerInvariant().Replace('_', '-');
    }

    /// <summary>
    /// Mint a stable built-in URN: <c>{prefix}/{language}/{segment}/{lemmaSlug}</c>.
    /// Segment is typically POS- or kind-like: <c>det</c>, <c>prep</c>, <c>conj</c>, <c>noun</c>, <c>verb</c>, <c>literal</c>.
    /// </summary>
    public static string FormatBuiltInUrn(string languageCode, string segment, string lemma)
    {
        string lang = NormalizeLanguageCode(languageCode);
        string seg = SlugSegment(segment);
        string term = SlugSegment(lemma);
        return BuiltInUrnPrefix + "/" + lang + "/" + seg + "/" + term;
    }

    /// <summary>Returns true if <paramref name="id"/> is a Unity built-in thesaurus URN.</summary>
    public static bool IsBuiltInUrn(string id)
    {
        return !string.IsNullOrEmpty(id) &&
               id.StartsWith(BuiltInUrnPrefix, StringComparison.Ordinal);
    }

    /// <summary>
    /// Resolve <paramref name="languageCode"/> to continuuuum <c>languages.id</c> using caller-supplied lookup
    /// (e.g. <c>SELECT id FROM languages WHERE code = ?</c>). Same contract as continuuuum_api server.
    /// </summary>
    public static bool TryResolveLanguageId(Func<string, string> tryCodeToId, string languageCode, out string languageId)
    {
        languageId = null;
        if (tryCodeToId == null)
            return false;
        string code = NormalizeLanguageCode(languageCode);
        string id = tryCodeToId(code);
        if (string.IsNullOrEmpty(id))
            return false;
        languageId = id;
        return true;
    }

    private static string SlugSegment(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "_";
        var sb = new StringBuilder(s.Length);
        bool lastUnderscore = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(char.ToLowerInvariant(c));
                lastUnderscore = false;
            }
            else if (c == '_' || c == '-' || char.IsWhiteSpace(c))
            {
                if (!lastUnderscore && sb.Length > 0)
                {
                    sb.Append('_');
                    lastUnderscore = true;
                }
            }
        }
        string r = sb.ToString().Trim('_');
        return string.IsNullOrEmpty(r) ? "_" : r;
    }
}
