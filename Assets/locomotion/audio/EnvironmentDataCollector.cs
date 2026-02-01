using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Audio
{
    /// <summary>
    /// Collects environment data for training and inference.
    /// Gathers weather, spatial, and physics data from various sources.
    /// </summary>
    public class EnvironmentDataCollector : MonoBehaviour
    {
        [Header("Data Sources")]
        [Tooltip("Reference to WeatherSystem (auto-found if null)")]
        public MonoBehaviour weatherSystemObject;

        [Tooltip("Reference to actor's RagdollSystem for physics state (MonoBehaviour, resolved via reflection)")]
        public MonoBehaviour ragdollSystem;

        [Header("Collection Settings")]
        [Tooltip("Include weather data in collection")]
        public bool includeWeather = true;

        [Tooltip("Include spatial position data")]
        public bool includeSpatial = true;

        [Tooltip("Include physics state data")]
        public bool includePhysics = true;

        [Tooltip("Include nearby objects data")]
        public bool includeNearbyObjects = true;

        [Tooltip("Radius for nearby object detection")]
        [Range(1f, 50f)]
        public float nearbyObjectRadius = 10f;

        [Header("Debug")]
        [Tooltip("Enable debug logging")]
        public bool enableDebugLogging = false;

        private void Awake()
        {
            // Auto-find weather system if not assigned
            if (weatherSystemObject == null && includeWeather)
            {
                weatherSystemObject = FindAnyObjectByType<MonoBehaviour>();
                // Try to find WeatherSystem via reflection
                var weatherSystemType = System.Type.GetType("Weather.WeatherSystem");
                if (weatherSystemType != null)
                {
                    var weatherSystems = FindObjectsByType(weatherSystemType, FindObjectsSortMode.None);
                    if (weatherSystems != null && weatherSystems.Length > 0)
                    {
                        weatherSystemObject = weatherSystems[0] as MonoBehaviour;
                    }
                }
            }

            // Auto-find ragdoll system if not assigned
            if (ragdollSystem == null)
            {
                // Use reflection to find RagdollSystem (to avoid Runtime dependency)
                var ragdollSystemType = System.Type.GetType("RagdollSystem, Locomotion.Runtime");
                if (ragdollSystemType == null)
                {
                    ragdollSystemType = System.Type.GetType("RagdollSystem, Assembly-CSharp");
                }
                if (ragdollSystemType != null)
                {
                    var found = GetComponentInParent(ragdollSystemType) as MonoBehaviour;
                    if (found == null)
                    {
                        var foundObj = FindAnyObjectByType(ragdollSystemType);
                        if (foundObj != null)
                        {
                            found = foundObj as MonoBehaviour;
                        }
                    }
                    if (found != null)
                    {
                        ragdollSystem = found;
                    }
                }
            }
        }

        /// <summary>
        /// Collect current environment data at a specific position.
        /// </summary>
        public EnvironmentData CollectEnvironmentData(Vector3 position)
        {
            EnvironmentData data = new EnvironmentData
            {
                position = position,
                timestamp = Time.time
            };

            // Collect weather data
            if (includeWeather)
            {
                CollectWeatherData(data);
            }

            // Collect spatial data
            if (includeSpatial)
            {
                CollectSpatialData(data, position);
            }

            // Collect physics state
            if (includePhysics && ragdollSystem != null)
            {
                CollectPhysicsData(data);
            }

            // Collect nearby objects
            if (includeNearbyObjects)
            {
                CollectNearbyObjectsData(data, position);
            }

            return data;
        }

        /// <summary>
        /// Serialize environment data for Python training script.
        /// </summary>
        public string SerializeForTraining(EnvironmentData data)
        {
            return JsonUtility.ToJson(data, true);
        }

        /// <summary>
        /// Collect weather data from WeatherSystem.
        /// </summary>
        private void CollectWeatherData(EnvironmentData data)
        {
            if (weatherSystemObject == null)
                return;

            try
            {
                // Use reflection to access WeatherSystem methods
                var weatherSystemType = weatherSystemObject.GetType();
                
                // Try to get temperature
                var tempProperty = weatherSystemType.GetProperty("temperature");
                if (tempProperty != null)
                {
                    data.temperature = Convert.ToSingle(tempProperty.GetValue(weatherSystemObject));
                }

                // Try to get wind data
                var windField = weatherSystemType.GetField("wind");
                if (windField != null)
                {
                    var windObject = windField.GetValue(weatherSystemObject);
                    if (windObject != null)
                    {
                        var windType = windObject.GetType();
                        
                        // Get wind speed
                        var speedProperty = windType.GetProperty("windSpeed");
                        if (speedProperty != null)
                        {
                            data.windSpeed = Convert.ToSingle(speedProperty.GetValue(windObject));
                        }

                        // Get wind direction
                        var directionProperty = windType.GetProperty("windDirection");
                        if (directionProperty != null)
                        {
                            var direction = directionProperty.GetValue(windObject);
                            if (direction is Vector3)
                            {
                                data.windDirection = (Vector3)direction;
                            }
                        }
                    }
                }

                // Try to get precipitation data
                var precipField = weatherSystemType.GetField("precipitation");
                if (precipField != null)
                {
                    var precipObject = precipField.GetValue(weatherSystemObject);
                    if (precipObject != null)
                    {
                        var precipType = precipObject.GetType();
                        
                        // Get precipitation rate
                        var rateProperty = precipType.GetProperty("precipitationRate");
                        if (rateProperty != null)
                        {
                            data.precipitationRate = Convert.ToSingle(rateProperty.GetValue(precipObject));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (enableDebugLogging)
                {
                    Debug.LogWarning($"[EnvironmentDataCollector] Error collecting weather data: {e.Message}");
                }
            }
        }

        /// <summary>
        /// Collect spatial position data.
        /// </summary>
        private void CollectSpatialData(EnvironmentData data, Vector3 position)
        {
            data.position = position;
            
            // Get height from terrain or mesh
            data.height = GetHeightAtPosition(position);
        }

        /// <summary>
        /// Collect physics state data from ragdoll system.
        /// Uses reflection to avoid direct dependency on Runtime.
        /// </summary>
        private void CollectPhysicsData(EnvironmentData data)
        {
            if (ragdollSystem == null)
                return;

            // Get ragdollRoot property using reflection
            var ragdollRootProp = ragdollSystem.GetType().GetProperty("ragdollRoot");
            if (ragdollRootProp != null)
            {
                var ragdollRoot = ragdollRootProp.GetValue(ragdollSystem) as Transform;
                if (ragdollRoot != null)
                {
                    var rigidbody = ragdollRoot.GetComponent<Rigidbody>();
                    if (rigidbody != null)
                    {
                        data.velocity = rigidbody.linearVelocity;
                        data.angularVelocity = rigidbody.angularVelocity;
                    }
                }
            }

            // Get current state using reflection
            var getCurrentStateMethod = ragdollSystem.GetType().GetMethod("GetCurrentState");
            if (getCurrentStateMethod != null)
            {
                var state = getCurrentStateMethod.Invoke(ragdollSystem, null);
                if (state != null)
                {
                    var rootPositionProp = state.GetType().GetProperty("rootPosition");
                    var rootRotationProp = state.GetType().GetProperty("rootRotation");
                    
                    if (rootPositionProp != null)
                    {
                        var pos = rootPositionProp.GetValue(state);
                        if (pos is Vector3)
                            data.rootPosition = (Vector3)pos;
                    }
                    
                    if (rootRotationProp != null)
                    {
                        var rot = rootRotationProp.GetValue(state);
                        if (rot is Quaternion)
                            data.rootRotation = (Quaternion)rot;
                    }
                }
            }
        }

        /// <summary>
        /// Collect nearby objects data.
        /// </summary>
        private void CollectNearbyObjectsData(EnvironmentData data, Vector3 position)
        {
            Collider[] colliders = Physics.OverlapSphere(position, nearbyObjectRadius);
            data.nearbyObjects = new List<NearbyObjectData>();

            foreach (var collider in colliders)
            {
                if (collider == null || collider.transform == null)
                    continue;

                // Skip self (using reflection to access transform)
                if (ragdollSystem != null)
                {
                    var ragdollTransform = ragdollSystem.transform;
                    if (collider.transform.IsChildOf(ragdollTransform))
                        continue;
                }

                NearbyObjectData objData = new NearbyObjectData
                {
                    position = collider.transform.position,
                    distance = Vector3.Distance(position, collider.transform.position),
                    objectType = collider.GetType().Name,
                    objectName = collider.gameObject.name
                };

                data.nearbyObjects.Add(objData);
            }
        }

        /// <summary>
        /// Get height at position (terrain or mesh).
        /// </summary>
        private float GetHeightAtPosition(Vector3 position)
        {
            // Try raycast down to find ground
            RaycastHit hit;
            if (Physics.Raycast(position + Vector3.up * 100f, Vector3.down, out hit, 200f))
            {
                return hit.point.y;
            }

            return position.y;
        }
    }

    /// <summary>
    /// Serializable environment data structure.
    /// </summary>
    [Serializable]
    public class EnvironmentData
    {
        [Tooltip("3D position")]
        public Vector3 position;

        [Tooltip("Height above ground")]
        public float height;

        [Tooltip("Temperature in Celsius")]
        public float temperature = 20f;

        [Tooltip("Wind speed in m/s")]
        public float windSpeed = 0f;

        [Tooltip("Wind direction vector")]
        public Vector3 windDirection = Vector3.zero;

        [Tooltip("Precipitation rate in mm/h")]
        public float precipitationRate = 0f;

        [Tooltip("Velocity vector")]
        public Vector3 velocity = Vector3.zero;

        [Tooltip("Angular velocity vector")]
        public Vector3 angularVelocity = Vector3.zero;

        [Tooltip("Root position")]
        public Vector3 rootPosition = Vector3.zero;

        [Tooltip("Root rotation")]
        public Quaternion rootRotation = Quaternion.identity;

        [Tooltip("List of nearby objects")]
        public List<NearbyObjectData> nearbyObjects = new List<NearbyObjectData>();

        [Tooltip("Timestamp of data collection")]
        public float timestamp;

        /// <summary>
        /// Convert to feature vector for ML model.
        /// </summary>
        public float[] ToFeatureVector()
        {
            List<float> features = new List<float>();

            // Position (3)
            features.Add(position.x);
            features.Add(position.y);
            features.Add(position.z);

            // Height (1)
            features.Add(height);

            // Weather (5)
            features.Add(temperature);
            features.Add(windSpeed);
            features.Add(windDirection.x);
            features.Add(windDirection.y);
            features.Add(windDirection.z);
            features.Add(precipitationRate);

            // Physics (6)
            features.Add(velocity.x);
            features.Add(velocity.y);
            features.Add(velocity.z);
            features.Add(angularVelocity.x);
            features.Add(angularVelocity.y);
            features.Add(angularVelocity.z);

            // Root transform (7)
            features.Add(rootPosition.x);
            features.Add(rootPosition.y);
            features.Add(rootPosition.z);
            features.Add(rootRotation.x);
            features.Add(rootRotation.y);
            features.Add(rootRotation.z);
            features.Add(rootRotation.w);

            // Nearby objects (simplified - average distance, count)
            float avgDistance = 0f;
            if (nearbyObjects != null && nearbyObjects.Count > 0)
            {
                foreach (var obj in nearbyObjects)
                {
                    avgDistance += obj.distance;
                }
                avgDistance /= nearbyObjects.Count;
            }
            features.Add(avgDistance);
            features.Add(nearbyObjects != null ? nearbyObjects.Count : 0f);

            return features.ToArray();
        }
    }

    /// <summary>
    /// Data about a nearby object.
    /// </summary>
    [Serializable]
    public class NearbyObjectData
    {
        public Vector3 position;
        public float distance;
        public string objectType;
        public string objectName;
    }
}
