using System;
using UnityEngine;

namespace SystemDrawer.DreamCycle
{
    [Serializable]
    public struct DayAspectStateDto
    {
        public string aspectId;
        public float satisfied01;
        public int spatialSeedHint;
        public string spatialSlotId;
    }

    /// <summary>Links one need aspect to a SpatialGenerator (TwoDimensional) and optional telecom devices.</summary>
    public sealed class NeedAspectSpatialSlot : MonoBehaviour
    {
        public string aspectId = "need_physiological";
        public MonoBehaviour spatialGenerator;
        public MonoBehaviour[] telecomDevices = Array.Empty<MonoBehaviour>();
        [Range(0f, 1f)] public float satisfied01 = 0.5f;
        public int spatialSeedHint;

        public void ApplyDayState(DayAspectStateDto state)
        {
            satisfied01 = state.satisfied01;
            spatialSeedHint = state.spatialSeedHint;
            ApplySeedToGenerator();
        }

        public void ApplySeedToGenerator()
        {
            if (spatialGenerator == null)
                return;
            var method = spatialGenerator.GetType().GetMethod("SetSeed");
            method?.Invoke(spatialGenerator, new object[] { spatialSeedHint });
            var regen = spatialGenerator.GetType().GetMethod("Regenerate");
            regen?.Invoke(spatialGenerator, null);
        }
    }
}
