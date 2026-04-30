using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// MVP name/string similarity for lie policy (no embeddings): overlaps token sets + short Levenshtein on labels.
/// </summary>
public static class ThoughtSimilarityMvp
{
    private const int MaxLevenshteinLen = 48;

    /// <summary>
    /// Collect semantic strings from decision payload + receiver context for blending with LSTM score.
    /// </summary>
    public static float ScoreNameOverlap(string[] incomingTags, Brain receiver, ThoughtData incoming)
    {
        var a = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in incomingTags ?? Array.Empty<string>())
            if (!string.IsNullOrWhiteSpace(t))
                foreach (var tok in Tokenize(t))
                    a.Add(tok);

        ReflectPayloadNames(incoming?.data, a);

        var b = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (receiver != null)
        {
            if (receiver.behaviorTree != null && receiver.behaviorTree.currentGoal != null)
            {
                var g = receiver.behaviorTree.currentGoal;
                Tokenize(g.goalName, b);
                if (g.target != null)
                    Tokenize(g.target.name, b);
            }

            if (receiver.impulseFilters != null)
            {
                for (int i = 0; i < receiver.impulseFilters.Count; i++)
                {
                    var f = receiver.impulseFilters[i];
                    if (f != null)
                        Tokenize(f.GetType().Name, b);
                }
            }

            Tokenize(receiver.behaviorTree != null ? receiver.behaviorTree.GetType().Name : "", b);
        }

        if (a.Count == 0 || b.Count == 0)
            return 0.5f;

        int inter = a.Intersect(b).Count();
        float jaccard = inter / (float)(a.Union(b).Count());
        return Mathf.Clamp01(jaccard + 0.25f * Mathf.Min(1f, inter));
    }

    private static void ReflectPayloadNames(object data, HashSet<string> sink)
    {
        if (data == null) return;
        var t = data.GetType();
        foreach (var fi in t.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
        {
            if (fi.FieldType == typeof(string))
            {
                var s = fi.GetValue(data) as string;
                Tokenize(s, sink);
            }
            Tokenize(fi.Name, sink);
        }
    }

    private static void Tokenize(string s, HashSet<string> sink)
    {
        foreach (var tok in Tokenize(s))
            sink.Add(tok);
    }

    private static IEnumerable<string> Tokenize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) yield break;
        var sb = new StringBuilder();
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }
        if (sb.Length > 0)
            yield return sb.ToString();
    }

    /// <summary>
    /// Normalized Levenshtein similarity 0–1 for short strings.
    /// </summary>
    public static float LevenshteinSimilarity(string x, string y)
    {
        if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y))
            return 0f;
        x = x.Length > MaxLevenshteinLen ? x.Substring(0, MaxLevenshteinLen) : x;
        y = y.Length > MaxLevenshteinLen ? y.Substring(0, MaxLevenshteinLen) : y;
        int d = LevenshteinDistance(x.ToLowerInvariant(), y.ToLowerInvariant());
        int maxLen = Mathf.Max(x.Length, y.Length);
        return maxLen == 0 ? 1f : 1f - d / (float)maxLen;
    }

    private static int LevenshteinDistance(string a, string b)
    {
        int n = a.Length, m = b.Length;
        var row = new int[m + 1];
        for (int j = 0; j <= m; j++) row[j] = j;
        for (int i = 1; i <= n; i++)
        {
            int prev = row[0];
            row[0] = i;
            for (int j = 1; j <= m; j++)
            {
                int cur = row[j];
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                row[j] = Mathf.Min(Mathf.Min(row[j] + 1, row[j - 1] + 1), prev + cost);
                prev = cur;
            }
        }
        return row[m];
    }
}
