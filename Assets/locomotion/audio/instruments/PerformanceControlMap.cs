using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    [Serializable]
    public struct PerformanceControlSlot
    {
        public string controlId;
        public string articulationChannel;
        [Range(0f, 1f)] public float minStrength;
        [Range(0f, 1f)] public float maxStrength;
        public float cooldownSeconds;
    }

    /// <summary>Maps player/actor control surfaces to musical articulation channels (proxy instrumentation).</summary>
    [CreateAssetMenu(fileName = "PerformanceControlMap", menuName = "Locomotion/Audio/Performance Control Map", order = 41)]
    public sealed class PerformanceControlMap : ScriptableObject
    {
        [SerializeField] List<PerformanceControlSlot> slots = new List<PerformanceControlSlot>();

        public IReadOnlyList<PerformanceControlSlot> Slots => slots;

        public void ReplaceSlots(IReadOnlyList<PerformanceControlSlot> newSlots)
        {
            slots = newSlots != null ? new List<PerformanceControlSlot>(newSlots) : new List<PerformanceControlSlot>();
        }

        public bool TryGetSlot(string controlId, out PerformanceControlSlot slot)
        {
            slot = default;
            if (string.IsNullOrEmpty(controlId) || slots == null) return false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].controlId == controlId)
                {
                    slot = slots[i];
                    return true;
                }
            }
            return false;
        }

        public bool ChannelIsAllowed(string articulationChannel)
        {
            if (string.IsNullOrEmpty(articulationChannel) || slots == null) return false;
            for (int i = 0; i < slots.Count; i++)
            {
                if (string.Equals(slots[i].articulationChannel, articulationChannel, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        public float MapControl(string controlId, float raw01, InstrumentProfileCurves curves)
        {
            if (!TryGetSlot(controlId, out var slot))
                return 0f;
            float clamped = Mathf.Clamp01(raw01);
            float ranged = Mathf.Lerp(slot.minStrength, slot.maxStrength, clamped);
            return curves != null ? curves.EvaluateInstrumentation(ranged) : ranged;
        }
    }
}
