using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Locomotion.Narrative
{
    /// <summary>Parse (GENERATE) and (GENERATE: description) from prompt text.</summary>
    public static class GenerationRequestParser
    {
        private static readonly Regex GenerateRegex = new Regex(
            @"\(GENERATE\s*(?::\s*(.+?))?\)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        /// <summary>Extract all (GENERATE) spans from text. Returns list of start, length, description.</summary>
        public static List<GenerationRequest> Parse(string text)
        {
            var list = new List<GenerationRequest>();
            if (string.IsNullOrEmpty(text)) return list;
            var matches = GenerateRegex.Matches(text);
            foreach (Match m in matches)
            {
                if (!m.Success) continue;
                int start = m.Index;
                int length = m.Length;
                string description = m.Groups.Count > 1 && m.Groups[1].Success ? m.Groups[1].Value.Trim() : "";
                list.Add(new GenerationRequest(start, length, description));
            }
            return list;
        }

        /// <summary>Return text with (GENERATE) spans replaced by spaces (so LSTM does not see literal marker).</summary>
        public static string StripForLSTM(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return GenerateRegex.Replace(text, " ");
        }
    }
}
