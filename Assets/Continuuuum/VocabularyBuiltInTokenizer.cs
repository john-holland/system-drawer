using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// Word tokenizer aligned with <c>NarrativeLSTMTokenizer.TokenizeText</c> (lowercase + alphanumeric regex chunks).
/// Lives in Continuuuum.Runtime to avoid asmdef cycles with Locomotion.Narrative.Serialization.
/// </summary>
public static class VocabularyBuiltInTokenizer
{
    private static readonly Regex WordRegex = new Regex(@"[a-z0-9]+", RegexOptions.Compiled);

    /// <summary>Same contract as NarrativeLSTMTokenizer.TokenizeText.</summary>
    public static string[] TokenizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return Array.Empty<string>();
        text = text.ToLowerInvariant().Trim();
        var matches = WordRegex.Matches(text);
        var list = new List<string>(matches.Count);
        foreach (Match m in matches)
            if (m.Success) list.Add(m.Value);
        return list.ToArray();
    }
}
