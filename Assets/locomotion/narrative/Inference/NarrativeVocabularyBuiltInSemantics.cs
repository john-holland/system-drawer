using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Built-in vocabulary semantics for narrative prompts: spatial time-window hints and typed literal spans.
    /// </summary>
    public static class NarrativeVocabularyBuiltInSemantics
    {
        private static string LiteralTypePattern()
        {
            var sb = new StringBuilder();
            foreach (var d in VocabularyBuiltInRegistry.All)
            {
                if (d.Category != VocabularyBuiltInCategory.LiteralType) continue;
                if (sb.Length > 0) sb.Append('|');
                sb.Append(Regex.Escape(d.Term));
            }
            return sb.Length > 0 ? sb.ToString() : "string";
        }

        /// <summary>
        /// Parse <c>name:type</c> pairs using literal lemmas from <see cref="VocabularyBuiltInRegistry"/>.
        /// </summary>
        public static List<(string name, string typeLemma)> ParseTypedLiterals(string text)
        {
            var list = new List<(string, string)>();
            if (string.IsNullOrEmpty(text)) return list;
            string alt = LiteralTypePattern();
            var rx = new Regex(@"\b(\w+)\s*:\s*(" + alt + @")\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            foreach (Match m in rx.Matches(text))
            {
                if (!m.Success) continue;
                list.Add((m.Groups[1].Value, m.Groups[2].Value.ToLowerInvariant()));
            }
            return list;
        }

        /// <summary>
        /// Expand or shift narrative time window when gateway tokens appear in the event title (data-driven heuristics).
        /// </summary>
        public static void ApplySpatialGatewayHints(IList<InterpretedEvent> events, float weekSeconds = 0f)
        {
            if (events == null || events.Count == 0) return;
            if (weekSeconds <= 0f)
                weekSeconds = 86400f * 7f;

            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                string[] tok = VocabularyBuiltInTokenizer.TokenizeText(ev.title ?? "");
                bool pause = tok.Contains("pause");
                bool forward = tok.Contains("forward");
                bool back = tok.Contains("back");

                float tMin = ev.tMin;
                float tMax = ev.tMax;

                if (pause)
                {
                    float span = Mathf.Max(3600f, (tMax - tMin) * 1.25f);
                    float mid = (tMin + tMax) * 0.5f;
                    tMin = Mathf.Max(0f, mid - span * 0.5f);
                    tMax = Mathf.Min(weekSeconds, mid + span * 0.5f);
                    if (tMax <= tMin) tMax = tMin + 3600f;
                }
                else if (forward)
                    tMax = Mathf.Min(weekSeconds, tMax + Mathf.Max(60f, (tMax - tMin) * 0.15f));
                else if (back)
                    tMin = Mathf.Max(0f, tMin - Mathf.Max(60f, (tMax - tMin) * 0.15f));

                ev.tMin = tMin;
                ev.tMax = tMax;
                events[i] = ev;
            }
        }

        /// <summary>
        /// Rough clause splits on leading discourse tokens for logging / downstream AST (best-effort).
        /// </summary>
        public static List<string> SegmentClauses(string text)
        {
            var clauses = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return clauses;
            string[] words = VocabularyBuiltInTokenizer.TokenizeText(text);
            if (words.Length == 0) return clauses;
            // Includes NSM causality/temporal discourse primes (if/because/not/when/before/after/maybe/can).
            var conj = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "if", "then", "else", "but", "because", "when", "while", "although", "unless",
                "and", "or", "nor", "yet", "so", "before", "after", "not", "maybe", "can"
            };
            var cur = new List<string>();
            foreach (var w in words)
            {
                if (conj.Contains(w) && cur.Count > 0)
                {
                    clauses.Add(string.Join(" ", cur));
                    cur.Clear();
                }
                cur.Add(w);
            }
            if (cur.Count > 0)
                clauses.Add(string.Join(" ", cur));
            return clauses;
        }
    }
}
