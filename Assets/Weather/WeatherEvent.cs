using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Types of weather events that can occur
    /// </summary>
    public enum WeatherEventType
    {
        PressureChange,
        Lightning,
        TemperatureChange,
        WindGust,
        Tornado,
        PrecipitationChange,
        HumidityChange,
        CloudFormation
    }

    /// <summary>
    /// Systems that can be affected by weather events
    /// </summary>
    [Flags]
    public enum AffectedSystem
    {
        None = 0,
        Meteorology = 1 << 0,
        Wind = 1 << 1,
        Precipitation = 1 << 2,
        Water = 1 << 3,
        Cloud = 1 << 4,
        WeatherPhysicsManifold = 1 << 5,
        All = ~0
    }

    /// <summary>
    /// Base class for weather events that can be processed by the WeatherSystem.
    /// Events can be additive or multiplicative and affect various weather subsystems.
    /// </summary>
    public class WeatherEvent : MonoBehaviour
    {
        [Header("Event Configuration")]
        [Tooltip("Type of weather event")]
        public WeatherEventType eventType = WeatherEventType.PressureChange;

        [Tooltip("Magnitude of the event (units depend on event type)")]
        public float magnitude = 0f;

        [Tooltip("Duration of the event in seconds (0 = permanent)")]
        public float duration = 0f;

        [Tooltip("If true, event is additive. If false, event is multiplicative")]
        public bool isAdditive = true;

        [Tooltip("Which systems this event affects")]
        public AffectedSystem affectsSystems = AffectedSystem.All;

        [Header("Event Data")]
        [Tooltip("Optional Vector3 data (e.g., wind direction, position)")]
        public Vector3 vectorData = Vector3.zero;

        [Tooltip("Optional additional float parameters")]
        public float parameter1 = 0f;
        public float parameter2 = 0f;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        [Tooltip("Area of effect radius in meters")]
        public float areaOfEffect = 10f;

        [Header("Timing")]
        [Tooltip("Time when event started")]
        private float startTime = 0f;

        [Tooltip("Whether event is currently active")]
        private bool isActive = false;

        /// <summary>
        /// Get the current magnitude, accounting for duration and time elapsed
        /// </summary>
        public float GetCurrentMagnitude()
        {
            if (!isActive)
                return 0f;

            if (duration <= 0f)
                return magnitude;

            float elapsed = Time.time - startTime;
            if (elapsed >= duration)
            {
                isActive = false;
                return 0f;
            }

            // Linear fade out (could be changed to exponential or other curves)
            float t = 1f - (elapsed / duration);
            return magnitude * t;
        }

        /// <summary>
        /// Activate this event
        /// </summary>
        public void Activate()
        {
            isActive = true;
            startTime = Time.time;
        }

        /// <summary>
        /// Deactivate this event
        /// </summary>
        public void Deactivate()
        {
            isActive = false;
        }

        /// <summary>
        /// Check if this event affects a specific system
        /// </summary>
        public bool AffectsSystem(AffectedSystem system)
        {
            return (affectsSystems & system) != 0;
        }

        /// <summary>
        /// Get event data as a structured object
        /// </summary>
        public WeatherEventData GetEventData()
        {
            return new WeatherEventData
            {
                eventType = this.eventType,
                magnitude = GetCurrentMagnitude(),
                isAdditive = this.isAdditive,
                vectorData = this.vectorData,
                parameter1 = this.parameter1,
                parameter2 = this.parameter2,
                position = transform.position
            };
        }

        private void Start()
        {
            // Auto-activate on start if enabled
            if (enabled)
            {
                Activate();
            }
        }

        /// <summary>
        /// Get color for event type
        /// </summary>
        private Color GetEventTypeColor()
        {
            switch (eventType)
            {
                case WeatherEventType.PressureChange:
                    return magnitude > 0 ? Color.red : Color.blue; // High = red, Low = blue
                case WeatherEventType.Lightning:
                    return Color.yellow;
                case WeatherEventType.TemperatureChange:
                    return magnitude > 0 ? Color.red : Color.cyan; // Hot = red, Cold = cyan
                case WeatherEventType.WindGust:
                    return Color.cyan;
                case WeatherEventType.Tornado:
                    return new Color(0.5f, 0.2f, 0.8f); // Purple
                case WeatherEventType.PrecipitationChange:
                    return Color.blue;
                case WeatherEventType.HumidityChange:
                    return new Color(0.3f, 0.6f, 1f); // Light blue
                case WeatherEventType.CloudFormation:
                    return Color.white;
                default:
                    return Color.gray;
            }
        }

        /// <summary>
        /// Get color for affected system
        /// </summary>
        private Color GetSystemColor(AffectedSystem system)
        {
            switch (system)
            {
                case AffectedSystem.Meteorology:
                    return Color.cyan;
                case AffectedSystem.Wind:
                    return Color.blue;
                case AffectedSystem.Precipitation:
                    return new Color(0.2f, 0.4f, 1f); // Dark blue
                case AffectedSystem.Water:
                    return new Color(0f, 0.5f, 1f); // Water blue
                case AffectedSystem.Cloud:
                    return Color.white;
                case AffectedSystem.WeatherPhysicsManifold:
                    return Color.magenta;
                default:
                    return Color.gray;
            }
        }

        /// <summary>
        /// Draw direction arrow gizmo
        /// </summary>
        private void DrawDirectionArrow(Vector3 position, Vector3 direction, float magnitude, Color color)
        {
            if (direction.magnitude < 0.01f)
                return;

            direction = direction.normalized;
            float arrowLength = Mathf.Clamp(magnitude * 0.5f, 1f, areaOfEffect * 0.5f);
            Vector3 endPos = position + direction * arrowLength;

            // Arrow shaft
            Gizmos.color = color;
            Gizmos.DrawLine(position, endPos);

            // Arrow head
            Vector3 right = Vector3.Cross(direction, Vector3.up).normalized;
            if (right.magnitude < 0.01f)
                right = Vector3.Cross(direction, Vector3.forward).normalized;

            Vector3 arrowHeadSize = direction * -0.3f + right * 0.2f;
            Gizmos.DrawLine(endPos, endPos + arrowHeadSize);
            Gizmos.DrawLine(endPos, endPos + direction * -0.3f - right * 0.2f);
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 position = transform.position;
            float currentMagnitude = GetCurrentMagnitude();
            Color baseColor = GetEventTypeColor();

            // Calculate opacity based on duration
            float opacity = 1f;
            if (duration > 0f && isActive)
            {
                float elapsed = Application.isPlaying ? (Time.time - startTime) : 0f;
                opacity = Mathf.Clamp01(1f - (elapsed / duration));
            }
            else if (!isActive)
            {
                opacity = 0.5f; // Dimmed if not active
            }

            baseColor.a = opacity;

            // Base sphere showing area of effect
            float radius = areaOfEffect * (0.5f + currentMagnitude * 0.01f); // Scale with magnitude
            radius = Mathf.Clamp(radius, 0.5f, areaOfEffect * 2f);

            Gizmos.color = baseColor;
            Gizmos.DrawWireSphere(position, radius);

            // Direction arrow if vectorData is set
            if (vectorData.magnitude > 0.01f)
            {
                DrawDirectionArrow(position, vectorData, currentMagnitude, baseColor);
            }

            // Affected systems indicator (colored rings)
            if (affectsSystems != AffectedSystem.None && affectsSystems != AffectedSystem.All)
            {
                float ringRadius = radius * 1.1f;
                int systemCount = 0;
                float angleStep = 360f / 6f; // Max 6 systems

                foreach (AffectedSystem system in System.Enum.GetValues(typeof(AffectedSystem)))
                {
                    if (system == AffectedSystem.None || system == AffectedSystem.All)
                        continue;

                    if (AffectsSystem(system))
                    {
                        Color systemColor = GetSystemColor(system);
                        systemColor.a = opacity * 0.7f;
                        Gizmos.color = systemColor;

                        float angle = systemCount * angleStep * Mathf.Deg2Rad;
                        Vector3 ringPos = position + new Vector3(
                            Mathf.Cos(angle) * ringRadius,
                            0f,
                            Mathf.Sin(angle) * ringRadius
                        );
                        Gizmos.DrawWireSphere(ringPos, radius * 0.15f);
                        systemCount++;
                    }
                }
            }
            else if (affectsSystems == AffectedSystem.All)
            {
                // Show all systems as a gradient ring
                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, opacity * 0.5f);
                Gizmos.DrawWireSphere(position, radius * 1.2f);
            }

            // Magnitude indicator (inner sphere size)
            if (currentMagnitude > 0.01f)
            {
                Gizmos.color = new Color(baseColor.r, baseColor.g, baseColor.b, opacity * 0.3f);
                float innerRadius = radius * 0.3f * (currentMagnitude / Mathf.Max(magnitude, 1f));
                Gizmos.DrawSphere(position, innerRadius);
            }
        }
    }

    /// <summary>
    /// Structured data for weather events
    /// </summary>
    public struct WeatherEventData
    {
        public WeatherEventType eventType;
        public float magnitude;
        public bool isAdditive;
        public Vector3 vectorData;
        public float parameter1;
        public float parameter2;
        public Vector3 position;
    }
}
