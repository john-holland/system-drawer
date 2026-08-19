using System;
using System.Collections.Generic;
using System.Text;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Parsed dialogue line or control node from lemma {P:dialogue|...} spans.
    /// </summary>
    [Serializable]
    public sealed class DialogueNodeDto
    {
        public string id;
        public string kind = "line";
        public string text;
        public string presentation = "text";
        public string answerId;
        public string goal;
        public string predicate4d;
        public string completion4d;
        public string continueWithDialogue;
        public string speakerKey;
        public string visMode = "auto";
        public string audioRef;
        public string dialogActorId;
        public int charStart;
        public int charEnd;
        public float seconds;
        public List<string> options = new List<string>();
        public List<DialogueNodeDto> children = new List<DialogueNodeDto>();
    }

    /// <summary>
    /// Mirrors Python dialogue_parser.py for Unity offline validation and import.
    /// </summary>
    public static class DialogueSpanParser
    {
        public const string PlaceholderName = "dialogue";

        public sealed class CompileResult
        {
            public string setId = "";
            public List<DialogueNodeDto> nodes = new List<DialogueNodeDto>();
            public List<DialogueIssue> issues = new List<DialogueIssue>();
        }

        [Serializable]
        public sealed class DialogueIssue
        {
            public string level;
            public string message;
            public int line;
        }

        public static CompileResult Compile(string text, string defaultSetId = "dialogue-set")
        {
            var result = new CompileResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                result.issues.Add(new DialogueIssue { level = "error", message = "Empty dialogue text" });
                return result;
            }

            var lines = new List<(int lineNo, int indent, Dictionary<string, string> props, string lineText)>();
            var rawLines = text.Split('\n');
            for (int i = 0; i < rawLines.Length; i++)
            {
                string raw = rawLines[i];
                if (string.IsNullOrWhiteSpace(raw) || raw.TrimStart().StartsWith("#"))
                    continue;
                if (!TryParseLine(raw, i + 1, out int indent, out var props, out string lineText, out var issue))
                {
                    if (issue != null)
                        result.issues.Add(issue);
                    continue;
                }
                if (issue != null)
                    result.issues.Add(issue);
                lines.Add((i + 1, indent, props, lineText));
            }

            if (lines.Count == 0)
            {
                result.issues.Add(new DialogueIssue { level = "error", message = "No dialogue spans found" });
                return result;
            }

            var setStack = new Stack<string>();
            var blockStack = new List<(int indent, List<DialogueNodeDto> children)> { (-1, result.nodes) };
            var answerIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int nodeCounter = 0;

            string NewId() => $"n{++nodeCounter}";

            List<DialogueNodeDto> CurrentChildren() => blockStack[blockStack.Count - 1].children;

            foreach (var row in lines)
            {
                var props = row.props;
                while (blockStack.Count > 1 && row.indent <= blockStack[blockStack.Count - 1].indent)
                    blockStack.RemoveAt(blockStack.Count - 1);

                if (props.TryGetValue("dialogue-set", out string openSet) || props.TryGetValue("dialog-set", out openSet))
                {
                    if (props.ContainsKey("dialogue-set") && !props.ContainsKey("answer"))
                    {
                        setStack.Push(openSet);
                        if (string.IsNullOrEmpty(result.setId))
                            result.setId = openSet;
                        if (!string.IsNullOrEmpty(row.lineText))
                            CurrentChildren().Add(MakeNode(NewId(), props, row.lineText, row.lineNo));
                        continue;
                    }
                }

                if (props.TryGetValue("end-block", out string endSet))
                {
                    if (setStack.Count == 0 || setStack.Peek() != endSet)
                    {
                        result.issues.Add(new DialogueIssue
                        {
                            level = "error",
                            message = $"end-block={endSet} does not match open set",
                            line = row.lineNo
                        });
                    }
                    else
                        setStack.Pop();
                    continue;
                }

                if (row.indent > blockStack[blockStack.Count - 1].indent)
                {
                    var parentChildren = CurrentChildren();
                    if (parentChildren.Count > 0)
                        blockStack.Add((row.indent, parentChildren[parentChildren.Count - 1].children));
                    else
                        blockStack.Add((row.indent, parentChildren));
                }

                var node = MakeNode(NewId(), props, row.lineText, row.lineNo);
                if (props.TryGetValue("answer", out string aid))
                {
                    if (!answerIds.Add(aid))
                    {
                        result.issues.Add(new DialogueIssue
                        {
                            level = "warning",
                            message = $"Duplicate answer id: {aid}",
                            line = row.lineNo
                        });
                    }
                }

                CurrentChildren().Add(node);
            }

            if (setStack.Count > 0)
            {
                result.issues.Add(new DialogueIssue
                {
                    level = "error",
                    message = "Unclosed dialogue sets"
                });
            }

            if (string.IsNullOrEmpty(result.setId))
                result.setId = defaultSetId;

            return result;
        }

        static bool TryParseLine(
            string line,
            int lineNo,
            out int indent,
            out Dictionary<string, string> props,
            out string text,
            out DialogueIssue issue)
        {
            indent = line.Length - line.TrimStart(' ').Length;
            props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            text = "";
            issue = null;

            var segments = PromptSpanParser.Parse(line.Trim());
            PromptSegment dialogueSpan = null;
            foreach (var seg in segments)
            {
                if (seg.isPlaceholder && seg.placeholderName.Equals(PlaceholderName, StringComparison.OrdinalIgnoreCase))
                {
                    dialogueSpan = seg;
                    break;
                }
            }

            if (dialogueSpan == null)
            {
                issue = new DialogueIssue { level = "error", message = "Expected {P:dialogue|...} span", line = lineNo };
                return false;
            }

            props = new Dictionary<string, string>(dialogueSpan.placeholderParams, StringComparer.OrdinalIgnoreCase);

            int after = dialogueSpan.start + dialogueSpan.length;
            if (after < line.Length)
            {
                string rest = line.Substring(after).Trim();
                if (!string.IsNullOrEmpty(rest))
                {
                    if (rest.StartsWith("\""))
                        text = Unquote(rest);
                    else
                        issue = new DialogueIssue { level = "warning", message = "Unparsed trailing content", line = lineNo };
                }
            }

            return true;
        }

        static DialogueNodeDto MakeNode(string id, Dictionary<string, string> props, string text, int lineNo)
        {
            props.TryGetValue("presentation", out string presentation);
            if (string.IsNullOrEmpty(presentation))
                presentation = "text";
            props.TryGetValue("vis", out string vis);
            if (string.IsNullOrEmpty(vis))
                vis = "auto";
            props.TryGetValue("speaker", out string speaker);
            props.TryGetValue("audio-ref", out string audioRef);
            if (string.IsNullOrEmpty(audioRef))
                props.TryGetValue("audioRef", out audioRef);

            var node = new DialogueNodeDto
            {
                id = id,
                kind = "line",
                text = text,
                presentation = presentation,
                goal = Get(props, "goal"),
                predicate4d = Get(props, "predicate4d"),
                completion4d = Get(props, "completion4d"),
                continueWithDialogue = Get(props, "continue-with-dialogue"),
                speakerKey = speaker,
                visMode = vis,
                audioRef = audioRef
            };

            if (props.TryGetValue("answer", out string answer))
                node.answerId = answer;
            if (props.TryGetValue("options", out string optsRaw))
                node.options = ParseOptions(optsRaw);

            return node;
        }

        static string Get(Dictionary<string, string> props, string key) =>
            props.TryGetValue(key, out string v) ? v : null;

        static List<string> ParseOptions(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
                return list;
            raw = raw.Trim();
            if (raw.StartsWith("[") && raw.EndsWith("]"))
                raw = raw.Substring(1, raw.Length - 2);
            foreach (string part in raw.Split(','))
            {
                string p = part.Trim();
                if (!string.IsNullOrEmpty(p))
                    list.Add(p);
            }
            return list;
        }

        static string Unquote(string raw)
        {
            raw = raw.Trim();
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
                return raw.Substring(1, raw.Length - 2).Replace("\\\"", "\"").Replace("\\n", "\n");
            return raw;
        }
    }
}
