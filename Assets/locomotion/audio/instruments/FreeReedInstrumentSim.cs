using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Accordion-class bellows: attenuated open from fold length + 3D DSP.</summary>
    public sealed class FreeReedInstrumentSim : PhysicalInstrumentBase
    {
        [Range(0f, 1f)] public float bellowsFold01 = 0.5f;
        [Range(0f, 1f)] public float bellowsPressure01 = 0.5f;

        void Reset()
        {
            if (proxy != null) proxy.family = InstrumentFamily.FreeReed;
        }

        public override DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            if (attenuatedOpen == null)
                attenuatedOpen = GetComponent<AttenuatedOpenClose>();
            if (attenuatedOpen != null)
                attenuatedOpen.mechanicalOpen01 = bellowsFold01;
            float control = Mathf.Clamp01(Mathf.Max(raw01, bellowsPressure01));
            var dsp = base.BuildVoice(controlId, control, bpm);
            dsp.amplitudeEnvelope = new Vector4(
                Mathf.Lerp(0.05f, 0.25f, bellowsPressure01),
                dsp.amplitudeEnvelope.y,
                Mathf.Clamp01(bellowsPressure01),
                dsp.amplitudeEnvelope.w);
            return dsp;
        }
    }
}
