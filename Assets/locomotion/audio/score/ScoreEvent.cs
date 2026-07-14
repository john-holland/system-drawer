using System;

namespace Locomotion.Audio
{
    [Serializable]
    public struct ScoreEvent
    {
        public float timeSec;
        public int midiNote;
        public float velocity01;
        public string proxyVoiceId;
        public string partName;
        public InstrumentFamily family;
    }

    [Serializable]
    public sealed class ScoreDocument
    {
        public string title;
        public float bpm = 120f;
        public ScoreEvent[] events = Array.Empty<ScoreEvent>();
        public string[] partNames = Array.Empty<string>();
    }
}
