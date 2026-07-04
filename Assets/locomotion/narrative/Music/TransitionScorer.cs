using UnityEngine;

namespace Locomotion.Narrative.Music
{
    public sealed class TransitionScorer
    {
        public float wKey = 1f;
        public float wTempo = 0.5f;
        public float wRhythm = 0.4f;
        public float wVoice = 0.3f;
        public float wOsc = 0.6f;
        public float wBank = 0.5f;

        [Range(0f, 1f)] public float smoothness = 0.8f;
        [Range(0f, 1f)] public float awkwardness = 0f;

        public float Score(
            MusicSectionAsset from,
            MusicSectionAsset to,
            RhythmMeterTemplate rhythmFrom,
            RhythmMeterTemplate rhythmTo,
            ModulationSavingsBank bank)
        {
            if (from == null || to == null) return float.MaxValue;
            if (ReferenceEquals(from, to)) return 0f;

            float keyCost = MusicTheory.TonalDistance(from.TonicPc, to.TonicPc);
            float tempoCost = Mathf.Abs(Mathf.Log(Mathf.Max(to.bpm, 1f) / Mathf.Max(from.bpm, 1f), 2f));
            float rhythmCost = MusicTheory.RhythmMismatch(rhythmFrom, rhythmTo);
            float voiceCost = MusicTheory.VoiceLeadingPenalty(from.chordRootPc, to.chordRootPc);
            float oscCost = bank != null ? bank.OscillationPenalty(to.TonicPc) : 0f;
            float bankCredit = bank != null ? bank.Credit(to.TonicPc) : 0f;

            int common = MusicTheory.CommonToneCount(from.TonicPc, to.TonicPc, from.majorMode);
            if (common >= 2) keyCost *= 0.5f;

            float dissonanceBoost = awkwardness * (keyCost + rhythmCost * 0.5f);
            float smoothScale = Mathf.Lerp(1.5f, 0.35f, smoothness);

            float cost = smoothScale * (
                wKey * keyCost +
                wTempo * tempoCost +
                wRhythm * rhythmCost +
                wVoice * voiceCost +
                wOsc * oscCost -
                wBank * bankCredit
            ) + dissonanceBoost;

            return cost;
        }

        public void ApplyAnimationCategory(MusicAnimationTransitionCategory category)
        {
            switch (category)
            {
                case MusicAnimationTransitionCategory.RaiseKey:
                    wKey *= 0.7f;
                    break;
                case MusicAnimationTransitionCategory.LowerKey:
                    wKey *= 0.7f;
                    break;
                case MusicAnimationTransitionCategory.RaiseTempo:
                    wTempo *= 1.3f;
                    break;
                case MusicAnimationTransitionCategory.LowerTempo:
                    wTempo *= 1.3f;
                    break;
                case MusicAnimationTransitionCategory.Modulate:
                    wBank *= 0.5f;
                    awkwardness = Mathf.Max(awkwardness, 0.2f);
                    break;
            }
        }
    }
}
