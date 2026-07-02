using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.DreamCycle
{
    [Serializable]
    public struct DreamMemoryFrame
    {
        public float waveSample;
        public float satisfied01;
        public int dayCollapseSeed;
        public string quadDigest;
        public double timestampUtc;
    }

    /// <summary>Ring buffer of sleep wave samples + day aspect digests for dream-memory LSTM input.</summary>
    public sealed class DreamMemoryBuffer : MonoBehaviour
    {
        public int capacity = 64;
        readonly Queue<DreamMemoryFrame> _frames = new Queue<DreamMemoryFrame>();

        public int Count => _frames.Count;

        public void Clear() => _frames.Clear();

        public void Push(DreamMemoryFrame frame)
        {
            _frames.Enqueue(frame);
            while (_frames.Count > capacity && capacity > 0)
                _frames.Dequeue();
        }

        public void PushWaveBatch(float[] samples, int dayCollapseSeed, string quadDigest)
        {
            if (samples == null || samples.Length == 0)
                return;
            double now = DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds;
            for (int i = 0; i < samples.Length; i++)
            {
                Push(new DreamMemoryFrame
                {
                    waveSample = samples[i],
                    dayCollapseSeed = dayCollapseSeed,
                    quadDigest = quadDigest ?? string.Empty,
                    timestampUtc = now + i * 0.001,
                    satisfied01 = Mathf.Clamp01(Mathf.Abs(samples[i]))
                });
            }
        }

        public bool TryPeekLatest(out DreamMemoryFrame frame)
        {
            if (_frames.Count == 0)
            {
                frame = default;
                return false;
            }
            frame = _frames.ToArray()[_frames.Count - 1];
            return true;
        }

        public DreamMemoryFrame[] Snapshot()
        {
            return _frames.ToArray();
        }
    }
}
