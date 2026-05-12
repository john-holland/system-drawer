using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Locomotion.Narrative
{
    /// <summary>
    /// One segment of authored prompt text: literal run or a <c>{P:...}</c> placeholder span.
    /// </summary>
    [Serializable]
    public sealed class PromptSegment
    {
        public bool isPlaceholder;
        public int start;
        public int length;
        public string textRun;
        public string placeholderName = "";
        public Dictionary<string, string> placeholderParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static PromptSegment TextRun(string raw, int start, int length)
        {
            return new PromptSegment
            {
                isPlaceholder = false,
                start = start,
                length = length,
                textRun = length > 0 ? raw.Substring(start, length) : ""
            };
        }

        public static PromptSegment Placeholder(int start, int length, string name, Dictionary<string, string> parameters)
        {
            return new PromptSegment
            {
                isPlaceholder = true,
                start = start,
                length = length,
                placeholderName = name ?? "",
                placeholderParams = parameters != null
                    ? new Dictionary<string, string>(parameters, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            };
        }

        public string SerializeToSpan()
        {
            if (!isPlaceholder)
                return textRun ?? "";
            return PromptSpanParser.FormatPlaceholder(placeholderName, placeholderParams);
        }
    }

    /// <summary>
    /// Parses and formats <c>{P:name}</c> and <c>{P:name|key=value|...}</c> spans inside narrative prompt strings.
    /// </summary>
    public static class PromptSpanParser
    {
        const string Open = "{P:";

        /// <summary>Split <paramref name="text"/> into ordered segments (covers entire string).</summary>
        public static List<PromptSegment> Parse(string text)
        {
            var segments = new List<PromptSegment>();
            if (text == null)
                return segments;
            int i = 0;
            while (i < text.Length)
            {
                int open = text.IndexOf(Open, i, StringComparison.Ordinal);
                if (open < 0)
                {
                    if (i < text.Length)
                        segments.Add(PromptSegment.TextRun(text, i, text.Length - i));
                    break;
                }

                if (open > i)
                    segments.Add(PromptSegment.TextRun(text, i, open - i));

                int innerStart = open + Open.Length;
                int close = text.IndexOf('}', innerStart);
                if (close < 0)
                {
                    segments.Add(PromptSegment.TextRun(text, open, text.Length - open));
                    break;
                }

                string inner = text.Substring(innerStart, close - innerStart);
                ParseInner(inner, out string name, out Dictionary<string, string> pars);
                int spanLen = close - open + 1;
                segments.Add(PromptSegment.Placeholder(open, spanLen, name, pars));
                i = close + 1;
            }

            return segments;
        }

        /// <summary>Rebuild full prompt text from segments (round-trip after edits).</summary>
        public static string JoinSegments(IReadOnlyList<PromptSegment> segments)
        {
            if (segments == null || segments.Count == 0)
                return "";
            var sb = new StringBuilder();
            foreach (PromptSegment s in segments)
                sb.Append(s.SerializeToSpan());
            return sb.ToString();
        }

        /// <summary>Replace substring [<paramref name="start"/>, <paramref name="length"/>] with <paramref name="replacement"/>.</summary>
        public static string ReplaceRange(string text, int start, int length, string replacement)
        {
            if (text == null) text = "";
            if (start < 0) start = 0;
            if (length < 0) length = 0;
            if (start > text.Length) start = text.Length;
            int end = Math.Min(text.Length, start + length);
            return text.Substring(0, start) + (replacement ?? "") + text.Substring(end);
        }

        public static string FormatPlaceholder(string name, IDictionary<string, string> parameters)
        {
            var sb = new StringBuilder();
            sb.Append(Open);
            bool first = true;
            if (!string.IsNullOrEmpty(name))
            {
                sb.Append(name);
                first = false;
            }

            if (parameters != null && parameters.Count > 0)
            {
                foreach (var kv in parameters.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (string.IsNullOrEmpty(kv.Key))
                        continue;
                    if (!first)
                        sb.Append('|');
                    sb.Append(kv.Key);
                    sb.Append('=');
                    sb.Append(kv.Value ?? "");
                    first = false;
                }
            }

            sb.Append('}');
            return sb.ToString();
        }

        static void ParseInner(string inner, out string name, out Dictionary<string, string> parameters)
        {
            name = "";
            parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(inner))
                return;

            string[] parts = inner.Split('|');
            if (parts.Length == 0)
                return;

            bool firstIsKv = parts[0].IndexOf('=') >= 0;
            int idx = 0;
            if (!firstIsKv)
            {
                name = parts[0].Trim();
                idx = 1;
            }

            for (int p = idx; p < parts.Length; p++)
            {
                string part = parts[p];
                int eq = part.IndexOf('=');
                if (eq <= 0)
                    continue;
                string k = part.Substring(0, eq).Trim();
                string v = eq < part.Length - 1 ? part.Substring(eq + 1) : "";
                if (!string.IsNullOrEmpty(k) && !parameters.ContainsKey(k))
                    parameters[k] = v;
            }
        }
    }
}
