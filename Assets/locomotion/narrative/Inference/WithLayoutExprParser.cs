using System.Collections.Generic;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Parses s-expression-style with-lists and plain English into layout placement frames.
    /// </summary>
    public static class WithLayoutExprParser
    {
        static bool _synonymsRegistered;
        const int MaxParseDepth = 8;

        public static bool TryParse(string text, out LayoutPlacementFrame root)
        {
            EnsureSynonyms();
            root = null;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            if (text.StartsWith("("))
            {
                root = ParseSExpr(text);
                return root != null;
            }

            root = ParseEnglish(text);
            return root != null;
        }

        static void EnsureSynonyms()
        {
            if (_synonymsRegistered)
                return;
            WithLemmaRegistry.RegisterBuiltInSynonyms();
            _synonymsRegistered = true;
        }

        static LayoutPlacementFrame ParseSExpr(string text)
        {
            var tokens = TokenizeSExpr(text);
            int i = 0;
            return ParseSExprList(tokens, ref i, 0);
        }

        static LayoutPlacementFrame ParseSExprList(List<string> tokens, ref int i, int depth)
        {
            if (depth > MaxParseDepth)
                return null;
            if (i >= tokens.Count || tokens[i] != "(")
                return null;
            i++;

            if (i >= tokens.Count)
                return null;

            var frame = new LayoutPlacementFrame();
            string head = tokens[i++];
            if (WithLemmaRegistry.TryParseRelation(head, out var rel))
                frame.relation = rel;
            else
                frame.entities.Add(head);

            while (i < tokens.Count && tokens[i] != ")")
            {
                if (tokens[i] == "(")
                {
                    var child = ParseSExprList(tokens, ref i, depth + 1);
                    if (child != null)
                        frame.children.Add(child);
                }
                else if (WithLemmaRegistry.TryParseRelation(tokens[i], out var childRel))
                {
                    var child = new LayoutPlacementFrame { relation = childRel };
                    i++;
                    while (i < tokens.Count && tokens[i] != ")" && tokens[i] != "(")
                    {
                        if (WithLemmaRegistry.IsDeictic(tokens[i]))
                            ApplyDeictic(child, tokens[i++]);
                        else
                            child.entities.Add(tokens[i++]);
                    }
                    frame.children.Add(child);
                }
                else
                {
                    if (WithLemmaRegistry.IsDeictic(tokens[i]))
                        ApplyDeictic(frame, tokens[i++]);
                    else
                        frame.entities.Add(tokens[i++]);
                }
            }

            if (i < tokens.Count && tokens[i] == ")")
                i++;
            return frame;
        }

        static List<string> TokenizeSExpr(string text)
        {
            var tokens = new List<string>();
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (c == '(' || c == ')') { tokens.Add(c.ToString()); i++; continue; }
                int start = i;
                while (i < text.Length && !char.IsWhiteSpace(text[i]) && text[i] != '(' && text[i] != ')')
                    i++;
                tokens.Add(text.Substring(start, i - start));
            }
            return tokens;
        }

        static LayoutPlacementFrame ParseEnglish(string text)
        {
            string[] words = VocabularyBuiltInTokenizer.TokenizeText(text);
            if (words.Length == 0)
                return null;

            var root = new LayoutPlacementFrame { relation = LayoutSpatialRelation.With };
            int i = 0;
            while (i < words.Length && (words[i] == "a" || words[i] == "an" || words[i] == "the"))
                i++;

            if (i < words.Length)
                root.entities.Add(words[i++]);

            while (i < words.Length)
            {
                if (words[i] == "with")
                {
                    i++;
                    if (CountDepth(root) >= MaxParseDepth)
                        break;
                    var child = ParseEnglishWithClause(words, ref i);
                    if (child != null)
                        root.children.Add(child);
                    continue;
                }
                if (words[i] == "and" || words[i] == ",")
                {
                    i++;
                    continue;
                }
                if (WithLemmaRegistry.TryParseDeicticPhrase(words, i, out _, out int rootDeicticConsumed))
                {
                    for (int d = 0; d < rootDeicticConsumed; d++)
                        ApplyDeictic(root, words[i + d]);
                    i += rootDeicticConsumed;
                    continue;
                }
                if (WithLemmaRegistry.TryParseRelationPhrase(words, i, out var rel, out int consumed))
                {
                    var child = new LayoutPlacementFrame { relation = rel };
                    if (root.entities.Count > 0)
                    {
                        child.entities.Add(root.entities[root.entities.Count - 1]);
                        root.entities.RemoveAt(root.entities.Count - 1);
                    }
                    i += consumed;
                    while (i < words.Length && words[i] != "and" && words[i] != "with" && words[i] != ",")
                    {
                        if (WithLemmaRegistry.TryParseDeicticPhrase(words, i, out _, out int deicticConsumed))
                        {
                            for (int d = 0; d < deicticConsumed; d++)
                                ApplyDeictic(child, words[i + d]);
                            i += deicticConsumed;
                        }
                        else if (WithLemmaRegistry.IsDeictic(words[i]))
                            ApplyDeictic(child, words[i++]);
                        else if (!WithLemmaRegistry.TryParseRelation(words[i], out _))
                            child.entities.Add(words[i++]);
                        else
                            break;
                    }
                    root.children.Add(child);
                    continue;
                }
                i++;
            }
            return root;
        }

        static int CountDepth(LayoutPlacementFrame frame)
        {
            if (frame == null || frame.children == null || frame.children.Count == 0)
                return 1;
            int max = 1;
            foreach (var child in frame.children)
                max = System.Math.Max(max, 1 + CountDepth(child));
            return max;
        }

        static LayoutPlacementFrame ParseEnglishWithClause(string[] words, ref int i)
        {
            var frame = new LayoutPlacementFrame { relation = LayoutSpatialRelation.None };
            while (i < words.Length)
            {
                if (words[i] == "and" || words[i] == "with")
                    break;
                if (words[i] == ",")
                {
                    i++;
                    continue;
                }
                if (WithLemmaRegistry.TryParseDeicticPhrase(words, i, out _, out int deicticConsumed))
                {
                    for (int d = 0; d < deicticConsumed; d++)
                        ApplyDeictic(frame, words[i + d]);
                    i += deicticConsumed;
                    continue;
                }
                if (WithLemmaRegistry.TryParseRelationPhrase(words, i, out var rel, out int consumed))
                {
                    frame.relation = rel;
                    i += consumed;
                    continue;
                }
                frame.entities.Add(words[i++]);
            }
            return frame.entities.Count > 0 || frame.anchor != null || frame.children.Count > 0 ? frame : null;
        }

        static void ApplyDeictic(LayoutPlacementFrame frame, string token)
        {
            string deictic = WithLemmaRegistry.CanonicalizeDeictic(token);
            if (string.IsNullOrEmpty(frame.anchor))
            {
                frame.anchor = deictic;
                return;
            }

            if (WithLemmaRegistry.TryMergeDeicticAnchor(frame.anchor, deictic, out string merged))
            {
                frame.anchor = merged;
                return;
            }

            if (CountDepth(frame) >= MaxParseDepth)
            {
                frame.anchor = deictic;
                return;
            }

            frame.children.Add(CreateDeicticSibling(frame, frame.anchor));
            frame.anchor = deictic;
        }

        static LayoutPlacementFrame CreateDeicticSibling(LayoutPlacementFrame parent, string anchor)
        {
            var sibling = new LayoutPlacementFrame
            {
                relation = parent.relation,
                anchor = anchor,
                causalityLeafId = parent.causalityLeafId
            };
            sibling.entities.AddRange(parent.entities);
            return sibling;
        }
    }
}
