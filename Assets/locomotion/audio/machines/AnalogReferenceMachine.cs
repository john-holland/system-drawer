using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    public enum AnalogConnectorKind
    {
        TRS,
        TS,
        TRRS,
        RCA,
        XLR,
        HdDigitalOpticalHdmi,
        Jack,
        Plug,
        Dac,
        Amp,
        Aux
    }

    [Serializable]
    public sealed class AnalogConnectorLink
    {
        public string id = Guid.NewGuid().ToString("N").Substring(0, 8);
        public AnalogConnectorKind kind = AnalogConnectorKind.TRS;
        public string fromMachineId;
        public string toMachineId;
        [Range(0f, 1f)] public float dryWet = 1f;
        public float latencyMs;
        public float colorEq = 0.5f;
        public bool loop;
        public float loopSpeed = 1f;
        public float loopLength = 1f;
        public float loopRate = 1f;
        public float loopVolume = 1f;
    }

    /// <summary>Analog reference machine: cabling/DAC/amp affecting timing and DSP color.</summary>
    public sealed class AnalogReferenceMachine : MonoBehaviour
    {
        public string machineId = "analog-ref";
        [Tooltip("DAC / amp / aux path identifies as Electronic for dry/wet and modulation options.")]
        public InstrumentFamily family = InstrumentFamily.Electronic;
        public ElectronicDeviceKind deviceKind = ElectronicDeviceKind.Dac;
        public InstrumentProfileCurves profileCurves;
        public AudioPowerBudget powerBudget;
        public List<AnalogConnectorLink> connectors = new List<AnalogConnectorLink>();
        public AudioEquipmentTrace equipmentTrace = new AudioEquipmentTrace();

        public float baseWatts = 25f;

        void OnEnable()
        {
            family = InstrumentFamily.Electronic;
            if (deviceKind == ElectronicDeviceKind.GenericElectronic)
                deviceKind = ElectronicDeviceKind.Dac;
            if (profileCurves == null)
                profileCurves = InstrumentProfileCurves.CreateRuntimeDefault(InstrumentFamily.Electronic);
            else
                profileCurves.family = InstrumentFamily.Electronic;

            equipmentTrace ??= new AudioEquipmentTrace();
            if (equipmentTrace.root != null)
            {
                equipmentTrace.root.id = machineId;
                equipmentTrace.root.label = gameObject.name;
                equipmentTrace.root.kind = AudioEquipmentLaneKind.Physical;
            }
            PublishPower();
        }

        void OnDisable()
        {
            if (powerBudget != null)
                powerBudget.Unregister(machineId);
        }

        public DSPParams ApplyThroughput(DSPParams input)
        {
            if (input == null) return new DSPParams();
            var dsp = new DSPParams
            {
                frequencyRange = input.frequencyRange,
                baseFrequency = input.baseFrequency,
                amplitudeEnvelope = input.amplitudeEnvelope,
                modulationRate = input.modulationRate,
                modulationDepth = input.modulationDepth,
                filterCutoff = input.filterCutoff,
                filterResonance = input.filterResonance,
                reverbAmount = input.reverbAmount,
                delayTime = input.delayTime,
                delayFeedback = input.delayFeedback
            };

            float totalLatency = 0f;
            for (int i = 0; i < connectors.Count; i++)
            {
                var c = connectors[i];
                if (c == null) continue;
                float wet = Mathf.Clamp01(c.dryWet);
                totalLatency += c.latencyMs * wet;
                float color = Mathf.Lerp(1f, Mathf.Lerp(0.6f, 1.4f, c.colorEq), wet);
                dsp.filterCutoff *= color;
                if (c.loop)
                {
                    dsp.amplitudeEnvelope = new Vector4(
                        dsp.amplitudeEnvelope.x,
                        dsp.amplitudeEnvelope.y,
                        Mathf.Lerp(dsp.amplitudeEnvelope.z, c.loopVolume, wet),
                        Mathf.Lerp(dsp.amplitudeEnvelope.w, c.loopLength, wet));
                    dsp.modulationRate = Mathf.Lerp(dsp.modulationRate, c.loopRate * c.loopSpeed, wet);
                }
                switch (c.kind)
                {
                    case AnalogConnectorKind.XLR:
                        dsp.filterResonance *= Mathf.Lerp(1f, 0.95f, wet);
                        break;
                    case AnalogConnectorKind.Amp:
                        dsp.amplitudeEnvelope = new Vector4(
                            dsp.amplitudeEnvelope.x,
                            dsp.amplitudeEnvelope.y,
                            Mathf.Clamp01(dsp.amplitudeEnvelope.z * Mathf.Lerp(1f, 1.2f, wet)),
                            dsp.amplitudeEnvelope.w);
                        break;
                    case AnalogConnectorKind.Dac:
                        dsp.filterCutoff = Mathf.Min(dsp.filterCutoff, Mathf.Lerp(dsp.filterCutoff, 18000f, wet));
                        break;
                    case AnalogConnectorKind.HdDigitalOpticalHdmi:
                        totalLatency += 0.5f * wet;
                        break;
                }
            }
            dsp.delayTime += totalLatency * 0.001f;
            return dsp;
        }

        void PublishPower()
        {
            if (powerBudget == null) return;
            powerBudget.Register(new AudioPowerRequirement
            {
                componentId = machineId,
                watts = baseWatts + connectors.Count * 3f,
                channels = Mathf.Max(1, connectors.Count),
                dspLoad01 = Mathf.Clamp01(0.02f * connectors.Count)
            });
        }
    }
}
