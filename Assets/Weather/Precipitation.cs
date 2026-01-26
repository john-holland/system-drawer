using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Precipitation intensity levels
    /// </summary>
    public enum PrecipitationIntensity
    {
        None,
        Light,      // < 2.5 mm/h
        Moderate,   // 2.5-7.6 mm/h
        Heavy       // > 7.6 mm/h
    }

    /// <summary>
    /// Precipitation types
    /// </summary>
    public enum PrecipitationType
    {
        Rain,
        Snow,
        Sleet,
        Hail
    }

    /// <summary>
    /// Precipitation system for rain/snow/sleet/hail rendering and accumulation tracking.
    /// Uses Verlet integration for particle physics and links to PhysicsManifold for effects.
    /// </summary>
    public class Precipitation : MonoBehaviour
    {
        [Header("Precipitation Parameters")]
        [Tooltip("Precipitation rate in mm/h")]
        public float precipitationRate = 0f;

        [Tooltip("Precipitation intensity (calculated from rate)")]
        public PrecipitationIntensity intensity = PrecipitationIntensity.None;

        [Tooltip("Precipitation type")]
        public PrecipitationType type = PrecipitationType.Rain;

        [Tooltip("Total accumulation in mm")]
        public float accumulation = 0f;

        [Header("Physics Integration")]
        [Tooltip("Link to PhysicsManifold for particle effects")]
        public PhysicsManifold linkToPhysicsManifold;

        [Header("Particle System")]
        [Tooltip("Particle system for visual effects (optional)")]
        public new ParticleSystem particleSystem;

        [Tooltip("Auto-configure particle system from precipitation parameters")]
        public bool autoConfigureParticleSystem = true;

        [Header("Configuration")]
        [Tooltip("Temperature threshold for snow (below this, precipitation is snow)")]
        public float snowTemperatureThreshold = 0f; // Celsius

        [Tooltip("Temperature threshold for sleet (between rain and snow)")]
        public float sleetTemperatureThreshold = 2f; // Celsius

        [Tooltip("Wind drift factor (how much wind affects precipitation)")]
        [Range(0f, 1f)]
        public float windDriftFactor = 0.5f;

        [Header("Portal Rain Effects")]
        [Tooltip("List of portal rain particle systems")]
        public List<PortalRainParticleSystem> portalParticleSystems = new List<PortalRainParticleSystem>();

        [Tooltip("Auto-detect portals in scene and create rain effects")]
        public bool autoDetectPortals = false;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        [Tooltip("Precipitation area radius in meters")]
        public float precipitationArea = 100f;

        /// <summary>
        /// Service update called by WeatherSystem
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Calculate intensity from rate
            intensity = CalculateIntensity(precipitationRate);

            // Update accumulation
            UpdateAccumulation(deltaTime);

            // Update particle system if configured
            if (autoConfigureParticleSystem && particleSystem != null)
            {
                UpdateParticleSystem();
            }

            // Update portal particle systems
            UpdatePortalParticleSystems();
        }

        private void Start()
        {
            if (autoDetectPortals)
            {
                AutoDetectPortals();
            }
        }

        /// <summary>
        /// Calculate intensity from precipitation rate
        /// </summary>
        public PrecipitationIntensity CalculateIntensity(float rate)
        {
            if (rate <= 0f)
                return PrecipitationIntensity.None;
            else if (rate < 2.5f)
                return PrecipitationIntensity.Light;
            else if (rate < 7.6f)
                return PrecipitationIntensity.Moderate;
            else
                return PrecipitationIntensity.Heavy;
        }

        /// <summary>
        /// Update accumulation based on precipitation rate
        /// </summary>
        private void UpdateAccumulation(float deltaTime)
        {
            if (precipitationRate > 0f)
            {
                // Convert mm/h to mm per second, then multiply by deltaTime
                float ratePerSecond = precipitationRate / 3600f; // mm/h to mm/s
                accumulation += ratePerSecond * deltaTime;
            }
        }

        /// <summary>
        /// Get particle system parameters for visual effects
        /// </summary>
        public ParticleSystemParameters GetParticleSystemParameters()
        {
            return new ParticleSystemParameters
            {
                intensity = intensity,
                type = type,
                rate = precipitationRate,
                windDrift = windDriftFactor
            };
        }

        /// <summary>
        /// Update particle system configuration
        /// </summary>
        private void UpdateParticleSystem()
        {
            if (particleSystem == null)
                return;

            var emission = particleSystem.emission;
            var main = particleSystem.main;

            // Set emission rate based on intensity
            float emissionRate = 0f;
            switch (intensity)
            {
                case PrecipitationIntensity.Light:
                    emissionRate = 100f;
                    break;
                case PrecipitationIntensity.Moderate:
                    emissionRate = 500f;
                    break;
                case PrecipitationIntensity.Heavy:
                    emissionRate = 2000f;
                    break;
            }

            emission.rateOverTime = emissionRate;

            // Set particle properties based on type
            switch (type)
            {
                case PrecipitationType.Rain:
                    main.startLifetime = 2f;
                    main.startSpeed = 10f;
                    main.startSize = 0.1f;
                    break;
                case PrecipitationType.Snow:
                    main.startLifetime = 5f;
                    main.startSpeed = 2f;
                    main.startSize = 0.2f;
                    break;
                case PrecipitationType.Sleet:
                    main.startLifetime = 3f;
                    main.startSpeed = 5f;
                    main.startSize = 0.15f;
                    break;
                case PrecipitationType.Hail:
                    main.startLifetime = 1f;
                    main.startSpeed = 15f;
                    main.startSize = 0.3f;
                    break;
            }
        }

        /// <summary>
        /// Determine precipitation type from temperature
        /// </summary>
        public void UpdateTypeFromTemperature(float temperature)
        {
            if (temperature < snowTemperatureThreshold)
                type = PrecipitationType.Snow;
            else if (temperature < sleetTemperatureThreshold)
                type = PrecipitationType.Sleet;
            else
                type = PrecipitationType.Rain;
        }

        /// <summary>
        /// Reset accumulation
        /// </summary>
        public void ResetAccumulation()
        {
            accumulation = 0f;
        }

        /// <summary>
        /// Get water volume from accumulation (for feeding into water systems)
        /// </summary>
        public float GetWaterVolume(float areaM2)
        {
            // Accumulation is in mm, convert to m³
            // Volume (m³) = Accumulation (mm) / 1000 * Area (m²)
            return (accumulation / 1000f) * areaM2;
        }

        /// <summary>
        /// Get color for precipitation type
        /// </summary>
        private Color GetPrecipitationTypeColor()
        {
            switch (type)
            {
                case PrecipitationType.Rain:
                    return Color.blue;
                case PrecipitationType.Snow:
                    return Color.white;
                case PrecipitationType.Sleet:
                    return Color.gray;
                case PrecipitationType.Hail:
                    return Color.cyan;
                default:
                    return Color.blue;
            }
        }

        /// <summary>
        /// Register a portal for rain effects.
        /// </summary>
        public void RegisterPortalForRain(MeshTerrainPortal portal)
        {
            if (portal == null)
                return;

            // Check if already registered
            foreach (var existing in portalParticleSystems)
            {
                if (existing != null && existing.portal == portal)
                {
                    return; // Already registered
                }
            }

            // Create portal rain particle system
            GameObject portalRainObj = new GameObject($"PortalRain_{portal.name}");
            portalRainObj.transform.SetParent(portal.transform);
            portalRainObj.transform.localPosition = Vector3.zero;

            PortalRainParticleSystem portalRain = portalRainObj.AddComponent<PortalRainParticleSystem>();
            portalRain.portal = portal;
            portalRain.terrain = FindObjectOfType<Terrain>();

            portalParticleSystems.Add(portalRain);
        }

        /// <summary>
        /// Unregister a portal from rain effects.
        /// </summary>
        public void UnregisterPortalForRain(MeshTerrainPortal portal)
        {
            for (int i = portalParticleSystems.Count - 1; i >= 0; i--)
            {
                if (portalParticleSystems[i] != null && portalParticleSystems[i].portal == portal)
                {
                    if (portalParticleSystems[i].gameObject != null)
                    {
                        DestroyImmediate(portalParticleSystems[i].gameObject);
                    }
                    portalParticleSystems.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Auto-detect portals in scene and register them.
        /// </summary>
        public void AutoDetectPortals()
        {
            MeshTerrainPortal[] portals = FindObjectsOfType<MeshTerrainPortal>();
            foreach (var portal in portals)
            {
                RegisterPortalForRain(portal);
            }

            Debug.Log($"Precipitation: Auto-detected and registered {portals.Length} portals for rain effects");
        }

        /// <summary>
        /// Update all portal particle systems.
        /// </summary>
        private void UpdatePortalParticleSystems()
        {
            // Remove null references
            portalParticleSystems.RemoveAll(ps => ps == null);

            // Update each portal particle system
            foreach (var portalPS in portalParticleSystems)
            {
                if (portalPS != null)
                {
                    portalPS.UpdateParticleSystem();
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Vector3 position = transform.position;

            // Type-based color (reusing WeatherEvent color system)
            Color typeColor = GetPrecipitationTypeColor();

            // Intensity visualization (color intensity based on precipitationRate)
            float intensityNormalized = Mathf.InverseLerp(0f, 10f, precipitationRate);
            typeColor.a = 0.3f + intensityNormalized * 0.5f;

            // Area of effect sphere (reusing WeatherEvent sphere style)
            Gizmos.color = typeColor;
            Gizmos.DrawWireSphere(position, precipitationArea);

            // Intensity visualization (inner sphere size)
            if (precipitationRate > 0.1f)
            {
                float innerRadius = precipitationArea * 0.3f * intensityNormalized;
                Gizmos.color = new Color(typeColor.r, typeColor.g, typeColor.b, typeColor.a * 0.5f);
                Gizmos.DrawSphere(position, innerRadius);
            }

            // Accumulation visualization (height indicator)
            if (accumulation > 0.1f)
            {
                float accumulationHeight = Mathf.Clamp(accumulation * 0.01f, 0f, precipitationArea * 0.2f);
                Vector3 accumulationPos = position + Vector3.down * accumulationHeight;
                Gizmos.color = new Color(typeColor.r, typeColor.g, typeColor.b, 0.7f);
                Gizmos.DrawWireCube(accumulationPos, new Vector3(precipitationArea * 0.5f, 0.1f, precipitationArea * 0.5f));
            }
        }
    }

    /// <summary>
    /// Particle system parameters structure
    /// </summary>
    public struct ParticleSystemParameters
    {
        public PrecipitationIntensity intensity;
        public PrecipitationType type;
        public float rate;
        public float windDrift;
    }
}
