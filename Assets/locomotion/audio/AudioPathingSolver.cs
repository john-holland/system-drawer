using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Computes a lightweight occlusion/echo heuristic between sound sources and ears.
    /// MVP strategy:
    /// - direct LOS ray(s) to estimate transmission through blockers
    /// - "fuzzy path" by sampling a small set of offset rays; better rays imply sound bleeding around edges
    /// - echo estimate from variability across samples and "trackback" count
    ///
    /// This is designed to be cheap, cache-friendly, and compatible with later hierarchical pathing integration.
    /// </summary>
    public class AudioPathingSolver : MonoBehaviour
    {
        public static AudioPathingSolver Instance { get; private set; }

        [Header("Occlusion Raycasts")]
        public LayerMask occluderMask = ~0;
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        public int maxRaycastHits = 32;

        [Header("Fuzzy Path Sampling")]
        [Tooltip("Number of offset rays to sample when direct path is occluded.")]
        [Range(0, 12)]
        public int fuzzySampleCount = 6;

        [Tooltip("Offset radius for fuzzy rays (meters).")]
        public float fuzzyOffsetRadius = 0.35f;

        [Header("Echo Heuristic")]
        [Tooltip("Minimum number of improved samples (trackbacks) to enable echo.")]
        public int minTrackbacksForEcho = 2;

        [Tooltip("Standard deviation threshold across sample transmissions to enable echo.")]
        public float transmissionStdDevForEcho = 0.12f;

        [Tooltip("Scales echo strength when echo is enabled.")]
        public float echoStrengthScale = 1f;

        [Header("Heuristic Clamps / Thresholds")]
        [Tooltip("If transmission drops below this threshold while traversing occluders, stop accumulating hits early.")]
        [Range(0f, 1f)]
        public float silenceTransmissionThreshold = 0.02f;

        [Tooltip("How much better an offset sample must be than direct to count as a trackback/improvement.")]
        [Range(0f, 1f)]
        public float trackbackImprovementThreshold = 0.05f;

        [Tooltip("Clamp range for material-derived transmission factors to keep results playable.")]
        [Range(0f, 1f)]
        public float minMaterialTransmission = 0.15f;

        [Range(0f, 1f)]
        public float maxMaterialTransmission = 0.95f;

        [Tooltip("Fallback transmission factor when no material/override is present.")]
        [Range(0f, 1f)]
        public float defaultTransmissionFactor = 0.6f;

        [Header("AudioSource Effects (optional)")]
        public bool allowUnityAudioEffects = false;
        public float lowPassCutoffMin = 800f;
        public float lowPassCutoffMax = 22000f;

        [Header("Optional Hierarchical Pathing Hook")]
        [Tooltip("If set, this solver will listen for runtime space updates.")]
        public HierarchicalPathingSolver hierarchicalPathingSolver;

        private readonly HashSet<Ears> registeredEars = new HashSet<Ears>();
        private readonly List<AudioSource> cachedSources = new List<AudioSource>(128);

        private RaycastHit[] raycastHits;
        private float lastSourceScanTime = -999f;
        private const float SourceScanInterval = 0.5f;

        [Header("Debug")]
        [Tooltip("If enabled, records the last query inputs/results for inspection.")]
        public bool recordLastQuery = false;

        [SerializeField] private Vector3 lastQuerySourcePos;
        [SerializeField] private Vector3 lastQueryListenerPos;
        [SerializeField] private AudioPathResult lastQueryResult;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            raycastHits = new RaycastHit[Mathf.Max(1, maxRaycastHits)];
        }

        private void OnEnable()
        {
            if (hierarchicalPathingSolver == null)
                hierarchicalPathingSolver = FindObjectOfType<HierarchicalPathingSolver>();

            if (hierarchicalPathingSolver != null)
                hierarchicalPathingSolver.Rebuilt += HandleHierarchicalPathingRebuilt;
        }

        private void OnDisable()
        {
            if (hierarchicalPathingSolver != null)
                hierarchicalPathingSolver.Rebuilt -= HandleHierarchicalPathingRebuilt;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void HandleHierarchicalPathingRebuilt()
        {
            // Placeholder hook. In later phases this is where we'd:
            // - refresh cached occluder sets / portals
            // - rebuild audio traversal caches between common listeners/sources
            //
            // MVP: invalidate cached AudioSource list so any newly-added/removed sources are picked up quickly
            // after significant scene topology changes.
            cachedSources.Clear();
            lastSourceScanTime = -999f;
        }

        public void RegisterEar(Ears ear)
        {
            if (ear != null)
                registeredEars.Add(ear);
        }

        public void UnregisterEar(Ears ear)
        {
            if (ear != null)
                registeredEars.Remove(ear);
        }

        public struct AudioPathResult
        {
            public float transmission;      // 0..1 (higher = clearer)
            public int occluderCount;       // number of blockers hit on best sample
            public int trackbacks;          // number of samples that significantly improve on direct
            public float transmissionStdDev;
            public bool echoEnabled;
            public float echoStrength;      // 0..1 (heuristic)
        }

        public AudioPathResult ComputeTransmission(Vector3 sourcePos, Vector3 listenerPos)
        {
            Vector3 dir = listenerPos - sourcePos;
            float distance = dir.magnitude;
            if (distance <= 0.0001f)
            {
                AudioPathResult r0 = new AudioPathResult
                {
                    transmission = 1f,
                    occluderCount = 0,
                    trackbacks = 0,
                    transmissionStdDev = 0f,
                    echoEnabled = false,
                    echoStrength = 0f
                };
                RecordLastQuery(sourcePos, listenerPos, r0);
                return r0;
            }

            dir /= distance;

            // Direct path
            (float directTransmission, int directOccluders) = EvaluateRay(sourcePos, listenerPos, dir, distance);
            float bestTransmission = directTransmission;
            int bestOccluders = directOccluders;

            int trackbacks = 0;
            float[] sampleTransmissions = fuzzySampleCount > 0 ? new float[fuzzySampleCount + 1] : null;
            if (sampleTransmissions != null)
                sampleTransmissions[0] = directTransmission;

            if (fuzzySampleCount > 0 && directOccluders > 0)
            {
                // Construct basis perpendicular to dir
                Vector3 right = Vector3.Cross(dir, Vector3.up);
                // If the ray is (nearly) parallel to up, that cross product degenerates to ~0.
                // Fall back to a different reference axis to build a stable perpendicular basis.
                if (right.sqrMagnitude < 0.0001f)
                    right = Vector3.Cross(dir, Vector3.forward);
                right.Normalize();
                Vector3 up = Vector3.Cross(right, dir).normalized;

                Vector3[] offsets = BuildOffsets(right, up, fuzzyOffsetRadius, fuzzySampleCount);

                int idx = 1;
                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector3 o = offsets[i];
                    Vector3 s = sourcePos + o;
                    Vector3 l = listenerPos + o;
                    (float t, int occ) = EvaluateRay(s, l, dir, distance);

                    if (sampleTransmissions != null && idx < sampleTransmissions.Length)
                        sampleTransmissions[idx++] = t;

                    if (t > bestTransmission)
                    {
                        bestTransmission = t;
                        bestOccluders = occ;
                    }

                    if (t > directTransmission + trackbackImprovementThreshold)
                        trackbacks++;
                }
            }

            float stdDev = sampleTransmissions != null ? StdDev(sampleTransmissions) : 0f;
            bool echo = (trackbacks >= minTrackbacksForEcho) && (stdDev >= transmissionStdDevForEcho);
            float echoStrength = echo ? Mathf.Clamp01((stdDev / Mathf.Max(0.0001f, transmissionStdDevForEcho)) - 1f) * echoStrengthScale : 0f;

            AudioPathResult r = new AudioPathResult
            {
                transmission = Mathf.Clamp01(bestTransmission),
                occluderCount = bestOccluders,
                trackbacks = trackbacks,
                transmissionStdDev = stdDev,
                echoEnabled = echo,
                echoStrength = Mathf.Clamp01(echoStrength)
            };
            RecordLastQuery(sourcePos, listenerPos, r);
            return r;
        }

        private void RecordLastQuery(Vector3 sourcePos, Vector3 listenerPos, AudioPathResult result)
        {
            if (!recordLastQuery)
                return;

            lastQuerySourcePos = sourcePos;
            lastQueryListenerPos = listenerPos;
            lastQueryResult = result;
        }

        public void ApplyUnityAudioEffects(AudioSource src, AudioPathResult result)
        {
            if (!allowUnityAudioEffects || src == null)
                return;

            // NOTE: Unity audio filters are per-AudioSource, not per-listener.
            // This is useful for single-listener setups or debugging.
            float t = Mathf.Clamp01(result.transmission);

            var lowPass = src.GetComponent<AudioLowPassFilter>();
            if (lowPass == null)
                lowPass = src.gameObject.AddComponent<AudioLowPassFilter>();

            lowPass.cutoffFrequency = Mathf.Lerp(lowPassCutoffMin, lowPassCutoffMax, t);

            var echo = src.GetComponent<AudioEchoFilter>();
            if (result.echoEnabled || (echo != null && echo.enabled))
            {
                if (echo == null)
                    echo = src.gameObject.AddComponent<AudioEchoFilter>();

                echo.enabled = result.echoEnabled;
                if (result.echoEnabled)
                {
                    // Mild, gamey canyon-ish echo.
                    echo.delay = Mathf.Lerp(60f, 180f, result.echoStrength);
                    echo.decayRatio = Mathf.Lerp(0.1f, 0.6f, result.echoStrength);
                    echo.wetMix = Mathf.Lerp(0.0f, 0.6f, result.echoStrength);
                    echo.dryMix = 1f;
                }
            }
        }

        public IReadOnlyList<AudioSource> GetActiveAudioSources()
        {
            RefreshSourcesIfNeeded();
            return cachedSources;
        }

        private void RefreshSourcesIfNeeded()
        {
            if (Time.time - lastSourceScanTime < SourceScanInterval && cachedSources.Count > 0)
                return;

            cachedSources.Clear();
            cachedSources.AddRange(FindObjectsOfType<AudioSource>());
            lastSourceScanTime = Time.time;
        }

        private (float transmission, int occluders) EvaluateRay(Vector3 sourcePos, Vector3 listenerPos, Vector3 dir, float distance)
        {
            EnsureHitBuffer();

            int hitCount = Physics.RaycastNonAlloc(sourcePos, dir, raycastHits, distance, occluderMask, triggerInteraction);
            if (hitCount <= 0)
                return (1f, 0);

            Array.Sort(raycastHits, 0, hitCount, RaycastHitDistanceComparer.Instance);

            float transmission = 1f;
            int occluders = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Collider c = raycastHits[i].collider;
                if (c == null)
                    continue;

                float factor = GetTransmissionFactor(c);
                transmission *= factor;
                occluders++;

                // Early-out if almost silent.
                if (transmission < silenceTransmissionThreshold)
                    break;
            }

            return (Mathf.Clamp01(transmission), occluders);
        }

        private float GetTransmissionFactor(Collider c)
        {
            // AcousticMaterial override
            var acoustic = c.GetComponentInParent<AcousticMaterial>();
            if (acoustic != null)
                return Mathf.Clamp(acoustic.transmission, minMaterialTransmission, maxMaterialTransmission);

            // Heuristic from PhysicMaterial (friction/bounciness as a rough proxy).
            PhysicMaterial pm = c.sharedMaterial;
            if (pm != null)
            {
                float friction = Mathf.Clamp01((pm.dynamicFriction + pm.staticFriction) * 0.5f);
                float bouncy = Mathf.Clamp01(pm.bounciness);

                // Higher friction + lower bounciness tends to mean “softer”, more absorbent.
                float absorb = Mathf.Clamp01(friction * 0.7f + (1f - bouncy) * 0.3f);
                float transmit = 1f - absorb;

                // Clamp to keep the game playable.
                return Mathf.Clamp(transmit, minMaterialTransmission, maxMaterialTransmission);
            }

            // Default: moderately occluding.
            return Mathf.Clamp(defaultTransmissionFactor, 0f, 1f);
        }

        private void EnsureHitBuffer()
        {
            int target = Mathf.Max(1, maxRaycastHits);
            if (raycastHits == null || raycastHits.Length != target)
                raycastHits = new RaycastHit[target];
        }

        private static Vector3[] BuildOffsets(Vector3 right, Vector3 up, float radius, int sampleCount)
        {
            // Deterministic set of offsets. Order is tuned for "cheap wins" first.
            List<Vector3> offsets = new List<Vector3>(sampleCount);
            if (sampleCount <= 0 || radius <= 0f)
                return offsets.ToArray();

            offsets.Add(right * radius);
            if (offsets.Count >= sampleCount) return offsets.ToArray();
            offsets.Add(-right * radius);
            if (offsets.Count >= sampleCount) return offsets.ToArray();
            offsets.Add(up * radius);
            if (offsets.Count >= sampleCount) return offsets.ToArray();
            offsets.Add(-up * radius);
            if (offsets.Count >= sampleCount) return offsets.ToArray();

            offsets.Add((right + up).normalized * radius);
            if (offsets.Count >= sampleCount) return offsets.ToArray();
            offsets.Add((right - up).normalized * radius);
            if (offsets.Count >= sampleCount) return offsets.ToArray();
            offsets.Add((-right + up).normalized * radius);
            if (offsets.Count >= sampleCount) return offsets.ToArray();
            offsets.Add((-right - up).normalized * radius);

            // Trim
            if (offsets.Count > sampleCount)
                offsets.RemoveRange(sampleCount, offsets.Count - sampleCount);

            return offsets.ToArray();
        }

        private static float StdDev(float[] samples)
        {
            if (samples == null || samples.Length == 0)
                return 0f;

            float mean = 0f;
            for (int i = 0; i < samples.Length; i++)
                mean += samples[i];
            mean /= samples.Length;

            float variance = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float d = samples[i] - mean;
                variance += d * d;
            }
            variance /= samples.Length;
            return Mathf.Sqrt(variance);
        }

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit a, RaycastHit b)
            {
                return a.distance.CompareTo(b.distance);
            }
        }
    }
}

