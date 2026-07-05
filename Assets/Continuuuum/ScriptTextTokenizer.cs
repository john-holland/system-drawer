using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>Tokenizes script text (whitespace words + atomic {P:...} placeholders).</summary>
public static class ScriptTextTokenizer
{
    static readonly Regex PlaceholderRe = new Regex(@"\{\{?P:[^}]+\}?\}?|\{P:[^}]+\}", RegexOptions.Compiled);

    public sealed class Token
    {
        public int index;
        public string text;
        public int charStart;
        public int charEnd;
        public bool isPlaceholder;
        public string placeholderName;
    }

    public sealed class KaraokeWindow
    {
        public List<Token> before = new List<Token>();
        public Token current;
        public List<Token> after = new List<Token>();
        public bool hasMoreBefore;
        public bool hasMoreAfter;
    }

    public static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        if (string.IsNullOrEmpty(text))
            return tokens;

        int cursor = 0;
        int tokenIndex = 0;

        while (cursor < text.Length)
        {
            Match ph = FindPlaceholderAt(text, cursor);
            int litEnd = ph != null ? ph.Index : text.Length;

            if (litEnd > cursor)
            {
                string literal = text.Substring(cursor, litEnd - cursor);
                int local = 0;
                while (local < literal.Length)
                {
                    while (local < literal.Length && char.IsWhiteSpace(literal[local]))
                        local++;
                    if (local >= literal.Length)
                        break;
                    int wordEnd = local;
                    while (wordEnd < literal.Length && !char.IsWhiteSpace(literal[wordEnd]))
                        wordEnd++;
                    tokens.Add(new Token
                    {
                        index = tokenIndex++,
                        text = literal.Substring(local, wordEnd - local),
                        charStart = cursor + local,
                        charEnd = cursor + wordEnd,
                        isPlaceholder = false,
                    });
                    local = wordEnd;
                }
            }

            if (ph != null)
            {
                tokens.Add(new Token
                {
                    index = tokenIndex++,
                    text = ph.Value,
                    charStart = ph.Index,
                    charEnd = ph.Index + ph.Length,
                    isPlaceholder = true,
                    placeholderName = ExtractPlaceholderName(ph.Value),
                });
                cursor = ph.Index + ph.Length;
            }
            else
            {
                break;
            }
        }

        return tokens;
    }

    static Match FindPlaceholderAt(string text, int from)
    {
        Match m = PlaceholderRe.Match(text, from);
        return m.Success && m.Index >= from ? m : null;
    }

    public static string ExtractPlaceholderName(string span)
    {
        if (string.IsNullOrEmpty(span))
            return "";
        int start = span.IndexOf("{P:", StringComparison.Ordinal);
        if (start < 0)
            start = span.IndexOf("{{P:", StringComparison.Ordinal);
        if (start < 0)
            return "";
        start = span.IndexOf('P', start) + 2;
        int end = span.IndexOf('|', start);
        if (end < 0)
            end = span.IndexOf('}', start);
        if (end <= start)
            return "";
        return span.Substring(start, end - start).Trim();
    }

    public static int WordIndexAtChar(IReadOnlyList<Token> tokens, int charOffset)
    {
        if (tokens == null || tokens.Count == 0)
            return 0;
        if (charOffset <= tokens[0].charStart)
            return 0;
        for (int i = 0; i < tokens.Count; i++)
        {
            Token t = tokens[i];
            if (charOffset >= t.charStart && charOffset < t.charEnd)
                return i;
            if (charOffset < t.charStart)
                return Math.Max(0, i - 1);
        }
        return tokens.Count - 1;
    }

    public static int ResolveWordIndex(IReadOnlyList<Token> tokens, string activePhrase, int activeEventIndex)
    {
        if (tokens == null || tokens.Count == 0)
            return 0;

        string phrase = (activePhrase ?? "").Trim();
        if (string.IsNullOrEmpty(phrase))
            return Math.Max(0, Math.Min(activeEventIndex, tokens.Count - 1));

        var matches = new List<int>();
        for (int i = 0; i < tokens.Count; i++)
        {
            Token t = tokens[i];
            if (string.Equals(t.text, phrase, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(i);
                continue;
            }
            if (t.isPlaceholder && string.Equals(t.placeholderName, phrase, StringComparison.OrdinalIgnoreCase))
                matches.Add(i);
        }

        if (matches.Count == 0)
            return 0;
        if (activeEventIndex >= 0 && activeEventIndex < matches.Count)
            return matches[activeEventIndex];
        return matches[0];
    }

    public static KaraokeWindow Window(IReadOnlyList<Token> tokens, int index, int radius = 5)
    {
        var win = new KaraokeWindow();
        if (tokens == null || tokens.Count == 0)
            return win;

        index = Math.Max(0, Math.Min(index, tokens.Count - 1));
        int start = Math.Max(0, index - radius);
        int end = Math.Min(tokens.Count - 1, index + radius);

        for (int i = start; i < index; i++)
            win.before.Add(tokens[i]);
        win.current = tokens[index];
        for (int i = index + 1; i <= end; i++)
            win.after.Add(tokens[i]);
        win.hasMoreBefore = start > 0;
        win.hasMoreAfter = end < tokens.Count - 1;
        return win;
    }
}
