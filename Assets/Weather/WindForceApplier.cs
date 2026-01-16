using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Component that automatically applies wind forces to Rigidbodies.
    /// Can be attached to objects or used as a global system.
    /// </summary>
    public class WindForceApplier : MonoBehaviour
    {
        [Header("Wind Reference")]
        [Tooltip("Reference to Wind system (auto-finds if not set)")]
        public Wind wind;

        [Header("Configuration")]
        [Tooltip("Apply forces to all rigidbodies in scene")]
        public bool applyToAllRigidbodies = false;

        [Tooltip("Objects to apply wind to (if applyToAllRigidbodies is false)")]
        public Rigidbody[] targetRigidbodies;

        [Tooltip("Update interval (0 = every frame)")]
        public float updateInterval = 0f;

        [Tooltip("Minimum wind speed to apply forces")]
        public float minWindSpeed = 0.1f;

        private float lastUpdateTime = 0f;

        private void Awake()
        {
            if (wind == null)
            {
                wind = FindObjectOfType<Wind>();
            }
        }

        private void FixedUpdate()
        {
            if (wind == null)
                return;

            // Check update interval
            if (updateInterval > 0f)
            {
                if (Time.time - lastUpdateTime < updateInterval)
                    return;
            }

            // Apply wind forces
            if (applyToAllRigidbodies)
            {
                ApplyToAllRigidbodies();
            }
            else
            {
                ApplyToTargets();
            }

            lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Apply wind forces to all rigidbodies in scene
        /// </summary>
        private void ApplyToAllRigidbodies()
        {
            Rigidbody[] allRigidbodies = FindObjectsOfType<Rigidbody>();
            foreach (var rb in allRigidbodies)
            {
                if (rb != null && !rb.isKinematic)
                {
                    ApplyWindToRigidbody(rb);
                }
            }
        }

        /// <summary>
        /// Apply wind forces to target rigidbodies
        /// </summary>
        private void ApplyToTargets()
        {
            foreach (var rb in targetRigidbodies)
            {
                if (rb != null && !rb.isKinematic)
                {
                    ApplyWindToRigidbody(rb);
                }
            }
        }

        /// <summary>
        /// Apply wind force to a specific rigidbody
        /// </summary>
        private void ApplyWindToRigidbody(Rigidbody rb)
        {
            if (wind.speed < minWindSpeed)
                return;

            wind.ApplyWindForce(rb);
        }
    }
}
