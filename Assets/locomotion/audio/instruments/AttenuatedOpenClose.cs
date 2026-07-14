using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Continuous open/close (tuba radiation, accordion bellows) driven by 3D DSP / pathing factors.
    /// Drive <see cref="mechanicalOpen01"/> from an open/close bridge via <see cref="SyncFromOpen01"/>.
    /// </summary>
    public sealed class AttenuatedOpenClose : MonoBehaviour
    {
        [Range(0f, 1f)] public float mechanicalOpen01 = 0.5f;
        [Range(0f, 1f)] public float pathingTransmission01 = 1f;
        [Range(0f, 1f)] public float enclosureOcclusion01;
        public AnimationCurve openToVolume = AnimationCurve.EaseInOut(0f, 0.2f, 1f, 1f);
        public AnimationCurve openToResonance = AnimationCurve.Linear(0f, 0.3f, 1f, 0.7f);
        public AnimationCurve openToDryWet = AnimationCurve.Linear(0f, 0.1f, 1f, 0.8f);
        public AnimationCurve openToBrightness = AnimationCurve.Linear(0f, 0.4f, 1f, 1f);

        public float AttenuatedOpen01 =>
            Mathf.Clamp01(mechanicalOpen01 * pathingTransmission01 * (1f - enclosureOcclusion01));

        public void SyncFromOpen01(float open01)
        {
            mechanicalOpen01 = Mathf.Clamp01(open01);
        }

        public void SampleFromPathing(float transmission01, float occlusion01)
        {
            pathingTransmission01 = Mathf.Clamp01(transmission01);
            enclosureOcclusion01 = Mathf.Clamp01(occlusion01);
        }

        public void ApplyToDsp(DSPParams dsp)
        {
            if (dsp == null) return;
            float o = AttenuatedOpen01;
            float vol = openToVolume.Evaluate(o);
            dsp.amplitudeEnvelope = new Vector4(
                dsp.amplitudeEnvelope.x,
                dsp.amplitudeEnvelope.y,
                Mathf.Clamp01(dsp.amplitudeEnvelope.z * vol),
                dsp.amplitudeEnvelope.w);
            dsp.filterResonance = Mathf.Lerp(dsp.filterResonance, openToResonance.Evaluate(o), 0.75f);
            dsp.filterCutoff = Mathf.Lerp(400f, Mathf.Max(dsp.filterCutoff, 2000f), openToBrightness.Evaluate(o));
            dsp.reverbAmount = Mathf.Lerp(dsp.reverbAmount, openToDryWet.Evaluate(o), 0.5f);
        }
    }
}
