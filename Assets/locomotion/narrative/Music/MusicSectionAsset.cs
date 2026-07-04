using UnityEngine;

namespace Locomotion.Narrative.Music
{
    [CreateAssetMenu(fileName = "MusicSection", menuName = "Locomotion/Narrative/Music Section", order = 10)]
    public sealed class MusicSectionAsset : ScriptableObject
    {
        [Header("Identity")]
        public string sectionId;

        [Header("Stem")]
        public MusicStemRole stemRole = MusicStemRole.Background;
        [Range(0, 11)] public int harmonicHue;
        [Range(0f, 1f)] public float energy = 0.5f;

        [Header("Harmony")]
        public string canonicalKey = "C";
        public bool majorMode = true;
        public int chordRootPc;
        public string chordProgressionTag;

        [Header("Timing")]
        public float bpm = 120f;
        public int bars = 4;
        public int beatsPerBar = 4;
        public int downbeatPhase;

        [Header("Audio")]
        public AudioClip loopClip;
        [Tooltip("Optional procedural stem (e.g. DynamicMusicGenerator).")]
        public ScriptableObject proceduralGenerator;

        public int TonicPc => MusicTheory.TonicFromKeyName(canonicalKey);

        public string StableId => string.IsNullOrWhiteSpace(sectionId) ? name : sectionId;
    }
}
