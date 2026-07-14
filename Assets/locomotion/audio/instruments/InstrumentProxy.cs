using System;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Proxy voice: score/player events → DSPParams for assembled pieces or bespoke control.</summary>
    public sealed class InstrumentProxy : MonoBehaviour
    {
        public string proxyVoiceId = "voice-0";
        public InstrumentFamily family = InstrumentFamily.Generic;
        public InstrumentProfileCurves profileCurves;
        public PerformanceControlMap controlMap;

        [Range(0f, 1f)]
        [Tooltip("0 = free continuous gesture, 1 = hard rhythmic/pitch grid.")]
        public float playerInteractionQuantize01 = 0.5f;

        [Range(0f, 1f)]
        [Tooltip("Blend between physical sim and ideal settings (warble ↔ autotune).")]
        public float physicalIdealBlend01 = 0.85f;

        public RealtimeAudioGenerator audioGenerator;
        public bool enforceTraditionalDefaults = true;

        public DSPParams LastDsp { get; private set; } = new DSPParams();
        public float LastArticulation01 { get; private set; }

        void Awake()
        {
            EnsureProfile();
        }

        public void EnsureProfile()
        {
            if (profileCurves == null)
            {
                profileCurves = InstrumentProfileCurves.CreateRuntimeDefault(family);
                profileCurves.enforceTraditionalDefaults = enforceTraditionalDefaults;
            }
            else
            {
                profileCurves.enforceTraditionalDefaults = enforceTraditionalDefaults;
            }
        }

        public bool TryArticulate(string controlId, float raw01, float timeSec, float bpm, out DSPParams dsp)
        {
            EnsureProfile();
            dsp = LastDsp;
            if (enforceTraditionalDefaults && profileCurves != null &&
                !profileCurves.IsOptionAllowed(controlId))
                return false;

            float qValue = PlayerInteractionQuantizer.QuantizeValue01(raw01, playerInteractionQuantize01);
            float artic = controlMap != null
                ? controlMap.MapControl(controlId, qValue, profileCurves)
                : profileCurves.EvaluateInstrumentation(qValue);

            artic = Mathf.Lerp(artic * 0.85f, artic, physicalIdealBlend01);
            LastArticulation01 = artic;
            dsp = profileCurves.ToDspParams(artic);
            LastDsp = dsp;

            if (audioGenerator != null)
                audioGenerator.GenerateFromDSP(dsp, SoundOrigin.BehaviorTree);

            return true;
        }

        public float QuantizeEventTime(float timeSec, float bpm) =>
            PlayerInteractionQuantizer.QuantizeTime(timeSec, bpm, playerInteractionQuantize01);
    }
}
