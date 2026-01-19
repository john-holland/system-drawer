using UnityEngine;
using Weather;
using Locomotion.Senses;

namespace Locomotion.Smell
{
    /// <summary>
    /// Placeholder for a higher-fidelity smell propagation solver.
    /// Intended direction:
    /// - Use queued/advection steps driven by the Weather wind field (and optionally the WeatherPhysicsManifold)
    /// - Optionally incorporate obstacle-aware transport using hierarchical pathing / voxel/oct-tree occupancy
    ///
    /// MVP smell is implemented in `Locomotion.Senses.SmellSensor` and does not depend on this class yet.
    /// </summary>
    public class HighDefinitionSmellSolver : MonoBehaviour
    {
        [Tooltip("Optional reference to WeatherSystem for access to Wind and manifold.")]
        public WeatherSystem weatherSystem;

        [Tooltip("If set, overrides WeatherSystem's wind reference.")]
        public Wind wind;

        [Tooltip("Simulation step interval (seconds).")]
        public float stepInterval = 0.1f;

        [Header("Puff Model (MVP HD)")]
        [Tooltip("How many puffs to keep per emitter.")]
        public int maxPuffsPerEmitter = 24;

        [Tooltip("How quickly puff intensity decays per second.")]
        public float decayPerSecond = 0.15f;

        [Tooltip("Diffusion/random-walk strength (meters per second).")]
        public float diffusion = 0.25f;

        [Tooltip("How far a puff contributes when sampling concentration.")]
        public float puffRadius = 2.0f;

        private float lastStepTime;

        private struct Puff
        {
            public Vector3 position;
            public float intensity;
            public string signature;
        }

        private readonly System.Collections.Generic.Dictionary<SmellEmitter, System.Collections.Generic.List<Puff>> puffs
            = new System.Collections.Generic.Dictionary<SmellEmitter, System.Collections.Generic.List<Puff>>();

        private void Awake()
        {
            if (weatherSystem == null)
                weatherSystem = FindObjectOfType<WeatherSystem>();

            if (wind == null && weatherSystem != null)
                wind = weatherSystem.wind;
        }

        private void Update()
        {
            if (stepInterval <= 0f)
                return;

            if (Time.time - lastStepTime < stepInterval)
                return;

            StepSimulation(stepInterval);
            lastStepTime = Time.time;
        }

        /// <summary>
        /// Sample a scalar concentration (unitless) around a position.
        /// </summary>
        public float SampleConcentration(Vector3 position, float radius, string signatureFilter = null)
        {
            float r = Mathf.Max(0.01f, radius);
            float r2 = r * r;
            float sum = 0f;

            foreach (var kvp in puffs)
            {
                var list = kvp.Value;
                for (int i = 0; i < list.Count; i++)
                {
                    Puff p = list[i];
                    if (!string.IsNullOrEmpty(signatureFilter) && p.signature != signatureFilter)
                        continue;

                    float d2 = (p.position - position).sqrMagnitude;
                    if (d2 > r2)
                        continue;

                    // Smooth-ish falloff
                    float t = 1f - Mathf.Clamp01(d2 / r2);
                    sum += p.intensity * t;
                }
            }

            return sum;
        }

        private void StepSimulation(float deltaTime)
        {
            // MVP HD model: maintain and advect a set of puffs for each SmellEmitter.
            // Later: Semi-Lagrangian field advection + obstacle interaction + manifold coupling.

            // Collect active emitters
            var emitters = FindObjectsOfType<SmellEmitter>();
            for (int e = 0; e < emitters.Length; e++)
            {
                var emitter = emitters[e];
                if (emitter == null || !emitter.isActiveAndEnabled)
                    continue;

                if (!puffs.TryGetValue(emitter, out var list))
                {
                    list = new System.Collections.Generic.List<Puff>(maxPuffsPerEmitter);
                    puffs[emitter] = list;
                }

                // Add a new puff at the emitter each step
                if (list.Count >= Mathf.Max(1, maxPuffsPerEmitter))
                    list.RemoveAt(0);

                list.Add(new Puff
                {
                    position = emitter.transform.position,
                    intensity = emitter.GetEffectiveIntensity(),
                    signature = emitter.signature
                });
            }

            // Advect and decay all puffs
            var toRemove = new System.Collections.Generic.List<SmellEmitter>();
            foreach (var kvp in puffs)
            {
                var emitter = kvp.Key;
                var list = kvp.Value;
                if (emitter == null || list == null)
                {
                    toRemove.Add(emitter);
                    continue;
                }

                for (int i = 0; i < list.Count; i++)
                {
                    Puff p = list[i];

                    // Wind advection (use Weather.Wind if present)
                    Vector3 windVec = Vector3.zero;
                    if (wind != null)
                    {
                        windVec = wind.GetWindAtPosition(p.position, p.position.y);
                    }

                    // Diffusion as random walk
                    Vector3 jitter = Random.insideUnitSphere * (diffusion * deltaTime);
                    jitter.y *= 0.25f; // keep mostly horizontal

                    p.position += windVec * deltaTime + jitter;
                    p.intensity *= Mathf.Clamp01(1f - decayPerSecond * deltaTime);

                    list[i] = p;
                }

                // Trim very weak puffs
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].intensity < 0.001f)
                        list.RemoveAt(i);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                puffs.Remove(toRemove[i]);
            }
        }
    }
}

