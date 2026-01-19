using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Per-ear listener component.
    /// - Registers/unregisters itself with AudioPathingSolver.
    /// - Samples nearby AudioSource(s) and converts them into Sensory impulses for the NervousSystem.
    /// </summary>
    public class Ears : MonoBehaviour
    {
        [System.Serializable]
        public class AuditoryDetection
        {
            public AudioSource source;
            public GameObject sourceObject;
            public float distance;
            public float transmission;     // 0..1
            public float perceivedLoudness; // heuristic
            public bool echoEnabled;
            public float echoStrength;
        }

        [Header("Hearing")]
        public float hearingRange = 30f;
        public int maxDetections = 6;
        public float scanInterval = 0.2f;

        [Header("Impulse Routing")]
        public bool sendSensoryImpulses = true;
        public string impulseChannelName = "Spinal";
        public int impulsePriority = 0;

        [Tooltip("Optional override. If null, uses solver instance or finds one.")]
        public AudioPathingSolver solver;

        private NervousSystem nervousSystem;
        private float lastScanTime = -999f;

        private readonly List<AuditoryDetection> detections = new List<AuditoryDetection>(16);
        private readonly List<AuditoryDetection> topDetections = new List<AuditoryDetection>(16);

        private void Awake()
        {
            nervousSystem = GetComponentInParent<NervousSystem>();
            if (solver == null)
                solver = AudioPathingSolver.Instance != null ? AudioPathingSolver.Instance : FindObjectOfType<AudioPathingSolver>();
        }

        private void OnEnable()
        {
            if (solver == null)
                solver = AudioPathingSolver.Instance != null ? AudioPathingSolver.Instance : FindObjectOfType<AudioPathingSolver>();

            solver?.RegisterEar(this);
        }

        private void OnDisable()
        {
            solver?.UnregisterEar(this);
        }

        private void Update()
        {
            if (!sendSensoryImpulses)
                return;

            if (scanInterval > 0f && Time.time - lastScanTime < scanInterval)
                return;

            if (solver == null)
                solver = AudioPathingSolver.Instance != null ? AudioPathingSolver.Instance : FindObjectOfType<AudioPathingSolver>();

            if (solver == null || nervousSystem == null)
                return;

            lastScanTime = Time.time;

            ScanAudioSources();

            if (topDetections.Count > 0)
            {
                // Create a single auditory impulse containing a batch of detections.
                var strongest = topDetections[0];
                var sensory = new SensoryData(
                    position: transform.position,
                    normal: transform.forward,
                    force: strongest.perceivedLoudness,
                    contactObject: strongest.sourceObject,
                    sensorType: SensorType.Auditory.ToString(),
                    payload: topDetections
                );

                var impulse = new ImpulseData(
                    ImpulseType.Sensory,
                    source: "Ears",
                    target: "NervousSystem",
                    data: sensory,
                    priority: impulsePriority
                );

                nervousSystem.SendImpulseUp(impulseChannelName, impulse);
            }
        }

        private void ScanAudioSources()
        {
            detections.Clear();
            topDetections.Clear();

            Vector3 earPos = transform.position;
            float range = Mathf.Max(0f, hearingRange);
            float range2 = range * range;

            var sources = solver.GetActiveAudioSources();
            for (int i = 0; i < sources.Count; i++)
            {
                AudioSource src = sources[i];
                if (src == null || !src.isActiveAndEnabled)
                    continue;

                if (!src.isPlaying)
                    continue;

                Vector3 srcPos = src.transform.position;
                float d2 = (srcPos - earPos).sqrMagnitude;
                if (d2 > range2)
                    continue;

                float distance = Mathf.Sqrt(d2);
                var path = solver.ComputeTransmission(srcPos, earPos);

                // Perceived loudness heuristic: volume * transmission, with mild distance falloff.
                float loudness = Mathf.Max(0f, src.volume) * path.transmission / (1f + distance * 0.25f);

                var det = new AuditoryDetection
                {
                    source = src,
                    sourceObject = src.gameObject,
                    distance = distance,
                    transmission = path.transmission,
                    perceivedLoudness = loudness,
                    echoEnabled = path.echoEnabled,
                    echoStrength = path.echoStrength
                };

                detections.Add(det);

                // Optional debug/audio-side effects (per AudioSource).
                solver.ApplyUnityAudioEffects(src, path);
            }

            // Keep the top N by loudness.
            detections.Sort((a, b) => b.perceivedLoudness.CompareTo(a.perceivedLoudness));
            int take = Mathf.Min(maxDetections, detections.Count);
            for (int i = 0; i < take; i++)
                topDetections.Add(detections[i]);
        }
    }
}

