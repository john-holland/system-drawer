using System;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    public enum MusicStemRole
    {
        Background,
        Font,
        Accent
    }

    public enum MusicAnimationTransitionCategory
    {
        Hold,
        RaiseKey,
        LowerKey,
        RaiseTempo,
        LowerTempo,
        Modulate
    }

    public enum MusicPointCutMode
    {
        None,
        SuspendForReturn,
        Release
    }

    public enum MusicOverlayEdgeKind
    {
        Forward,
        Return,
        Release
    }

    [Serializable]
    public struct MusicStemSlot
    {
        public MusicStemRole role;
        public string sectionId;
        public AudioClip clip;
        public int transpositionSemitones;
        public int barPhase;
        public float volume;
    }

    [Serializable]
    public struct MusicNarrativeBridge
    {
        public string fromLeafId;
        public string toLeafId;
        [TextArea(2, 4)] public string characterPovSummary;
        [TextArea(2, 4)] public string systemPovSummary;
    }
}
