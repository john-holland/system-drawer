using System;
using System.Collections.Generic;

/// <summary>Where <c>if</c> sits relative to its arguments.</summary>
public enum IfOperatorPosition
{
    /// <summary><c>if P</c> — no left argument (clause start or after a coordinator).</summary>
    Prefix = 0,
    /// <summary><c>P if Q</c> — left and right arguments.</summary>
    Infix = 1,
    /// <summary><c>P if</c> / <c>P if so</c> / adverb anaphor <c>happily, if so</c>.</summary>
    Postfix = 2,
    /// <summary><c>if P then Q</c> correlative (mixfix / circumfix).</summary>
    Circumfix = 3
}

/// <summary>One classified <c>if</c> (or composed <c>*-if-so</c>) in a token stream.</summary>
public readonly struct IfPredicateHit
{
    public int Index { get; }
    public IfOperatorPosition Position { get; }
    public bool Composed { get; }

    public IfPredicateHit(int index, IfOperatorPosition position, bool composed = false)
    {
        Index = index;
        Position = position;
        Composed = composed;
    }
}

/// <summary>
/// Prefix / infix / postfix / circumfix classification for the <c>if</c> predicate.
/// Postfix anaphor <c>if so</c> after an adverb still composes via <see cref="AdverbIfPostfix"/>.
/// </summary>
public static class IfPredicate
{
    static readonly HashSet<string> LeftEdge =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and", "or", "nor", "but", "yet", "so", "then", "else",
            "because", "when", "while", "although", "unless"
        };

    public static bool IsIf(string token)
    {
        return !string.IsNullOrEmpty(token)
               && string.Equals(token, "if", StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryClassify(string[] tokens, int ifIndex, out IfOperatorPosition position)
    {
        position = IfOperatorPosition.Prefix;
        if (tokens == null || ifIndex < 0 || ifIndex >= tokens.Length)
            return false;
        string tok = tokens[ifIndex];
        if (IsComposedIfSo(tok))
        {
            position = IfOperatorPosition.Postfix;
            return true;
        }
        if (!IsIf(tok))
            return false;
        position = Classify(tokens, ifIndex);
        return true;
    }

    public static IfOperatorPosition Classify(string[] tokens, int ifIndex)
    {
        if (tokens == null || ifIndex < 0 || ifIndex >= tokens.Length || !IsIf(tokens[ifIndex]))
            return IfOperatorPosition.Prefix;

        bool hasLeft = ifIndex > 0 && !LeftEdge.Contains(tokens[ifIndex - 1]);
        bool hasRight = ifIndex + 1 < tokens.Length;
        if (!hasRight)
            return hasLeft ? IfOperatorPosition.Postfix : IfOperatorPosition.Prefix;
        if (HasThenCorrelative(tokens, ifIndex))
            return IfOperatorPosition.Circumfix;
        bool adverbAnaphor = hasLeft
                             && AdverbIfPostfix.LooksLikeAdverb(tokens[ifIndex - 1])
                             && string.Equals(tokens[ifIndex + 1], "so", StringComparison.OrdinalIgnoreCase);
        if (adverbAnaphor)
            return IfOperatorPosition.Postfix;
        if (hasLeft && IsAnaphorSo(tokens, ifIndex))
            return IfOperatorPosition.Postfix;
        if (!hasLeft)
            return IfOperatorPosition.Prefix;
        return IfOperatorPosition.Infix;
    }

    public static IfPredicateHit[] FindAll(string[] tokens)
    {
        if (tokens == null || tokens.Length == 0)
            return Array.Empty<IfPredicateHit>();
        var hits = new List<IfPredicateHit>(2);
        for (int i = 0; i < tokens.Length; i++)
        {
            if (IsComposedIfSo(tokens[i]))
            {
                hits.Add(new IfPredicateHit(i, IfOperatorPosition.Postfix, true));
                continue;
            }
            if (IsIf(tokens[i]))
                hits.Add(new IfPredicateHit(i, Classify(tokens, i), false));
        }
        return hits.ToArray();
    }

    public static IfPredicateHit[] FindAllInText(string text) =>
        FindAll(AdverbIfPostfix.ApplyToText(text));

    static bool IsAnaphorSo(string[] tokens, int ifIndex)
    {
        if (ifIndex + 1 >= tokens.Length
            || !string.Equals(tokens[ifIndex + 1], "so", StringComparison.OrdinalIgnoreCase))
            return false;
        if (ifIndex + 2 >= tokens.Length)
            return true;
        return LeftEdge.Contains(tokens[ifIndex + 2]);
    }

    static bool HasThenCorrelative(string[] tokens, int ifIndex)
    {
        for (int i = ifIndex + 1; i < tokens.Length; i++)
        {
            if (IsIf(tokens[i])) return false;
            if (string.Equals(tokens[i], "then", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    static bool IsComposedIfSo(string token)
    {
        return !string.IsNullOrEmpty(token)
               && token.EndsWith(AdverbIfPostfix.IfSoSuffix, StringComparison.OrdinalIgnoreCase);
    }
}
