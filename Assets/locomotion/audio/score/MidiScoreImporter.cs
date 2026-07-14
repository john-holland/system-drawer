using System.Collections.Generic;
using System.IO;

namespace Locomotion.Audio
{
    /// <summary>Minimal MIDI Type-0/1 note-on importer (variable-length delta + note on).</summary>
    public static class MidiScoreImporter
    {
        public static ScoreDocument ImportBytes(byte[] data, string proxyVoiceId = "voice-0")
        {
            var doc = new ScoreDocument { title = "midi" };
            if (data == null || data.Length < 14) return doc;

            var events = new List<ScoreEvent>();
            float bpm = 120f;
            float ticksPerQuarter = 480f;
            float secondsPerTick = 60f / bpm / ticksPerQuarter;
            float t = 0f;

            int i = 0;
            // Skip header if present
            if (data.Length > 14 && data[0] == 'M' && data[1] == 'T' && data[2] == 'h' && data[3] == 'd')
            {
                ticksPerQuarter = (data[12] << 8) | data[13];
                if (ticksPerQuarter <= 0) ticksPerQuarter = 480f;
                secondsPerTick = 60f / bpm / ticksPerQuarter;
                i = 14;
                // Find first MTrk
                while (i + 8 < data.Length)
                {
                    if (data[i] == 'M' && data[i + 1] == 'T' && data[i + 2] == 'r' && data[i + 3] == 'k')
                    {
                        i += 8;
                        break;
                    }
                    i++;
                }
            }

            byte running = 0;
            while (i < data.Length)
            {
                int delta = ReadVarLen(data, ref i);
                t += delta * secondsPerTick;
                if (i >= data.Length) break;
                byte status = data[i];
                if (status < 0x80)
                {
                    status = running;
                }
                else
                {
                    running = status;
                    i++;
                }
                int type = status & 0xF0;
                if (type == 0x90 && i + 1 < data.Length)
                {
                    int note = data[i++];
                    int vel = data[i++];
                    if (vel > 0)
                    {
                        events.Add(new ScoreEvent
                        {
                            timeSec = t,
                            midiNote = note,
                            velocity01 = vel / 127f,
                            proxyVoiceId = proxyVoiceId,
                            family = InstrumentFamily.Generic
                        });
                    }
                }
                else if (type == 0x80 && i + 1 < data.Length)
                {
                    i += 2;
                }
                else if (status == 0xFF && i < data.Length)
                {
                    byte meta = data[i++];
                    int len = ReadVarLen(data, ref i);
                    if (meta == 0x51 && len == 3 && i + 2 < data.Length)
                    {
                        int mpqn = (data[i] << 16) | (data[i + 1] << 8) | data[i + 2];
                        if (mpqn > 0)
                        {
                            bpm = 60000000f / mpqn;
                            secondsPerTick = 60f / bpm / ticksPerQuarter;
                        }
                    }
                    i += len;
                }
                else if (type == 0xC0 || type == 0xD0)
                {
                    i += 1;
                }
                else if (type >= 0x80)
                {
                    i += 2;
                }
                else break;
            }

            doc.bpm = bpm;
            doc.events = events.ToArray();
            return doc;
        }

        public static ScoreDocument ImportFile(string path, string proxyVoiceId = "voice-0")
        {
            if (!File.Exists(path)) return new ScoreDocument { title = path };
            return ImportBytes(File.ReadAllBytes(path), proxyVoiceId);
        }

        static int ReadVarLen(byte[] data, ref int i)
        {
            int value = 0;
            while (i < data.Length)
            {
                byte b = data[i++];
                value = (value << 7) | (b & 0x7F);
                if ((b & 0x80) == 0) break;
            }
            return value;
        }
    }
}
