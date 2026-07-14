using UnityEngine;

namespace Locomotion.Audio
{
    public enum ElectronicDeviceKind
    {
        GenericElectronic,
        DrumMachine,
        Dac,
        DigitalFxRack,
        Synthesizer
    }

    /// <summary>
    /// Electronic family device (DAC, drum machine, digital rack).
    /// Owns dry/wet, LFO, PWM, and wave-shape articulation options.
    /// </summary>
    public sealed class ElectronicInstrumentSim : PhysicalInstrumentBase
    {
        public ElectronicDeviceKind deviceKind = ElectronicDeviceKind.GenericElectronic;

        void Reset()
        {
            if (proxy != null)
                proxy.family = InstrumentFamily.Electronic;
        }

        protected override void Awake()
        {
            base.Awake();
            if (proxy != null)
            {
                proxy.family = InstrumentFamily.Electronic;
                proxy.EnsureProfile();
                if (proxy.profileCurves != null)
                    proxy.profileCurves.family = InstrumentFamily.Electronic;
            }
        }

        public override DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            string key = string.IsNullOrEmpty(controlId) ? SuggestControlId() : controlId;
            var dsp = base.BuildVoice(key, raw01, bpm);
            if (proxy?.profileCurves == null) return dsp;

            var c = proxy.profileCurves;
            float t = Mathf.Clamp01(raw01);
            dsp.modulationRate = Mathf.Lerp(dsp.modulationRate, c.lfoRate.Evaluate(t), 0.75f);
            dsp.modulationDepth = Mathf.Lerp(dsp.modulationDepth, c.lfoDepth.Evaluate(t), 0.75f);
            dsp.reverbAmount = Mathf.Lerp(dsp.reverbAmount, c.dryWet.Evaluate(t), 0.85f);
            if (c.waveShape == InstrumentWaveShape.Square || c.waveShape == InstrumentWaveShape.Saw)
                dsp.filterResonance = Mathf.Clamp01(dsp.filterResonance + c.pwmWidth.Evaluate(t) * 0.25f);
            return dsp;
        }

        string SuggestControlId() => deviceKind switch
        {
            ElectronicDeviceKind.DrumMachine => ElectronicOptionKeys.DrumMachine,
            ElectronicDeviceKind.Dac => ElectronicOptionKeys.Dac,
            ElectronicDeviceKind.DigitalFxRack => ElectronicOptionKeys.DigitalFx,
            _ => ElectronicOptionKeys.DryWet
        };
    }
}
