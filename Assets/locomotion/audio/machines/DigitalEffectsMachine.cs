using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    public enum DigitalEffectKind
    {
        Passthrough,
        LowPass,
        Delay,
        Reverb,
        Distortion,
        LfoMod,
        Pwm,
        Loop
    }

    [Serializable]
    public sealed class DigitalEffectNode
    {
        public string id = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string label = "Effect";
        public DigitalEffectKind kind = DigitalEffectKind.Passthrough;
        [Range(0f, 1f)] public float dryWet = 1f;
        public float paramA = 0.5f;
        public float paramB = 0.5f;
        public string loopMachineId;
        public float loopSpeed = 1f;
        public float loopLength = 1f;
        public float loopRate = 1f;
        public float loopVolume = 1f;
        public MusicScaleMode scaleMode = MusicScaleMode.Ionian;
        [Range(0, 11)] public int keyTonic;
        public bool oscillation;
        public float lfoRate = 1f;
        public float lfoDepth = 0.2f;
        public float pwmWidth = 0.5f;
    }

    /// <summary>
    /// Physical digital effects rack: component and spawnable GameObject type.
    /// Ordered DSP graph with insert before/after semantics.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DigitalEffectsMachine : MonoBehaviour
    {
        public string machineId = "digital-fx";
        [Tooltip("Digital racks / drum machines identify as Electronic (dry/wet, LFO, PWM).")]
        public InstrumentFamily family = InstrumentFamily.Electronic;
        public ElectronicDeviceKind deviceKind = ElectronicDeviceKind.DigitalFxRack;
        public bool enforceTraditionalDefaults = true;
        public InstrumentProfileCurves profileCurves;
        public AudioPowerBudget powerBudget;
        public List<DigitalEffectNode> graph = new List<DigitalEffectNode>();
        public AudioEquipmentTrace equipmentTrace = new AudioEquipmentTrace();

        [Header("Power draw estimate")]
        public float baseWatts = 40f;
        public float wattsPerNode = 8f;

        public bool UnrealisticWarning => powerBudget != null && powerBudget.UnrealisticOutputQualityWarning;

        void OnEnable()
        {
            family = InstrumentFamily.Electronic;
            if (profileCurves == null)
                profileCurves = InstrumentProfileCurves.CreateRuntimeDefault(InstrumentFamily.Electronic);
            else
                profileCurves.family = InstrumentFamily.Electronic;
            EnsureTraceRoot();
            PublishPower();
        }

        void OnDisable()
        {
            if (powerBudget != null)
                powerBudget.Unregister(machineId);
        }

        void EnsureTraceRoot()
        {
            equipmentTrace ??= new AudioEquipmentTrace();
            if (equipmentTrace.root == null)
            {
                equipmentTrace.root = new AudioEquipmentTraceNode
                {
                    id = machineId,
                    label = gameObject.name,
                    kind = AudioEquipmentLaneKind.Physical,
                    machineComponentId = machineId
                };
            }
        }

        public void InsertBefore(string selectedEffectId, DigitalEffectNode node)
        {
            if (node == null) return;
            int idx = IndexOf(selectedEffectId);
            if (idx < 0) graph.Add(node);
            else graph.Insert(idx, node);
            SyncTraceInsert(selectedEffectId, node, before: true);
            PublishPower();
        }

        public void InsertAfter(string selectedEffectId, DigitalEffectNode node)
        {
            if (node == null) return;
            int idx = IndexOf(selectedEffectId);
            if (idx < 0) graph.Add(node);
            else graph.Insert(idx + 1, node);
            SyncTraceInsert(selectedEffectId, node, before: false);
            PublishPower();
        }

        public DSPParams Process(DSPParams input)
        {
            if (input == null) return new DSPParams();
            var dsp = CloneDsp(input);
            for (int i = 0; i < graph.Count; i++)
            {
                var n = graph[i];
                if (n == null) continue;
                float wet = Mathf.Clamp01(n.dryWet);
                switch (n.kind)
                {
                    case DigitalEffectKind.LowPass:
                        dsp.filterCutoff = Mathf.Lerp(dsp.filterCutoff, Mathf.Lerp(200f, 12000f, n.paramA), wet);
                        dsp.filterResonance = Mathf.Lerp(dsp.filterResonance, n.paramB, wet);
                        break;
                    case DigitalEffectKind.Delay:
                        dsp.delayTime = Mathf.Lerp(dsp.delayTime, n.paramA, wet);
                        dsp.delayFeedback = Mathf.Lerp(dsp.delayFeedback, n.paramB, wet);
                        break;
                    case DigitalEffectKind.Reverb:
                        dsp.reverbAmount = Mathf.Lerp(dsp.reverbAmount, n.paramA, wet);
                        break;
                    case DigitalEffectKind.LfoMod:
                    case DigitalEffectKind.Pwm:
                        if (n.oscillation || n.kind == DigitalEffectKind.LfoMod)
                        {
                            dsp.modulationRate = Mathf.Lerp(dsp.modulationRate, n.lfoRate, wet);
                            dsp.modulationDepth = Mathf.Lerp(dsp.modulationDepth, n.lfoDepth, wet);
                        }
                        break;
                    case DigitalEffectKind.Distortion:
                        dsp.filterResonance = Mathf.Clamp01(dsp.filterResonance + n.paramA * wet * 0.5f);
                        break;
                    case DigitalEffectKind.Loop:
                        dsp.amplitudeEnvelope = new Vector4(
                            dsp.amplitudeEnvelope.x,
                            dsp.amplitudeEnvelope.y,
                            Mathf.Lerp(dsp.amplitudeEnvelope.z, n.loopVolume, wet),
                            Mathf.Lerp(dsp.amplitudeEnvelope.w, n.loopLength, wet));
                        break;
                }
            }
            return dsp;
        }

        void SyncTraceInsert(string selectedId, DigitalEffectNode node, bool before)
        {
            EnsureTraceRoot();
            var traceNode = new AudioEquipmentTraceNode
            {
                id = node.id,
                label = node.label,
                kind = AudioEquipmentLaneKind.DigitalTiming,
                machineComponentId = machineId
            };
            if (before) equipmentTrace.InsertBefore(selectedId, traceNode);
            else equipmentTrace.InsertAfter(selectedId, traceNode);
        }

        int IndexOf(string id)
        {
            for (int i = 0; i < graph.Count; i++)
                if (graph[i] != null && graph[i].id == id) return i;
            return -1;
        }

        void PublishPower()
        {
            if (powerBudget == null) return;
            powerBudget.Register(new AudioPowerRequirement
            {
                componentId = machineId,
                watts = baseWatts + wattsPerNode * graph.Count,
                channels = Mathf.Max(1, graph.Count),
                dspLoad01 = Mathf.Clamp01(0.05f * graph.Count)
            });
        }

        static DSPParams CloneDsp(DSPParams src)
        {
            return new DSPParams
            {
                frequencyRange = src.frequencyRange,
                baseFrequency = src.baseFrequency,
                amplitudeEnvelope = src.amplitudeEnvelope,
                modulationRate = src.modulationRate,
                modulationDepth = src.modulationDepth,
                filterCutoff = src.filterCutoff,
                filterResonance = src.filterResonance,
                reverbAmount = src.reverbAmount,
                delayTime = src.delayTime,
                delayFeedback = src.delayFeedback
            };
        }
    }
}
