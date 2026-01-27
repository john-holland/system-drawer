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

        private MonoBehaviour nervousSystem;
        private float lastScanTime = -999f;

        private readonly List<AuditoryDetection> detections = new List<AuditoryDetection>(16);
        private readonly List<AuditoryDetection> topDetections = new List<AuditoryDetection>(16);
        
        // Cached reflection data to avoid repeated lookups
        private static System.Type cachedSensoryDataType = null;
        private static System.Reflection.ConstructorInfo cachedSensoryDataConstructor = null;
        private static bool sensoryDataTypeLookupFailed = false;

        /// <summary>
        /// Helper method to set property using reflection.
        /// </summary>
        private void SetProperty(object obj, string propertyName, object value)
        {
            if (obj == null) return;
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
        }

        private void Awake()
        {
            // Use reflection to find NervousSystem (to avoid Runtime dependency)
            var nervousSystemType = System.Type.GetType("NervousSystem, Locomotion.Runtime");
            if (nervousSystemType == null)
            {
                nervousSystemType = System.Type.GetType("NervousSystem, Assembly-CSharp");
            }
            if (nervousSystemType != null)
            {
                var found = GetComponentInParent(nervousSystemType);
                if (found != null)
                {
                    nervousSystem = found as MonoBehaviour;
                }
            }
            
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
                // Create a single auditory impulse containing a batch of detections using reflection
                var strongest = topDetections[0];
                
                // Use cached type and constructor if available, otherwise look them up
                if (cachedSensoryDataType == null && !sensoryDataTypeLookupFailed)
                {
                    try
                    {
                        // Create SensoryData using reflection - try fully qualified names first
                        cachedSensoryDataType = System.Type.GetType("SensoryData, Locomotion.Runtime, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
                        if (cachedSensoryDataType == null)
                        {
                            cachedSensoryDataType = System.Type.GetType("SensoryData, Locomotion.Runtime");
                        }
                        if (cachedSensoryDataType == null)
                        {
                            cachedSensoryDataType = System.Type.GetType("SensoryData, Assembly-CSharp");
                        }
                        
                        // If still not found, try loading from all loaded assemblies
                        if (cachedSensoryDataType == null)
                        {
                            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                            {
                                try
                                {
                                    // Try fully qualified name first
                                    cachedSensoryDataType = assembly.GetType("SensoryData", false, false);
                                    if (cachedSensoryDataType != null)
                                        break;
                                }
                                catch (System.Exception ex)
                                {
                                    // Ignore exceptions during type lookup
                                    Debug.LogWarning($"[Ears] Exception looking up SensoryData in assembly {assembly.FullName}: {ex.Message}");
                                }
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Ears] Exception during SensoryData type lookup: {ex.Message}\n{ex.StackTrace}");
                        sensoryDataTypeLookupFailed = true;
                    }
                    
                    if (cachedSensoryDataType != null)
                    {
                        try
                        {
                            // Get all constructors and find the one that matches
                            var constructors = cachedSensoryDataType.GetConstructors(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                            
                            // Look for constructor with 6 parameters (Vector3, Vector3, float, GameObject, string, object)
                            foreach (var ctor in constructors)
                            {
                                var parameters = ctor.GetParameters();
                                if (parameters.Length == 6 &&
                                    parameters[0].ParameterType == typeof(Vector3) &&
                                    parameters[1].ParameterType == typeof(Vector3) &&
                                    parameters[2].ParameterType == typeof(float) &&
                                    parameters[3].ParameterType == typeof(GameObject) &&
                                    parameters[4].ParameterType == typeof(string) &&
                                    parameters[5].ParameterType == typeof(object))
                                {
                                    cachedSensoryDataConstructor = ctor;
                                    break;
                                }
                            }
                            
                            // If 6-parameter constructor not found, try 5-parameter (without optional object payload)
                            if (cachedSensoryDataConstructor == null)
                            {
                                foreach (var ctor in constructors)
                                {
                                    var parameters = ctor.GetParameters();
                                    if (parameters.Length == 5 &&
                                        parameters[0].ParameterType == typeof(Vector3) &&
                                        parameters[1].ParameterType == typeof(Vector3) &&
                                        parameters[2].ParameterType == typeof(float) &&
                                        parameters[3].ParameterType == typeof(GameObject) &&
                                        parameters[4].ParameterType == typeof(string))
                                    {
                                        cachedSensoryDataConstructor = ctor;
                                        break;
                                    }
                                }
                            }
                            
                            if (cachedSensoryDataConstructor == null)
                            {
                                Debug.LogError($"[Ears] Could not find SensoryData constructor. Found {constructors.Length} constructor(s).");
                                foreach (var ctor in constructors)
                                {
                                    var paramList = string.Join(", ", System.Array.ConvertAll(ctor.GetParameters(), p => p.ParameterType.Name));
                                    Debug.LogError($"[Ears] Constructor: ({paramList})");
                                }
                                sensoryDataTypeLookupFailed = true;
                            }
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"[Ears] Exception looking up SensoryData constructor: {ex.Message}\n{ex.StackTrace}");
                            sensoryDataTypeLookupFailed = true;
                        }
                    }
                    else
                    {
                        Debug.LogError("[Ears] Could not find SensoryData type");
                        sensoryDataTypeLookupFailed = true;
                    }
                }
                
                object sensory = null;
                if (cachedSensoryDataConstructor != null)
                {
                    try
                    {
                        // Invoke constructor with parameters
                        if (cachedSensoryDataConstructor.GetParameters().Length == 6)
                        {
                            sensory = cachedSensoryDataConstructor.Invoke(new object[] 
                            { 
                                transform.position, 
                                transform.forward, 
                                strongest.perceivedLoudness, 
                                strongest.sourceObject, 
                                "Auditory", 
                                topDetections 
                            });
                        }
                        else
                        {
                            // 5-parameter version (without payload)
                            sensory = cachedSensoryDataConstructor.Invoke(new object[] 
                            { 
                                transform.position, 
                                transform.forward, 
                                strongest.perceivedLoudness, 
                                strongest.sourceObject, 
                                "Auditory"
                            });
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Ears] Exception creating SensoryData instance: {ex.Message}\n{ex.StackTrace}");
                    }
                }

                // Create ImpulseData using reflection
                var impulseDataType = System.Type.GetType("ImpulseData, Locomotion.Runtime");
                if (impulseDataType == null)
                {
                    impulseDataType = System.Type.GetType("ImpulseData, Assembly-CSharp");
                }
                
                object impulse = null;
                if (impulseDataType != null)
                {
                    // Get ImpulseType enum value
                    var impulseTypeEnum = System.Type.GetType("ImpulseType, Locomotion.Runtime");
                    if (impulseTypeEnum == null)
                    {
                        impulseTypeEnum = System.Type.GetType("ImpulseType, Assembly-CSharp");
                    }
                    object sensoryImpulseType = null;
                    if (impulseTypeEnum != null)
                    {
                        sensoryImpulseType = System.Enum.Parse(impulseTypeEnum, "Sensory");
                    }
                    
                    // Create ImpulseData using constructor
                    var constructor = impulseDataType.GetConstructor(new System.Type[] 
                    { 
                        impulseTypeEnum ?? typeof(int), 
                        typeof(string), 
                        typeof(string), 
                        typeof(object), 
                        typeof(int) 
                    });
                    if (constructor != null)
                    {
                        impulse = constructor.Invoke(new object[] 
                        { 
                            sensoryImpulseType ?? 0, 
                            "Ears", 
                            "NervousSystem", 
                            sensory, 
                            impulsePriority 
                        });
                    }
                }

                // Use reflection to call SendImpulseUp (to avoid Runtime dependency)
                if (nervousSystem != null && impulse != null)
                {
                    var sendImpulseMethod = nervousSystem.GetType().GetMethod("SendImpulseUp");
                    if (sendImpulseMethod != null)
                    {
                        sendImpulseMethod.Invoke(nervousSystem, new object[] { impulseChannelName, impulse });
                    }
                }
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

