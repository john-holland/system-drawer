using System;
using System.Text;
using UnityEngine;

namespace Locomotion.DreamCycle
{
    [Serializable]
    public struct DreamFragment
    {
        public string label;
        public string narrativeText;
        public float confidence;
        public double timestampUtc;
        public bool isDreamMemory;
        public float distanceFromBed;
        public DreamUnwrapMode unwrapMode;
        [Range(0f, 1f)] public float improbability01;
        [Range(0f, 1f)] public float fidelity01;
    }

    /// <summary>
    /// Wraps LSTMPredictor for dream-memory reconstruction (non-authoritative for physics).
    /// v1: deterministic replay from buffer; ONNX hook later.
    /// Full developer narrative always runs; play improbability only shapes unwrap mode.
    /// </summary>
    public sealed class DreamMemoryLSTM : MonoBehaviour
    {
        public LSTMPredictor predictor;
        public DreamMemoryBuffer buffer;
        public Brain brain;
        public DreamSafeRefrainSettings safeRefrain = DreamSafeRefrainSettings.Default;
        public int dreamSeed;
        public bool dreamMemoryMode = true;
        public bool recallRemOnly = true;

        [Header("Play improbability (floats + unwrap mode)")]
        public PlayImprobabilityAudit playImprobability;
        public bool hasPlayImprobability;

        public void SetPlayImprobability(PlayImprobabilityAudit audit)
        {
            playImprobability = audit;
            hasPlayImprobability = audit.unwrapMode != DreamUnwrapMode.None;
        }

        public void ClearPlayImprobability()
        {
            playImprobability = default;
            hasPlayImprobability = false;
        }

        public void EncodeDreamMemory()
        {
            if (predictor == null || buffer == null)
                return;
            var frames = recallRemOnly ? buffer.SnapshotRemOnly() : buffer.Snapshot();
            if (frames.Length == 0)
                return;
            predictor.dreamMemoryMode = true;
            predictor.UpdateWithState(null);
        }

        public DreamFragment RecallDreamFragment()
        {
            var settings = safeRefrain.refrainLabel != null && safeRefrain.refrainLabel.Length > 0
                ? safeRefrain
                : DreamSafeRefrainSettings.Default;

            var fragment = new DreamFragment
            {
                label = settings.refrainLabel,
                isDreamMemory = true,
                confidence = predictor != null ? predictor.GetConfidence() : 0.5f,
                timestampUtc = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
            };

            var frames = recallRemOnly && buffer != null ? buffer.SnapshotRemOnly() : buffer?.Snapshot();
            if (frames == null || frames.Length == 0)
            {
                fragment.narrativeText = "Empty dream buffer.";
                return ApplyRefrain(fragment, settings);
            }

            var latest = frames[frames.Length - 1];
            var sb = new StringBuilder();
            sb.Append("Recalled wave t=").Append(latest.timestampUtc.ToString("F2"));
            sb.Append(" seed=").Append(latest.dayCollapseSeed);
            if (latest.goodDayCollapseSeed > 0)
                sb.Append(" good=").Append(latest.goodDayCollapseSeed);
            if (!string.IsNullOrEmpty(latest.quadDigest))
                sb.Append(" digest=").Append(latest.quadDigest);
            sb.Append(" layer=").Append(latest.dreamLayer);
            sb.Append(" sample=").Append(latest.waveSample.ToString("F3"));

            if (hasPlayImprobability)
            {
                sb.Append(" unwrap=").Append(playImprobability.unwrapMode);
                sb.Append(" fidelity=").Append(playImprobability.fidelity01.ToString("F2"));
                if (playImprobability.unwrapMode == DreamUnwrapMode.EscapismPreview)
                    sb.Append(" [preview density]");
                else if (playImprobability.unwrapMode == DreamUnwrapMode.PlayThoughtUnpack)
                    sb.Append(" [play thought]");
            }

            if (predictor != null && predictor.model != null)
                sb.Append(" [ONNX stub]");

            fragment.narrativeText = sb.ToString();
            fragment = ApplyRefrain(fragment, settings);
            LogThought(fragment);
            return fragment;
        }

        DreamFragment ApplyRefrain(DreamFragment fragment, DreamSafeRefrainSettings settings)
        {
            if (hasPlayImprobability)
                return DreamSafeRefrain.Apply(fragment, buffer, settings, playImprobability);
            return DreamSafeRefrain.Apply(fragment, buffer, settings);
        }

        void LogThought(DreamFragment fragment)
        {
            if (brain == null)
                return;
            var thought = new ThoughtData(brain, brain, ThoughtType.Alert, new AlertThoughtPayload
            {
                message = fragment.narrativeText,
                severity = fragment.confidence
            });
            brain.ReceiveThought(brain, thought);
        }

        public void ApplyDreamSeed(int seed)
        {
            dreamSeed = seed;
            if (predictor != null)
                predictor.dreamSeed = seed;
        }
    }
}
