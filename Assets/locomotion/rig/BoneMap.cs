using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Rig
{
    /// <summary>
    /// Editor/runtime-friendly bone map representation using a serializable list.
    /// This is intentionally trait-based (not Humanoid-only), so vehicles/buildings can participate.
    /// </summary>
    public class BoneMap : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            [Tooltip("Stable trait id, e.g. 'Human:Head' or 'Vehicle:WheelFL'")]
            public string traitId;

            public Transform transform;
        }

        public List<Entry> entries = new List<Entry>();

        public bool TryGet(string traitId, out Transform t)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null && e.traitId == traitId)
                {
                    t = e.transform;
                    return t != null;
                }
            }

            t = null;
            return false;
        }

        public void Set(string traitId, Transform t)
        {
            if (string.IsNullOrWhiteSpace(traitId))
                return;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e != null && e.traitId == traitId)
                {
                    e.transform = t;
                    return;
                }
            }

            entries.Add(new Entry { traitId = traitId, transform = t });
        }
    }
}

