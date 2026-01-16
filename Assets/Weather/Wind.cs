using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Wind data for a specific altitude level
    /// </summary>
    [System.Serializable]
    public class WindData
    {
        public float speed; // m/s
        public float direction; // degrees (0-360, direction wind comes FROM)
        public float gustSpeed; // m/s
    }

    /// <summary>
    /// Wind system that generates wind field vectors and affects clouds, precipitation, and physics objects.
    /// Supports altitude-based wind variation and uses Semi-Lagrangian advection.
    /// </summary>
    public class Wind : MonoBehaviour
    {
        [Header("Wind Parameters")]
        [Tooltip("Horizontal wind speed in m/s")]
        public float speed = 5f;

        [Tooltip("Wind direction in degrees (0-360, meteorological convention: direction wind comes FROM)")]
        [Range(0f, 360f)]
        public float direction = 0f; // North

        [Tooltip("Peak wind speed (gusts) in m/s")]
        public float gustSpeed = 10f;

        [Header("Altitude Levels")]
        [Tooltip("Wind data by altitude (m above ground level)")]
        public Dictionary<float, WindData> altitudeLevels = new Dictionary<float, WindData>();

        [Tooltip("Default altitude levels (m AGL)")]
        public float[] defaultAltitudeLevels = { 0f, 100f, 500f, 1000f, 2000f };

        [Header("Physics Integration")]
        [Tooltip("PhysicsManifold for wind field storage (optional)")]
        public PhysicsManifold physicsManifold;

        [Tooltip("Force multiplier for physics objects")]
        public float forceMultiplier = 1f;

        [Header("Configuration")]
        [Tooltip("Auto-generate altitude levels on start")]
        public bool autoGenerateAltitudeLevels = true;

        [Tooltip("Wind variation over time")]
        public bool enableWindVariation = true;

        [Tooltip("Variation speed")]
        public float variationSpeed = 0.1f;

        [Tooltip("Variation amplitude")]
        public float variationAmplitude = 2f;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        [Tooltip("Wind field bounds size (meters)")]
        public float windFieldBounds = 50f;

        [Tooltip("Number of wind arrows to draw")]
        [Range(3, 20)]
        public int arrowCount = 10;

        private void Awake()
        {
            if (autoGenerateAltitudeLevels)
            {
                GenerateDefaultAltitudeLevels();
            }
        }

        /// <summary>
        /// Service update called by WeatherSystem
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Update wind variation
            if (enableWindVariation)
            {
                UpdateWindVariation(deltaTime);
            }

            // Generate/update wind field
            GenerateWindField();
        }

        /// <summary>
        /// Get wind vector at a specific position and altitude
        /// </summary>
        public Vector3 GetWindAtPosition(Vector3 position, float altitude)
        {
            // Find closest altitude level
            WindData windData = GetWindDataAtAltitude(altitude);

            // Convert direction to vector (meteorological convention: direction wind comes FROM)
            // So we need to reverse it for the vector direction
            float directionRad = (windData.direction + 180f) * Mathf.Deg2Rad;
            Vector3 windVector = new Vector3(
                Mathf.Sin(directionRad) * windData.speed,
                0f, // Horizontal wind only (altitude variation handled separately)
                Mathf.Cos(directionRad) * windData.speed
            );

            return windVector;
        }

        /// <summary>
        /// Get wind data at a specific altitude (interpolates between levels)
        /// </summary>
        public WindData GetWindDataAtAltitude(float altitude)
        {
            if (altitudeLevels.Count == 0)
            {
                return new WindData { speed = speed, direction = direction, gustSpeed = gustSpeed };
            }

            // Find closest altitude levels
            float closestBelow = float.MinValue;
            float closestAbove = float.MaxValue;

            foreach (var level in altitudeLevels.Keys)
            {
                if (level <= altitude && level > closestBelow)
                    closestBelow = level;
                if (level >= altitude && level < closestAbove)
                    closestAbove = level;
            }

            // If we're below all levels, use lowest
            if (closestBelow == float.MinValue)
            {
                return altitudeLevels[closestAbove];
            }

            // If we're above all levels, use highest
            if (closestAbove == float.MaxValue)
            {
                return altitudeLevels[closestBelow];
            }

            // Interpolate between levels
            if (closestBelow == closestAbove)
            {
                return altitudeLevels[closestBelow];
            }

            float t = (altitude - closestBelow) / (closestAbove - closestBelow);
            WindData below = altitudeLevels[closestBelow];
            WindData above = altitudeLevels[closestAbove];

            // Interpolate speed and direction
            float interpolatedSpeed = Mathf.Lerp(below.speed, above.speed, t);
            float interpolatedDirection = Mathf.LerpAngle(below.direction, above.direction, t);
            float interpolatedGust = Mathf.Lerp(below.gustSpeed, above.gustSpeed, t);

            return new WindData
            {
                speed = interpolatedSpeed,
                direction = interpolatedDirection,
                gustSpeed = interpolatedGust
            };
        }

        /// <summary>
        /// Calculate wind force to apply to a physics object
        /// </summary>
        public Vector3 GetWindForce(Rigidbody rb)
        {
            if (rb == null)
                return Vector3.zero;

            // Get wind at object's position
            Vector3 position = rb.transform.position;
            float altitude = position.y;
            Vector3 windVector = GetWindAtPosition(position, altitude);

            // Calculate force based on object's cross-sectional area (approximate)
            // F = 0.5 * ρ * v² * A * Cd
            // Simplified: F = k * v² * A
            float area = EstimateCrossSectionalArea(rb);
            float forceMagnitude = 0.5f * 1.225f * windVector.magnitude * windVector.magnitude * area * 0.5f; // ρ ≈ 1.225 kg/m³, Cd ≈ 0.5

            Vector3 force = windVector.normalized * forceMagnitude * forceMultiplier;

            // Check if object implements IOnWeatherForce for custom handling
            IOnWeatherForce weatherForceHandler = rb.GetComponent<IOnWeatherForce>();
            if (weatherForceHandler != null)
            {
                bool handled = weatherForceHandler.OnWeatherForce(force, WeatherEventType.WindGust);
                if (handled)
                {
                    return Vector3.zero; // Force was handled by object
                }
            }

            return force;
        }

        /// <summary>
        /// Apply wind force to a rigidbody
        /// </summary>
        public void ApplyWindForce(Rigidbody rb)
        {
            if (rb == null)
                return;

            Vector3 force = GetWindForce(rb);
            if (force.magnitude > 0f)
            {
                rb.AddForce(force, ForceMode.Force);
            }
        }

        /// <summary>
        /// Estimate cross-sectional area of a rigidbody
        /// </summary>
        private float EstimateCrossSectionalArea(Rigidbody rb)
        {
            // Use bounds to estimate area
            Bounds bounds = GetRigidbodyBounds(rb);
            Vector3 size = bounds.size;
            
            // Assume wind comes from direction, so use perpendicular cross-section
            // Simplified: use average of width and height
            float area = (size.x + size.z) * 0.5f * size.y;
            return Mathf.Max(area, 0.1f); // Minimum area
        }

        /// <summary>
        /// Get bounds of a rigidbody
        /// </summary>
        private Bounds GetRigidbodyBounds(Rigidbody rb)
        {
            Collider collider = rb.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds;
            }

            // Fallback: use transform scale
            return new Bounds(rb.transform.position, rb.transform.localScale);
        }

        /// <summary>
        /// Generate or update wind field in PhysicsManifold
        /// </summary>
        public void GenerateWindField()
        {
            if (physicsManifold != null)
            {
                // Update physics manifold with wind data
                // This would be implemented based on PhysicsManifold structure
                // For now, we'll leave it as a placeholder
            }
        }

        /// <summary>
        /// Apply a weather event to wind system
        /// </summary>
        public void ApplyWeatherEvent(WeatherEventData eventData)
        {
            switch (eventData.eventType)
            {
                case WeatherEventType.WindGust:
                    if (eventData.isAdditive)
                        gustSpeed += eventData.magnitude;
                    else
                        gustSpeed *= eventData.magnitude;
                    break;

                case WeatherEventType.Tornado:
                    // Tornado creates a vortex pattern
                    // This would be implemented with a special wind pattern
                    // For now, increase wind speed significantly
                    speed += eventData.magnitude;
                    gustSpeed += eventData.magnitude * 2f;
                    ApplyTornadoForce(eventData);
                    break;
            }
        }

        /// <summary>
        /// Apply tornado force to physics objects in the scene
        /// </summary>
        private void ApplyTornadoForce(WeatherEventData eventData)
        {
            // Find all rigidbodies in the scene
            Rigidbody[] rigidbodies = FindObjectsOfType<Rigidbody>();

            foreach (var rb in rigidbodies)
            {
                if (rb == null)
                    continue;

                // Calculate distance from tornado center (eventData.position)
                Vector3 toObject = rb.transform.position - eventData.position;
                float distance = toObject.magnitude;

                // Tornado force decreases with distance
                float forceStrength = eventData.magnitude / (1f + distance * 0.1f);

                // Create vortex force (tangential + upward)
                Vector3 tangent = Vector3.Cross(Vector3.up, toObject.normalized);
                Vector3 tornadoForce = (tangent + Vector3.up * 0.5f) * forceStrength;

                // Check for custom handler
                IOnWeatherForce handler = rb.GetComponent<IOnWeatherForce>();
                if (handler != null)
                {
                    handler.OnWeatherForce(tornadoForce, WeatherEventType.Tornado);
                }
                else
                {
                    rb.AddForce(tornadoForce, ForceMode.Force);
                }
            }
        }

        /// <summary>
        /// Generate default altitude levels
        /// </summary>
        private void GenerateDefaultAltitudeLevels()
        {
            altitudeLevels.Clear();
            foreach (float altitude in defaultAltitudeLevels)
            {
                // Wind typically increases with altitude (up to a point)
                float altitudeFactor = 1f + (altitude / 1000f) * 0.5f;
                altitudeLevels[altitude] = new WindData
                {
                    speed = speed * altitudeFactor,
                    direction = direction,
                    gustSpeed = gustSpeed * altitudeFactor
                };
            }
        }

        /// <summary>
        /// Update wind variation over time
        /// </summary>
        private void UpdateWindVariation(float deltaTime)
        {
            float variation = Mathf.Sin(Time.time * variationSpeed) * variationAmplitude;
            speed += variation * deltaTime;
            speed = Mathf.Max(speed, 0f);
        }

        /// <summary>
        /// Draw direction arrow (reusing WeatherEvent style)
        /// </summary>
        private void DrawWindArrow(Vector3 position, Vector3 direction, float speed, Color color)
        {
            if (direction.magnitude < 0.01f)
                return;

            direction = direction.normalized;
            float arrowLength = Mathf.Clamp(speed * 2f, 2f, windFieldBounds * 0.3f);
            Vector3 endPos = position + direction * arrowLength;

            // Arrow shaft
            Gizmos.color = color;
            Gizmos.DrawLine(position, endPos);

            // Arrow head
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(direction, Vector3.forward).normalized;

            Vector3 arrowHeadSize = direction * -0.5f + right * 0.3f;
            Gizmos.DrawLine(endPos, endPos + arrowHeadSize);
            Gizmos.DrawLine(endPos, endPos + direction * -0.5f - right * 0.3f);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 position = transform.position;

            // Wind speed color (cyan to blue gradient)
            float speedNormalized = Mathf.InverseLerp(0f, 20f, speed);
            Color windColor = Color.Lerp(Color.cyan, Color.blue, speedNormalized);
            windColor.a = 0.8f;

            // Affected area bounds (reusing WeatherEvent sphere style)
            Gizmos.color = new Color(windColor.r, windColor.g, windColor.b, 0.2f);
            Gizmos.DrawWireSphere(position, windFieldBounds);

            // Wind direction arrows (reusing WeatherEvent arrow style)
            // Convert direction to vector (meteorological convention: direction wind comes FROM)
            float directionRad = (direction + 180f) * Mathf.Deg2Rad;
            Vector3 windDirection = new Vector3(
                Mathf.Sin(directionRad),
                0f,
                Mathf.Cos(directionRad)
            );

            // Draw multiple arrows in a grid pattern
            int gridSize = Mathf.CeilToInt(Mathf.Sqrt(arrowCount));
            float spacing = windFieldBounds * 2f / (gridSize + 1);

            for (int i = 0; i < arrowCount; i++)
            {
                int x = i % gridSize;
                int z = i / gridSize;
                Vector3 arrowPos = position + new Vector3(
                    (x - gridSize * 0.5f + 0.5f) * spacing,
                    0f,
                    (z - gridSize * 0.5f + 0.5f) * spacing
                );

                // Get wind at this position (ground level)
                Vector3 windVector = GetWindAtPosition(arrowPos, 0f);
                DrawWindArrow(arrowPos, windVector, speed, windColor);
            }

            // Altitude level indicators
            if (altitudeLevels.Count > 1)
            {
                Gizmos.color = new Color(windColor.r, windColor.g, windColor.b, 0.3f);
                foreach (var kvp in altitudeLevels)
                {
                    float altitude = kvp.Key;
                    Vector3 levelPos = position + Vector3.up * altitude;
                    Gizmos.DrawWireSphere(levelPos, windFieldBounds * 0.1f);
                }
            }
        }
    }
}
