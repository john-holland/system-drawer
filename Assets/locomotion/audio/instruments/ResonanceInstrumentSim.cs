using UnityEngine;

namespace Locomotion.Audio
{
    public enum ResonatorMaterialKind
    {
        Metal,
        Bone,
        Wood,
        Composite
    }

    /// <summary>Idiophone / currumba-class: material manifold + optional SDF volume thump location.</summary>
    public sealed class ResonanceInstrumentSim : PhysicalInstrumentBase
    {
        public ResonatorMaterialKind material = ResonatorMaterialKind.Wood;
        [Range(0f, 1f)] public float tune01 = 0.5f;
        public Vector3 thumpLocalPoint;

        void Reset()
        {
            if (proxy != null) proxy.family = InstrumentFamily.Resonance;
        }

        public override DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            var dsp = base.BuildVoice(controlId, raw01, bpm);
            float matBright = material switch
            {
                ResonatorMaterialKind.Metal => 1.2f,
                ResonatorMaterialKind.Bone => 1.05f,
                ResonatorMaterialKind.Wood => 0.9f,
                _ => 1f
            };
            dsp.baseFrequency *= Mathf.Lerp(0.9f, 1.1f, tune01) * matBright;
            dsp.filterResonance = Mathf.Clamp01(dsp.filterResonance + 0.2f);
            if (useSdfVolumeAnalysis)
            {
                float radial = thumpLocalPoint.magnitude;
                dsp.filterCutoff *= Mathf.Lerp(0.8f, 1.2f, Mathf.Clamp01(radial));
            }
            return dsp;
        }
    }
}
