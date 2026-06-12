using System.Collections.Generic;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Parses s-expression-style with-lists and plain English into layout placement frames.
    /// </summary>
    public static class WithLayoutExprParser
    {
        static bool _synonymsRegistered;

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
            return ParseSExprList(tokens, ref i);
        }

        static LayoutPlacementFrame ParseSExprList(List<string> tokens, ref int i)
        {
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
                    var child = ParseSExprList(tokens, ref i);
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
                            child.anchor = tokens[i++];
                        else
                            child.entities.Add(tokens[i++]);
                    }
                    frame.children.Add(child);
                }
                else
                {
                    if (WithLemmaRegistry.IsDeictic(tokens[i]))
                        frame.anchor = tokens[i++];
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
                        if (WithLemmaRegistry.IsDeictic(words[i]))
                            child.anchor = words[i++];
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
                if (WithLemmaRegistry.TryParseRelationPhrase(words, i, out var rel, out int consumed))
                {
                    frame.relation = rel;
                    i += consumed;
                    continue;
                }
                if (WithLemmaRegistry.IsDeictic(words[i]))
                {
                    frame.anchor = words[i++];
                    continue;
                }
                frame.entities.Add(words[i++]);
            }
            return frame.entities.Count > 0 || frame.anchor != null ? frame : null;
        }
    }
}
