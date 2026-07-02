using System;
using System.Collections.Generic;
using System.Text;

namespace Locomotion.Narrative
{
    [Serializable]
    public sealed class QuestNodeDto
    {
        public string id;
        public string kind = "objective";
        public string objectiveId;
        public string text;
        public string summary;
        public string spatial4dId;
        public string predicate4d;
        public string completion4d;
        public string style;
        public string travelBinding;
        public string mapLayer = "composite";
        public string uiBt;
        public string mapBt;
        public string animBt;
        public string audioCue;
        public string ambientLoop;
        public List<QuestNodeDto> children = new List<QuestNodeDto>();
    }

    public static class QuestSpanParser
    {
        public const string PlaceholderName = "quest";

        public sealed class CompileResult
        {
            public string setId = "";
            public string title = "";
            public List<QuestNodeDto> nodes = new List<QuestNodeDto>();
            public List<QuestIssue> issues = new List<QuestIssue>();
        }

        [Serializable]
        public sealed class QuestIssue
        {
            public string level;
            public string message;
            public int line;
        }

        public static CompileResult Compile(string text, string defaultSetId = "quest-set")
        {
            var result = new CompileResult();
            if (string.IsNullOrWhiteSpace(text))
            {
                result.issues.Add(new QuestIssue { level = "error", message = "Empty quest text" });
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
                result.issues.Add(new QuestIssue { level = "error", message = "No quest spans found" });
                return result;
            }

            var setStack = new Stack<string>();
            var blockStack = new List<(int indent, List<QuestNodeDto> children)> { (-1, result.nodes) };
            var objectiveIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int nodeCounter = 0;
            string NewId() => $"q{++nodeCounter}";
            List<QuestNodeDto> CurrentChildren() => blockStack[blockStack.Count - 1].children;

            foreach (var row in lines)
            {
                var props = row.props;
                while (blockStack.Count > 1 && row.indent <= blockStack[blockStack.Count - 1].indent)
                    blockStack.RemoveAt(blockStack.Count - 1);

                if (props.TryGetValue("quest-set", out string openSet))
                {
                    setStack.Push(openSet);
                    if (string.IsNullOrEmpty(result.setId))
                        result.setId = openSet;
                    if (!string.IsNullOrEmpty(row.lineText) && string.IsNullOrEmpty(result.title))
                        result.title = row.lineText;
                    if (!string.IsNullOrEmpty(row.lineText))
                        CurrentChildren().Add(MakeNode(NewId(), props, row.lineText, row.lineNo));
                    continue;
                }

                if (props.TryGetValue("end-block", out string endSet))
                {
                    if (setStack.Count == 0 || setStack.Peek() != endSet)
                    {
                        result.issues.Add(new QuestIssue
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
                if (props.TryGetValue("objective", out string oid))
                {
                    if (!objectiveIds.Add(oid))
                    {
                        result.issues.Add(new QuestIssue
                        {
                            level = "warning",
                            message = $"Duplicate objective id: {oid}",
                            line = row.lineNo
                        });
                    }
                }
                CurrentChildren().Add(node);
            }

            if (setStack.Count > 0)
                result.issues.Add(new QuestIssue { level = "error", message = "Unclosed quest sets" });
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
            out QuestIssue issue)
        {
            indent = line.Length - line.TrimStart(' ').Length;
            props = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            text = "";
            issue = null;

            var segments = PromptSpanParser.Parse(line.Trim());
            PromptSegment questSpan = null;
            foreach (var seg in segments)
            {
                if (seg.isPlaceholder && seg.placeholderName.Equals(PlaceholderName, StringComparison.OrdinalIgnoreCase))
                {
                    questSpan = seg;
                    break;
                }
            }

            if (questSpan == null)
            {
                issue = new QuestIssue { level = "error", message = "Expected {P:quest|...} span", line = lineNo };
                return false;
            }

            props = new Dictionary<string, string>(questSpan.placeholderParams, StringComparer.OrdinalIgnoreCase);
            int after = questSpan.start + questSpan.length;
            if (after < line.Length)
            {
                string rest = line.Substring(after).Trim();
                if (!string.IsNullOrEmpty(rest) && rest.StartsWith("\""))
                    text = Unquote(rest);
            }
            return true;
        }

        static QuestNodeDto MakeNode(string id, Dictionary<string, string> props, string text, int lineNo)
        {
            props.TryGetValue("summary", out string summary);
            return new QuestNodeDto
            {
                id = id,
                kind = props.ContainsKey("objective") ? "objective" : "control",
                objectiveId = Get(props, "objective"),
                text = !string.IsNullOrEmpty(text) ? text : summary,
                summary = summary ?? text,
                spatial4dId = Get(props, "spatial4d"),
                predicate4d = Get(props, "predicate4d"),
                completion4d = Get(props, "completion4d"),
                style = Get(props, "style") ?? Get(props, "style-suggest"),
                travelBinding = Get(props, "travel-binding"),
                mapLayer = Get(props, "map-layer") ?? "composite",
                uiBt = Get(props, "ui-bt"),
                mapBt = Get(props, "map-bt"),
                animBt = Get(props, "anim-bt"),
                audioCue = Get(props, "audio-cue"),
                ambientLoop = Get(props, "ambient-loop")
            };
        }

        static string Get(Dictionary<string, string> props, string key) =>
            props.TryGetValue(key, out string v) ? v : null;

        static string Unquote(string raw)
        {
            raw = raw.Trim();
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
                return raw.Substring(1, raw.Length - 2).Replace("\\\"", "\"").Replace("\\n", "\n");
            return raw;
        }
    }
}
