using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Pond/Lake system that collects water from precipitation and rivers.
    /// Manages lake effect on local weather and tracks volume and surface area.
    /// </summary>
    public class Pond : MonoBehaviour
    {
        [Header("Pond Parameters")]
        [Tooltip("Water level in meters")]
        public float waterLevel = 0f;

        [Tooltip("Volume in m³")]
        public float volume = 0f;

        [Tooltip("Surface area in m²")]
        public float surfaceArea = 100f;

        [Tooltip("Temperature in Celsius (affects local weather)")]
        public float temperature = 20f;

        [Header("Lake Effect")]
        [Tooltip("Enable lake effect on local weather")]
        public bool lakeEffectEnabled = true;

        [Tooltip("Natural entropy for procedural effects")]
        [Range(0f, 1f)]
        public float naturalEntropy = 0.5f;

        [Header("Configuration")]
        [Tooltip("Auto-calculate surface area from geometry")]
        public bool autoCalculateSurfaceArea = true;

        [Tooltip("Temperature lag factor (how quickly temperature follows ambient)")]
        [Range(0f, 1f)]
        public float temperatureLagFactor = 0.1f;

        [Header("Geometry")]
        [Tooltip("Pond bounds for surface area calculation")]
        public Bounds pondBounds;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        private void Awake()
        {
            if (autoCalculateSurfaceArea)
            {
                CalculateSurfaceArea();
            }
        }

        /// <summary>
        /// Service update called by Water system
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Update temperature based on ambient (with lag)
            UpdateTemperature(deltaTime);

            // Update volume based on water level
            UpdateVolume();
        }

        /// <summary>
        /// Add water from precipitation or rivers
        /// </summary>
        public void AddWater(float volumeM3)
        {
            volume += volumeM3;

            // Update water level based on volume and surface area
            if (surfaceArea > 0f)
            {
                waterLevel = volume / surfaceArea;
            }
        }

        /// <summary>
        /// Calculate surface area from geometry
        /// </summary>
        public void CalculateSurfaceArea()
        {
            if (pondBounds.size.magnitude > 0f)
            {
                // Use bounds to estimate surface area
                surfaceArea = pondBounds.size.x * pondBounds.size.z;
            }
            else
            {
                // Fallback: use transform scale
                Vector3 scale = transform.lossyScale;
                surfaceArea = scale.x * scale.z;
            }
        }

        /// <summary>
        /// Get lake effect on local weather
        /// </summary>
        public LakeEffect GetLakeEffect()
        {
            if (!lakeEffectEnabled)
            {
                return new LakeEffect { temperatureModifier = 0f, humidityModifier = 0f };
            }

            // Lake effect: water temperature affects local air temperature and humidity
            // Warmer water increases local temperature and humidity
            // Colder water decreases local temperature and increases humidity (evaporation)

            float ambientTemp = 20f; // Would come from Meteorology system
            float tempDifference = temperature - ambientTemp;

            float temperatureModifier = tempDifference * 0.1f * naturalEntropy; // 10% of temp difference
            float humidityModifier = Mathf.Abs(tempDifference) * 0.5f * naturalEntropy; // Humidity increases with temp difference

            return new LakeEffect
            {
                temperatureModifier = temperatureModifier,
                humidityModifier = humidityModifier
            };
        }

        /// <summary>
        /// Update temperature based on ambient temperature (with lag)
        /// </summary>
        private void UpdateTemperature(float deltaTime)
        {
            // Would get ambient temperature from Meteorology system
            float ambientTemp = 20f; // Placeholder

            // Lagged update
            temperature = Mathf.Lerp(temperature, ambientTemp, temperatureLagFactor * deltaTime);
        }

        /// <summary>
        /// Update volume based on water level
        /// </summary>
        private void UpdateVolume()
        {
            volume = waterLevel * surfaceArea;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 position = transform.position;

            // Bounds wireframe box
            Color waterColor = new Color(0f, 0.5f, 1f, 0.6f);
            Gizmos.color = waterColor;

            if (pondBounds.size.magnitude > 0f)
            {
                Gizmos.DrawWireCube(position + pondBounds.center, pondBounds.size);
            }
            else
            {
                // Fallback: use transform scale
                Vector3 scale = transform.lossyScale;
                Gizmos.DrawWireCube(position, scale);
            }

            // Water level plane
            waterColor.a = 0.3f;
            Gizmos.color = waterColor;
            float size = Mathf.Sqrt(surfaceArea);
            Gizmos.DrawCube(
                position + Vector3.up * waterLevel,
                new Vector3(size, 0.1f, size)
            );

            // Surface area visualization
            Gizmos.color = new Color(waterColor.r, waterColor.g, waterColor.b, 0.2f);
            Gizmos.DrawWireSphere(position, Mathf.Sqrt(surfaceArea / Mathf.PI));
        }
    }

    /// <summary>
    /// Lake effect data structure
    /// </summary>
    public struct LakeEffect
    {
        public float temperatureModifier; // °C
        public float humidityModifier; // %
    }
}
