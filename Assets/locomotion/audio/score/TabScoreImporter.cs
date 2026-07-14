using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Locomotion.Audio
{
    /// <summary>ASCII guitar tab line importer (e.g. e|--0--2--3--|).</summary>
    public static class TabScoreImporter
    {
        static readonly Regex LineRe = new Regex(@"^[A-Ga-g]#?\s*[|:]\s*([0-9\-xX\|]+)", RegexOptions.Compiled);

        public static ScoreDocument ImportText(string tabText, float bpm = 120f, string proxyVoiceId = "guitar-0")
        {
            var doc = new ScoreDocument { title = "tab", bpm = bpm };
            if (string.IsNullOrEmpty(tabText)) return doc;

            var events = new List<ScoreEvent>();
            float beat = 60f / MathfMax(bpm, 1f);
            string[] lines = tabText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                var m = LineRe.Match(line.Trim());
                if (!m.Success) continue;
                string body = m.Groups[1].Value;
                int stringOpenMidi = GuessOpenString(line);
                int col = 0;
                for (int i = 0; i < body.Length; i++)
                {
                    char c = body[i];
                    if (c >= '0' && c <= '9')
                    {
                        int fret = c - '0';
                        if (i + 1 < body.Length && body[i + 1] >= '0' && body[i + 1] <= '9')
                        {
                            fret = fret * 10 + (body[i + 1] - '0');
                            i++;
                        }
                        events.Add(new ScoreEvent
                        {
                            timeSec = col * beat * 0.25f,
                            midiNote = stringOpenMidi + fret,
                            velocity01 = 0.8f,
                            proxyVoiceId = proxyVoiceId,
                            partName = "guitar",
                            family = InstrumentFamily.Strings
                        });
                        col++;
                    }
                    else if (c == '-' || c == 'x' || c == 'X')
                    {
                        col++;
                    }
                }
            }
            doc.events = events.ToArray();
            doc.partNames = new[] { "guitar" };
            return doc;
        }

        static int GuessOpenString(string line)
        {
            string s = line.Trim().ToLowerInvariant();
            if (s.StartsWith("e")) return 64;
            if (s.StartsWith("b")) return 59;
            if (s.StartsWith("g")) return 55;
            if (s.StartsWith("d")) return 50;
            if (s.StartsWith("a")) return 45;
            return 40; // low E
        }

        static float MathfMax(float a, float b) => a > b ? a : b;
    }
}
