using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>String instrument: tensile/taughtness + optional hollow factor; rope break hook optional.</summary>
    public sealed class StringInstrumentSim : PhysicalInstrumentBase
    {
        [Range(0f, 1f)] public float tensile01 = 0.7f;
        [Range(0f, 1f)] public float taughtness01 = 0.7f;
        [Range(0f, 1f)] public float hollowBodyCoupling01;
        public bool enableStressBreak;
        public float bowWeight = 0.06f;

        void Reset()
        {
            if (proxy != null) proxy.family = InstrumentFamily.Strings;
        }

        public override DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            var dsp = base.BuildVoice(controlId, raw01, bpm);
            float tension = Mathf.Clamp01(tensile01 * taughtness01);
            dsp.baseFrequency *= Mathf.Lerp(0.92f, 1.08f, tension);
            dsp.filterResonance = Mathf.Clamp01(dsp.filterResonance + hollowBodyCoupling01 * 0.2f);
            dsp.modulationDepth = Mathf.Clamp01(dsp.modulationDepth + bowWeight);
            if (enableStressBreak && tension > 0.98f && raw01 > 0.95f)
            {
                dsp.filterResonance = Mathf.Clamp01(dsp.filterResonance + 0.35f);
                dsp.modulationDepth = Mathf.Clamp01(dsp.modulationDepth + 0.4f);
            }
            return dsp;
        }
    }
}
