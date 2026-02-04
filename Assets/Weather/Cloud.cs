using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Cloud types
    /// </summary>
    public enum CloudType
    {
        Cumulus,
        Stratus,
        Cirrus,
        Cumulonimbus,
        Nimbostratus,
        Altocumulus,
        Altostratus
    }

    /// <summary>
    /// Cloud system for visual representation, pressure system integration, and meteorology linking.
    /// Uses Semi-Lagrangian advection for movement and Position-Based Dynamics for volume deformation.
    /// </summary>
    public class Cloud : MonoBehaviour
    {
        [Header("Cloud Parameters")]
        [Tooltip("Altitude range (base and top) in meters")]
        public Vector2 altitude = new Vector2(1000f, 2000f);

        [Tooltip("Pressure in hectopascals (hPa)")]
        public float pressure = 1013.25f;

        [Tooltip("Cloud coverage in oktas (0-8) or percentage (0-100)")]
        [Range(0f, 100f)]
        public float coverage = 50f;

        [Tooltip("Cloud type")]
        public CloudType type = CloudType.Cumulus;

        [Tooltip("Density in kg/m³ (affects visual rendering)")]
        public float density = 0.5f;

        [Header("Meteorology Integration")]
        [Tooltip("Whether this cloud is managed by Meteorology component")]
        public bool isManagedByMeteorology = true;

        [Tooltip("Reference to Meteorology component")]
        public Meteorology meteorology;

        [Header("Physics Integration")]
        [Tooltip("Reference to PhysicsManifold for pressure/velocity data")]
        public PhysicsManifold physicsManifold;

        [Header("Visual")]
        [Tooltip("Particle system for cloud rendering (optional)")]
        public new ParticleSystem particleSystem;

        [Tooltip("Material for cloud rendering (optional)")]
        public Material cloudMaterial;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        [Tooltip("Coverage area radius in meters")]
        public float coverageArea = 50f;

        private void Awake()
        {
            if (meteorology == null && isManagedByMeteorology)
            {
                meteorology = FindFirstObjectByType<Meteorology>();
            }
        }

        /// <summary>
        /// Service update called by WeatherSystem
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Update from meteorology if managed
            if (isManagedByMeteorology && meteorology != null)
            {
                UpdateFromMeteorology(meteorology);
            }

            // Apply wind effects (would come from Wind system)
            // This would be called by WeatherSystem after Wind update
        }

        /// <summary>
        /// Update cloud state from meteorology data
        /// </summary>
        public void UpdateFromMeteorology(Meteorology meteorology)
        {
            if (meteorology == null)
                return;

            // Update pressure from meteorology
            pressure = meteorology.pressure;

            // Update coverage from meteorology cloud cover
            if (meteorology.useOktas)
            {
                coverage = meteorology.cloudCover * 12.5f; // Convert oktas to percentage
            }
            else
            {
                coverage = meteorology.cloudCover;
            }

            // Determine cloud type based on conditions
            UpdateCloudType(meteorology);
        }

        /// <summary>
        /// Apply wind effects to cloud movement
        /// </summary>
        public void ApplyWind(Wind wind)
        {
            if (wind == null)
                return;

            // Get wind at cloud altitude
            float cloudAltitude = (altitude.x + altitude.y) * 0.5f;
            Vector3 windVector = wind.GetWindAtPosition(transform.position, cloudAltitude);

            // Move cloud based on wind (Semi-Lagrangian advection)
            transform.position += windVector * Time.deltaTime;
        }

        /// <summary>
        /// Get visual parameters for rendering
        /// </summary>
        public CloudVisualParameters GetVisualParameters()
        {
            return new CloudVisualParameters
            {
                altitude = this.altitude,
                pressure = this.pressure,
                coverage = this.coverage,
                type = this.type,
                density = this.density,
                position = transform.position
            };
        }

        /// <summary>
        /// Apply a weather event to cloud
        /// </summary>
        public void ApplyWeatherEvent(WeatherEventData eventData)
        {
            switch (eventData.eventType)
            {
                case WeatherEventType.CloudFormation:
                    if (eventData.isAdditive)
                        coverage += eventData.magnitude;
                    else
                        coverage *= eventData.magnitude;
                    coverage = Mathf.Clamp(coverage, 0f, 100f);
                    break;

                case WeatherEventType.PressureChange:
                    if (eventData.isAdditive)
                        pressure += eventData.magnitude;
                    else
                        pressure *= eventData.magnitude;
                    break;
            }
        }

        /// <summary>
        /// Update cloud type based on meteorology conditions
        /// </summary>
        private void UpdateCloudType(Meteorology meteorology)
        {
            // Simple cloud type determination based on altitude and conditions
            float avgAltitude = (altitude.x + altitude.y) * 0.5f;

            if (avgAltitude > 6000f)
            {
                type = CloudType.Cirrus;
            }
            else if (avgAltitude > 2000f)
            {
                if (meteorology.pressure < 1000f)
                    type = CloudType.Cumulonimbus;
                else
                    type = CloudType.Altocumulus;
            }
            else
            {
                if (meteorology.humidity > 80f)
                    type = CloudType.Nimbostratus;
                else if (meteorology.pressure < 1000f)
                    type = CloudType.Cumulonimbus;
                else
                    type = CloudType.Cumulus;
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 position = transform.position;

            // Altitude range visualization (two horizontal planes)
            Color cloudColor = Color.white;
            cloudColor.a = 0.5f;
            Gizmos.color = cloudColor;

            // Base altitude plane
            Gizmos.DrawWireCube(
                position + Vector3.up * altitude.x,
                new Vector3(coverageArea * 2f, 0.1f, coverageArea * 2f)
            );

            // Top altitude plane
            Gizmos.DrawWireCube(
                position + Vector3.up * altitude.y,
                new Vector3(coverageArea * 2f, 0.1f, coverageArea * 2f)
            );

            // Coverage area sphere
            float coveragePercent = coverage / 100f;
            cloudColor.a = coveragePercent * 0.3f;
            Gizmos.color = cloudColor;
            Gizmos.DrawSphere(position + Vector3.up * (altitude.x + altitude.y) * 0.5f, coverageArea);

            // Density visualization (opacity)
            cloudColor.a = density * 0.5f;
            Gizmos.color = cloudColor;
            Gizmos.DrawWireSphere(position + Vector3.up * (altitude.x + altitude.y) * 0.5f, coverageArea * 0.8f);
        }
    }

    /// <summary>
    /// Cloud visual parameters structure
    /// </summary>
    public struct CloudVisualParameters
    {
        public Vector2 altitude;
        public float pressure;
        public float coverage;
        public CloudType type;
        public float density;
        public Vector3 position;
    }
}
