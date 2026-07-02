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
    }

    /// <summary>
    /// Wraps LSTMPredictor for dream-memory reconstruction (non-authoritative for physics).
    /// v1: deterministic replay from buffer; ONNX hook later.
    /// </summary>
    public sealed class DreamMemoryLSTM : MonoBehaviour
    {
        public LSTMPredictor predictor;
        public DreamMemoryBuffer buffer;
        public Brain brain;
        public int dreamSeed;
        public bool dreamMemoryMode = true;

        public void EncodeDreamMemory()
        {
            if (predictor == null || buffer == null)
                return;
            var frames = buffer.Snapshot();
            if (frames.Length == 0)
                return;
            predictor.dreamMemoryMode = true;
            predictor.UpdateWithState(null);
        }

        public DreamFragment RecallDreamFragment()
        {
            var fragment = new DreamFragment
            {
                label = "dream memory (non-authoritative)",
                isDreamMemory = true,
                confidence = predictor != null ? predictor.GetConfidence() : 0.5f,
                timestampUtc = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds
            };

            if (buffer == null || !buffer.TryPeekLatest(out var latest))
            {
                fragment.narrativeText = "Empty dream buffer.";
                return fragment;
            }

            var sb = new StringBuilder();
            sb.Append("Recalled wave t=").Append(latest.timestampUtc.ToString("F2"));
            sb.Append(" seed=").Append(latest.dayCollapseSeed);
            if (!string.IsNullOrEmpty(latest.quadDigest))
                sb.Append(" digest=").Append(latest.quadDigest);
            sb.Append(" sample=").Append(latest.waveSample.ToString("F3"));

            if (predictor != null && predictor.model != null)
            {
                sb.Append(" [ONNX stub]");
            }

            fragment.narrativeText = sb.ToString();
            LogThought(fragment);
            return fragment;
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
