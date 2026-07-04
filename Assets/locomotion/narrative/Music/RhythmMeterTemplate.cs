using System;
using UnityEngine;

namespace Locomotion.Narrative.Music
{
    public enum PoeticFootTemplate
    {
        Iambic,
        Trochaic,
        Anapestic,
        Dactylic
    }

    [Serializable]
    public sealed class RhythmMeterTemplate
    {
        public int beatsPerBar = 4;
        public int beatSubdivision = 4;
        public float swingAmount;
        public float stressPattern = 0.5f;
        public PoeticFootTemplate footTemplate = PoeticFootTemplate.Iambic;
        public int feetPerLine = 5;
        public int quantizationMs = 500;
        public string quadPathId = "R";

        public float BeatsPerFoot => footTemplate == PoeticFootTemplate.Anapestic ? 3f : 2f;

        public RhythmMeterTemplate Clone()
        {
            return new RhythmMeterTemplate
            {
                beatsPerBar = beatsPerBar,
                beatSubdivision = beatSubdivision,
                swingAmount = swingAmount,
                stressPattern = stressPattern,
                footTemplate = footTemplate,
                feetPerLine = feetPerLine,
                quantizationMs = quantizationMs,
                quadPathId = quadPathId
            };
        }
    }
}
