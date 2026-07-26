using System;
using System.Collections.Generic;

namespace Locomotion.Narrative
{
    /// <summary>Compound spatial lemmas and relation token normalization for layout parsing.</summary>
    public static class WithLemmaRegistry
    {
        static readonly Dictionary<string, LayoutSpatialRelation> RelationByLemma =
            new Dictionary<string, LayoutSpatialRelation>(StringComparer.OrdinalIgnoreCase)
            {
                { "with", LayoutSpatialRelation.With },
                { "left-of", LayoutSpatialRelation.LeftOf },
                { "to-the-left-of", LayoutSpatialRelation.LeftOf },
                { "to the left of", LayoutSpatialRelation.LeftOf },
                { "right-of", LayoutSpatialRelation.RightOf },
                { "to-the-right-of", LayoutSpatialRelation.RightOf },
                { "to the right of", LayoutSpatialRelation.RightOf },
                { "in-front-of", LayoutSpatialRelation.ForwardOf },
                { "in front of", LayoutSpatialRelation.ForwardOf },
                { "forward-of", LayoutSpatialRelation.ForwardOf },
                { "behind", LayoutSpatialRelation.Behind },
                { "through", LayoutSpatialRelation.Through },
                { "through-there", LayoutSpatialRelation.Through },
                { "through there", LayoutSpatialRelation.Through },
                { "near", LayoutSpatialRelation.Near },
                { "along", LayoutSpatialRelation.Along },
                { "along-the-road", LayoutSpatialRelation.Along },
                { "along the road", LayoutSpatialRelation.Along },
                { "inside", LayoutSpatialRelation.Inside },
                { "outside", LayoutSpatialRelation.Outside },
                { "there", LayoutSpatialRelation.Through },
                { "here", LayoutSpatialRelation.Near },
                { "over-here", LayoutSpatialRelation.Near },
                { "over here", LayoutSpatialRelation.Near },
                { "where", LayoutSpatialRelation.Near },
                { "above", LayoutSpatialRelation.Above },
                { "below", LayoutSpatialRelation.Below },
                { "far", LayoutSpatialRelation.Far },
                { "side", LayoutSpatialRelation.Side },
                { "crash-through", LayoutSpatialRelation.Through },
                { "crash through", LayoutSpatialRelation.Through },
                { "wall-run", LayoutSpatialRelation.Along },
                { "scale", LayoutSpatialRelation.Along }
            };

        public static void RegisterBuiltInSynonyms()
        {
            BuiltInSynonyms.RegisterAlias("to the left of", "to-the-left-of");
            BuiltInSynonyms.RegisterAlias("to the right of", "to-the-right-of");
            BuiltInSynonyms.RegisterAlias("in front of", "in-front-of");
            BuiltInSynonyms.RegisterAlias("through there", "through-there");
            BuiltInSynonyms.RegisterAlias("over here", "over-here");
            BuiltInSynonyms.RegisterAlias("along the road", "along-the-road");
            BuiltInSynonyms.RegisterAlias("here here", "here-here");
            BuiltInSynonyms.RegisterAlias("there there", "there-there");
            BuiltInSynonyms.RegisterAlias("crash through", "crash-through");
            BuiltInSynonyms.RegisterAlias("wall run", "wall-run");
        }

        public static string CanonicalizeDeictic(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return token;
            return BuiltInSynonyms.CanonicalizeToken(token.Trim());
        }

        public static bool TryMergeDeicticAnchor(string current, string next, out string merged)
        {
            merged = null;
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(next))
                return false;
            string a = CanonicalizeDeictic(current);
            string b = CanonicalizeDeictic(next);
            if (a != b)
                return false;
            merged = a + "-" + b;
            return true;
        }

        public static bool TryParseDeicticPhrase(string[] tokens, int start, out string deictic, out int consumed)
        {
            deictic = null;
            consumed = 0;
            if (tokens == null || start >= tokens.Length)
                return false;

            for (int len = Math.Min(4, tokens.Length - start); len >= 1; len--)
            {
                var slice = new string[len];
                Array.Copy(tokens, start, slice, 0, len);
                string multi = BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(slice);
                if (!string.IsNullOrEmpty(multi) && IsDeictic(multi))
                {
                    deictic = multi;
                    consumed = len;
                    return true;
                }
                if (len == 1 && IsDeictic(slice[0]))
                {
                    deictic = CanonicalizeDeictic(slice[0]);
                    consumed = 1;
                    return true;
                }
            }
            return false;
        }

        public static bool IsPlayerDeictic(string anchor)
        {
            if (string.IsNullOrWhiteSpace(anchor))
                return false;
            string t = CanonicalizeDeictic(anchor);
            return t == "here" || t == "here-here" || t == "over-here";
        }

        public static bool IsCausalityDeictic(string anchor)
        {
            if (string.IsNullOrWhiteSpace(anchor))
                return false;
            string t = CanonicalizeDeictic(anchor);
            return t == "there" || t == "there-there" || t == "through-there";
        }

        public static bool TryParseRelation(string token, out LayoutSpatialRelation relation)
        {
            relation = LayoutSpatialRelation.None;
            if (string.IsNullOrWhiteSpace(token))
                return false;
            string canon = BuiltInSynonyms.CanonicalizeToken(token);
            return RelationByLemma.TryGetValue(canon, out relation);
        }

        public static bool TryParseRelationPhrase(string[] tokens, int start, out LayoutSpatialRelation relation, out int consumed)
        {
            relation = LayoutSpatialRelation.None;
            consumed = 0;
            if (tokens == null || start >= tokens.Length)
                return false;

            for (int len = Math.Min(4, tokens.Length - start); len >= 1; len--)
            {
                var slice = new string[len];
                Array.Copy(tokens, start, slice, 0, len);
                string joined = string.Join(" ", slice);
                string canon = BuiltInSynonyms.TryCanonicalizeMultiWordPhrase(slice) ?? joined;
                if (RelationByLemma.TryGetValue(canon, out relation))
                {
                    consumed = len;
                    return true;
                }
            }
            return false;
        }

        public static bool IsDeictic(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return false;
            string t = CanonicalizeDeictic(token);
            return t == "there" || t == "there-there" || t == "here" || t == "here-here"
                || t == "over-here" || t == "through-there";
        }
    }
}
