using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Shared physical instrument fields: warble/autotune blend, dry/wet, optional case + attenuated open.</summary>
    public abstract class PhysicalInstrumentBase : MonoBehaviour
    {
        public InstrumentProxy proxy;
        public InstrumentProfileCurves profileOverride;
        [Range(0f, 1f)] public float physicalIdealBlend01 = 0.85f;
        [Range(0f, 1f)] public float sectionDryWet = 0.5f;
        public bool useSdfVolumeAnalysis;
        public InstrumentCaseTopology caseTopology;
        public AttenuatedOpenClose attenuatedOpen;

        protected virtual void Awake()
        {
            if (proxy == null)
                proxy = GetComponent<InstrumentProxy>();
            if (proxy != null)
            {
                proxy.physicalIdealBlend01 = physicalIdealBlend01;
                if (profileOverride != null)
                    proxy.profileCurves = profileOverride;
                proxy.EnsureProfile();
            }
        }

        public virtual DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            if (proxy == null)
                return new DSPParams();
            proxy.physicalIdealBlend01 = physicalIdealBlend01;
            proxy.TryArticulate(controlId, raw01, Time.time, bpm, out var dsp);
            if (attenuatedOpen != null)
                attenuatedOpen.ApplyToDsp(dsp);
            dsp.reverbAmount = Mathf.Lerp(dsp.reverbAmount, sectionDryWet, 0.5f);
            return dsp;
        }
    }
}
