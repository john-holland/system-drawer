using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Senses
{
    /// <summary>
    /// Emits a smell signature into the world.
    /// Smell propagation is evaluated by Smell sensors (advected by CombinedWind).
    /// </summary>
    public class SmellEmitter : MonoBehaviour
    {
        [Header("Signature")]
        public string signature = "default";

        [Header("Strength")]
        [Tooltip("Base emission strength (unitless). Higher = detectable further.")]
        public float intensity = 1f;

        [Tooltip("Maximum effective radius in meters (before wind factors).")]
        public float radius = 10f;

        [Tooltip("Optional multiplier (e.g., opened container, bleeding, etc.).")]
        public float emissionMultiplier = 1f;

        [Header("Debug")]
        public bool drawGizmos = false;
        public Color gizmoColor = new Color(0.7f, 0.2f, 1f, 0.25f);

        private static readonly HashSet<SmellEmitter> Registry = new HashSet<SmellEmitter>();

        private void OnEnable()
        {
            Registry.Add(this);
        }

        private void OnDisable()
        {
            Registry.Remove(this);
        }

        public float GetEffectiveIntensity()
        {
            return Mathf.Max(0f, intensity) * Mathf.Max(0f, emissionMultiplier);
        }

        public float GetEffectiveRadius()
        {
            return Mathf.Max(0f, radius);
        }

        public static void GetEmittersInRange(Vector3 worldPosition, float range, List<SmellEmitter> results)
        {
            if (results == null)
                return;

            results.Clear();
            float r2 = range * range;

            foreach (var e in Registry)
            {
                if (e == null || !e.isActiveAndEnabled)
                    continue;

                float d2 = (e.transform.position - worldPosition).sqrMagnitude;
                if (d2 <= r2)
                {
                    results.Add(e);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmos)
                return;

            Gizmos.color = gizmoColor;
            Gizmos.DrawWireSphere(transform.position, GetEffectiveRadius());
        }
    }
}

