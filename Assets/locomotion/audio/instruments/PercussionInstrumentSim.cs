using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>Percussion with precached pathing samples and hit tracking for wear.</summary>
    public sealed class PercussionInstrumentSim : PhysicalInstrumentBase
    {
        public Transform[] strikePoints;
        public int hitCount;
        [Range(0f, 1f)] public float wear01;
        public bool allowProxyVehiclePlayWhenAccuracyExceeded = true;
        public float accuracyLimit01 = 0.35f;

        readonly Dictionary<int, Vector3> _pathCache = new Dictionary<int, Vector3>();

        void Reset()
        {
            if (proxy != null) proxy.family = InstrumentFamily.Percussion;
        }

        public void PrecacheStrikePaths()
        {
            _pathCache.Clear();
            if (strikePoints == null) return;
            for (int i = 0; i < strikePoints.Length; i++)
            {
                if (strikePoints[i] != null)
                    _pathCache[i] = strikePoints[i].position;
            }
        }

        public override DSPParams BuildVoice(string controlId, float raw01, float bpm)
        {
            hitCount++;
            wear01 = Mathf.Clamp01(wear01 + 0.001f);
            var dsp = base.BuildVoice(controlId, raw01, bpm);
            dsp.filterCutoff *= Mathf.Lerp(1f, 0.85f, wear01);
            return dsp;
        }

        public bool TryProxyVehiclePlay(float considerAccuracy01)
        {
            if (!allowProxyVehiclePlayWhenAccuracyExceeded)
                return false;
            return considerAccuracy01 > accuracyLimit01;
        }

        public bool TryGetCachedStrike(int index, out Vector3 pos) =>
            _pathCache.TryGetValue(index, out pos);
    }
}
