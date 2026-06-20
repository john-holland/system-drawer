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
        const string DoubleOpen = "{{P:";

        /// <summary>Text with all placeholder spans removed (for LSTM / plain-text consumers).</summary>
        public static string StripForLSTM(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text ?? "";
            var segments = Parse(text);
            var sb = new StringBuilder();
            foreach (PromptSegment s in segments)
            {
                if (!s.isPlaceholder)
                    sb.Append(s.textRun ?? "");
            }
            return sb.ToString();
        }

        /// <summary>Read bool param from a placeholder segment.</summary>
        public static bool TryGetBoolParam(PromptSegment segment, string key, out bool value)
        {
            value = false;
            if (segment == null || !segment.isPlaceholder || segment.placeholderParams == null ||
                !segment.placeholderParams.TryGetValue(key, out string raw))
                return false;
            return TryParseBoolParam(raw, out value);
        }

        public static bool TryParseBoolParam(string raw, out bool value)
        {
            value = false;
            if (string.IsNullOrWhiteSpace(raw))
                return false;
            raw = raw.Trim();
            if (raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("yes", StringComparison.OrdinalIgnoreCase))
            {
                value = true;
                return true;
            }
            if (raw == "0" || raw.Equals("false", StringComparison.OrdinalIgnoreCase) ||
                raw.Equals("no", StringComparison.OrdinalIgnoreCase))
            {
                value = false;
                return true;
            }
            return false;
        }

        /// <summary>Split <paramref name="text"/> into ordered segments (covers entire string).</summary>
        public static List<PromptSegment> Parse(string text)
        {
            var segments = new List<PromptSegment>();
            if (text == null)
                return segments;
            int i = 0;
            while (i < text.Length)
            {
                int open = FindNextOpen(text, i, out bool isDouble);
                if (open < 0)
                {
                    if (i < text.Length)
                        segments.Add(PromptSegment.TextRun(text, i, text.Length - i));
                    break;
                }

                if (open > i)
                    segments.Add(PromptSegment.TextRun(text, i, open - i));

                int innerStart = open + (isDouble ? DoubleOpen.Length : Open.Length);
                int closeBrace = FindMatchingClose(text, innerStart, isDouble);
                if (closeBrace < 0)
                {
                    segments.Add(PromptSegment.TextRun(text, open, text.Length - open));
                    break;
                }

                int innerLength = isDouble
                    ? closeBrace - 1 - innerStart
                    : closeBrace - innerStart;
                string inner = text.Substring(innerStart, innerLength);
                ParseInner(inner, isDouble, out string name, out Dictionary<string, string> pars);
                int spanLen = closeBrace - open + 1;
                segments.Add(PromptSegment.Placeholder(open, spanLen, name, pars));
                i = closeBrace + 1;
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

        static int FindNextOpen(string text, int start, out bool isDouble)
        {
            isDouble = false;
            int single = text.IndexOf(Open, start, StringComparison.Ordinal);
            int dbl = text.IndexOf(DoubleOpen, start, StringComparison.Ordinal);
            if (single < 0 && dbl < 0)
                return -1;
            if (dbl >= 0 && (single < 0 || dbl <= single))
            {
                isDouble = true;
                return dbl;
            }
            return single;
        }

        static int FindMatchingClose(string text, int innerStart, bool isDouble)
        {
            if (!isDouble)
                return text.IndexOf('}', innerStart);
            int idx = text.IndexOf("}}", innerStart, StringComparison.Ordinal);
            return idx >= 0 ? idx + 1 : -1;
        }

        static void ParseInner(string inner, bool isDoubleBrace, out string name, out Dictionary<string, string> parameters)
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
                name = UnquoteName(parts[0].Trim(), isDoubleBrace);
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

        static string UnquoteName(string raw, bool isDoubleBrace)
        {
            if (!isDoubleBrace || string.IsNullOrEmpty(raw))
                return raw ?? "";
            raw = raw.Trim();
            if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
                return raw.Substring(1, raw.Length - 2);
            return raw;
        }

        static void ParseInner(string inner, out string name, out Dictionary<string, string> parameters) =>
            ParseInner(inner, false, out name, out parameters);
    }
}
