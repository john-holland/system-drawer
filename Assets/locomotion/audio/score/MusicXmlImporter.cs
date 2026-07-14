using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Locomotion.Audio
{
    /// <summary>Lightweight MusicXML note extractor (pitch step/octave + duration approximations).</summary>
    public static class MusicXmlImporter
    {
        static readonly Regex NoteRe = new Regex(
            @"<note[\s\S]*?<pitch>[\s\S]*?<step>([A-G])</step>(?:[\s\S]*?<alter>(-?\d+)</alter>)?[\s\S]*?<octave>(\d+)</octave>[\s\S]*?</pitch>[\s\S]*?<duration>(\d+)</duration>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static ScoreDocument ImportXml(string xml, float bpm = 120f, string proxyVoiceId = "score-0")
        {
            var doc = new ScoreDocument { title = "musicxml", bpm = bpm };
            if (string.IsNullOrEmpty(xml)) return doc;
            var events = new List<ScoreEvent>();
            float t = 0f;
            float quarterSec = 60f / Math.Max(1f, bpm);
            foreach (Match m in NoteRe.Matches(xml))
            {
                char step = m.Groups[1].Value[0];
                int alter = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
                int octave = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
                int duration = int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture);
                int midi = StepToMidi(step, alter, octave);
                events.Add(new ScoreEvent
                {
                    timeSec = t,
                    midiNote = midi,
                    velocity01 = 0.75f,
                    proxyVoiceId = proxyVoiceId,
                    family = InstrumentFamily.Keyboard,
                    partName = "score"
                });
                t += (duration / 4f) * quarterSec;
            }
            doc.events = events.ToArray();
            doc.partNames = new[] { "score" };
            return doc;
        }

        static int StepToMidi(char step, int alter, int octave)
        {
            int pc = step switch
            {
                'C' => 0,
                'D' => 2,
                'E' => 4,
                'F' => 5,
                'G' => 7,
                'A' => 9,
                'B' => 11,
                _ => 0
            };
            return (octave + 1) * 12 + pc + alter;
        }
    }
}
