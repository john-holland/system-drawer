using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Shared instrument/machine profile curves. Drives both synthesis timbre and
    /// proxy articulation response (instrumentation). Family defaults ship as presets.
    /// </summary>
    [CreateAssetMenu(fileName = "InstrumentProfileCurves", menuName = "Locomotion/Audio/Instrument Profile Curves", order = 40)]
    public sealed class InstrumentProfileCurves : ScriptableObject
    {
        public InstrumentFamily family = InstrumentFamily.Generic;

        [Tooltip("When true, inspector/runtime only exposes traditional options for this family.")]
        public bool enforceTraditionalDefaults = true;

        [Header("Timbre / envelope")]
        public AnimationCurve pitch = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
        public AnimationCurve treble = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
        public AnimationCurve bassSub = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
        public AnimationCurve length = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
        public AnimationCurve attack = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public AnimationCurve decay = AnimationCurve.Linear(0f, 1f, 1f, 0.2f);
        public AnimationCurve sustain = AnimationCurve.Linear(0f, 0.7f, 1f, 0.7f);
        public AnimationCurve volume = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve resonance = AnimationCurve.Linear(0f, 0.3f, 1f, 0.3f);
        public AnimationCurve noise = AnimationCurve.Linear(0f, 0f, 1f, 0.1f);

        [Header("Electronic (dry/wet, LFO, PWM, waves) — InstrumentFamily.Electronic")]
        [Tooltip("Mix of processed vs dry signal; traditional for Electronic (DAC, drum machine, FX).")]
        public AnimationCurve dryWet = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        public AnimationCurve modulationDepth = AnimationCurve.Linear(0f, 0f, 1f, 0.25f);
        public AnimationCurve modulationRate = AnimationCurve.Linear(0f, 0.5f, 1f, 4f);
        public AnimationCurve pwmWidth = AnimationCurve.Linear(0f, 0.5f, 1f, 0.5f);
        public AnimationCurve lfoDepth = AnimationCurve.Linear(0f, 0f, 1f, 0.2f);
        public AnimationCurve lfoRate = AnimationCurve.Linear(0f, 0.1f, 1f, 8f);
        public InstrumentWaveShape waveShape = InstrumentWaveShape.Sine;

        [Header("Theory")]
        [Range(0, 11)] public int keyTonic;
        public MusicScaleMode scaleMode = MusicScaleMode.Ionian;

        [Header("Instrumentation response (control → DSP)")]
        [Tooltip("Maps normalized control force into articulation strength.")]
        public AnimationCurve instrumentationResponse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public float EvaluateInstrumentation(float control01) =>
            instrumentationResponse != null ? instrumentationResponse.Evaluate(Mathf.Clamp01(control01)) : Mathf.Clamp01(control01);

        public bool IsOptionAllowed(string optionKey)
        {
            if (!enforceTraditionalDefaults || string.IsNullOrEmpty(optionKey))
                return true;
            return IsTraditionalOption(family, optionKey);
        }

        public static bool IsTraditionalOption(InstrumentFamily family, string optionKey)
        {
            string k = optionKey.Trim().ToLowerInvariant().Replace("_", "").Replace("-", "");

            // dry/wet, LFO, PWM, wave shapes belong to Electronic (DAC, drum machine, digital FX).
            if (ElectronicOptionKeys.IsElectronicOption(k))
                return family == InstrumentFamily.Electronic || family == InstrumentFamily.Generic;

            switch (family)
            {
                case InstrumentFamily.Strings:
                    return k is "bow" or "pluck" or "fret" or "volume" or "pitch" or "vibrato";
                case InstrumentFamily.Wind:
                    return k is "breath" or "valve" or "volume" or "pitch" or "resonance" or "attenuatedopen";
                case InstrumentFamily.FreeReed:
                    return k is "bellows" or "key" or "volume" or "attack" or "attenuatedopen";
                case InstrumentFamily.Percussion:
                    return k is "hit" or "volume" or "pitch" or "resonance";
                case InstrumentFamily.Keyboard:
                    return k is "key" or "pedal" or "volume" or "sustain" or "lid" or "fallboard";
                case InstrumentFamily.Resonance:
                    return k is "strike" or "volume" or "resonance" or "material";
                case InstrumentFamily.Electronic:
                    return ElectronicOptionKeys.IsElectronicOption(k)
                           || k is "volume" or "pitch" or "noise" or "filter" or "delay" or "reverb"
                               or "loop" or "pad" or "sequencer" or "sample";
                default:
                    return true;
            }
        }

        public static InstrumentProfileCurves CreateRuntimeDefault(InstrumentFamily family)
        {
            var curves = CreateInstance<InstrumentProfileCurves>();
            curves.family = family;
            curves.enforceTraditionalDefaults = true;
            ApplyFamilyDefaults(curves);
            return curves;
        }

        public static void ApplyFamilyDefaults(InstrumentProfileCurves c)
        {
            if (c == null) return;
            switch (c.family)
            {
                case InstrumentFamily.Strings:
                    c.attack = AnimationCurve.EaseInOut(0f, 0f, 0.15f, 1f);
                    c.decay = AnimationCurve.Linear(0f, 1f, 1f, 0.15f);
                    c.resonance = AnimationCurve.Linear(0f, 0.45f, 1f, 0.45f);
                    c.instrumentationResponse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                    break;
                case InstrumentFamily.Wind:
                    c.attack = AnimationCurve.Linear(0f, 0f, 0.35f, 1f);
                    c.bassSub = AnimationCurve.Linear(0f, 0.6f, 1f, 0.6f);
                    c.instrumentationResponse = new AnimationCurve(
                        new Keyframe(0f, 0f), new Keyframe(0.4f, 0.25f), new Keyframe(1f, 1f));
                    break;
                case InstrumentFamily.FreeReed:
                    c.attack = AnimationCurve.Linear(0f, 0.1f, 0.5f, 1f);
                    c.volume = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                    c.instrumentationResponse = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                    break;
                case InstrumentFamily.Percussion:
                    c.attack = AnimationCurve.Linear(0f, 1f, 0.05f, 1f);
                    c.decay = AnimationCurve.EaseInOut(0f, 1f, 0.4f, 0f);
                    c.sustain = AnimationCurve.Linear(0f, 0f, 1f, 0f);
                    c.instrumentationResponse = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                    break;
                case InstrumentFamily.Keyboard:
                    c.attack = AnimationCurve.Linear(0f, 0f, 0.08f, 1f);
                    c.sustain = AnimationCurve.Linear(0f, 0.8f, 1f, 0.8f);
                    c.instrumentationResponse = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
                    break;
                case InstrumentFamily.Resonance:
                    c.resonance = AnimationCurve.Linear(0f, 0.7f, 1f, 0.7f);
                    c.decay = AnimationCurve.Linear(0f, 1f, 1f, 0.3f);
                    break;
                case InstrumentFamily.Electronic:
                    c.dryWet = AnimationCurve.Linear(0f, 0.35f, 1f, 1f);
                    c.lfoDepth = AnimationCurve.Linear(0f, 0.1f, 1f, 0.55f);
                    c.lfoRate = AnimationCurve.Linear(0f, 0.25f, 1f, 12f);
                    c.pwmWidth = AnimationCurve.Linear(0f, 0.25f, 1f, 0.75f);
                    c.modulationDepth = AnimationCurve.Linear(0f, 0.15f, 1f, 0.6f);
                    c.modulationRate = AnimationCurve.Linear(0f, 1f, 1f, 8f);
                    c.waveShape = InstrumentWaveShape.Square;
                    c.attack = AnimationCurve.Linear(0f, 0f, 0.05f, 1f);
                    c.instrumentationResponse = AnimationCurve.Linear(0f, 0f, 1f, 1f);
                    break;
            }
        }

        public DSPParams ToDspParams(float control01)
        {
            float t = Mathf.Clamp01(control01);
            float artic = EvaluateInstrumentation(t);
            return new DSPParams
            {
                baseFrequency = Mathf.Lerp(110f, 880f, pitch.Evaluate(t)),
                amplitudeEnvelope = new Vector4(
                    Mathf.Max(0.001f, attack.Evaluate(t) * 0.5f),
                    Mathf.Max(0.001f, decay.Evaluate(t) * 0.5f),
                    sustain.Evaluate(t),
                    Mathf.Max(0.001f, length.Evaluate(t) * 0.5f)),
                modulationRate = modulationRate.Evaluate(t),
                modulationDepth = modulationDepth.Evaluate(t) * artic,
                filterCutoff = Mathf.Lerp(400f, 12000f, treble.Evaluate(t)),
                filterResonance = resonance.Evaluate(t),
                reverbAmount = dryWet.Evaluate(t) * 0.5f
            };
        }
    }
}
