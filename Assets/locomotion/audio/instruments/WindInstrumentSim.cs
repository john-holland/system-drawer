using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Wind: advection assist scalar + attenuated open (tuba-class).</summary>
    public sealed class WindInstrumentSim : PhysicalInstrumentBase
    {
        [Range(0f, 1f)] public float breath01;
        [Range(0f, 1f)] public float advectionAssist01 = 0.5f;
        public bool tubaStyleRadiation = true;

        void Reset()
        {
            if (proxy != null) proxy.family = InstrumentFamily.Wind;
        }

        public override DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            if (attenuatedOpen == null)
                attenuatedOpen = GetComponent<AttenuatedOpenClose>();
            if (attenuatedOpen != null && tubaStyleRadiation)
                attenuatedOpen.mechanicalOpen01 = Mathf.Clamp01(breath01);
            var dsp = base.BuildVoice(controlId, Mathf.Max(raw01, breath01), bpm);
            dsp.filterCutoff *= Mathf.Lerp(0.85f, 1.15f, advectionAssist01);
            return dsp;
        }
    }
}
