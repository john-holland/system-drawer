using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Water modes for different visual/behavioral states
    /// </summary>
    [System.Flags]
    public enum WaterMode
    {
        Still = 1 << 0,
        Rippling = 1 << 1,
        Wave = 1 << 2,
        Spray = 1 << 3,
        Rush = 1 << 4,
        WhiteWater = 1 << 5
    }

    /// <summary>
    /// Water system for prebaking water effects and connecting ponds, rivers, and rain.
    /// Uses Finite Volume Method for flow calculations.
    /// </summary>
    public class Water : MonoBehaviour
    {
        [Header("Water Parameters")]
        [Tooltip("Water level in meters (or mm relative to reference)")]
        public float waterLevel = 0f;

        [Tooltip("Flow rate in m³/s (for rivers)")]
        public float flowRate = 0f;

        [Tooltip("Volume in m³ (for ponds)")]
        public float volume = 0f;

        [Tooltip("Water modes (flags)")]
        public WaterMode waterModes = WaterMode.Still;

        [Header("Water Bodies")]
        [Tooltip("List of Pond components managed by this water system")]
        public List<Pond> ponds = new List<Pond>();

        [Tooltip("List of River components managed by this water system")]
        public List<River> rivers = new List<River>();

        [Tooltip("List of Dam components managed by this water system")]
        public List<Dam> dams = new List<Dam>();

        [Header("Configuration")]
        [Tooltip("Auto-find water bodies on start")]
        public bool autoFindWaterBodies = true;

        [Header("Terrain")]
        [Tooltip("Terrain for height map calculations (optional)")]
        public Terrain terrain;

        [Tooltip("Mesh terrain sampler for height calculations (optional, used if terrain is null)")]
        public MeshTerrainSampler meshTerrainSampler;

        [Tooltip("Auto-find terrain in scene if not assigned")]
        public bool autoFindTerrain = true;

        [Tooltip("Auto-find mesh terrain sampler if not assigned")]
        public bool autoFindMeshTerrainSampler = true;

        [Tooltip("Height multiplier for terrain heights")]
        public float terrainHeightMultiplier = 1f;

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        private void Start()
        {
            if (autoFindWaterBodies)
            {
                FindWaterBodies();
            }

            if (autoFindTerrain && terrain == null)
            {
                terrain = FindFirstObjectByType<Terrain>();
            }

            if (autoFindMeshTerrainSampler && meshTerrainSampler == null && terrain == null)
            {
                meshTerrainSampler = FindFirstObjectByType<MeshTerrainSampler>();
            }
        }

        /// <summary>
        /// Service update called by WeatherSystem
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Update flow calculations
            CalculateFlow();

            // Update water bodies
            UpdateWaterBodies(deltaTime);
        }

        /// <summary>
        /// Add water from precipitation
        /// </summary>
        public void AddWater(float volumeM3)
        {
            volume += volumeM3;

            // Distribute to ponds/rivers based on configuration
            DistributeWater(volumeM3);
        }

        /// <summary>
        /// Get water level at a specific position (from terrain or default)
        /// </summary>
        public float GetWaterLevelAt(Vector3 position)
        {
            float terrainHeight = GetTerrainHeightAt(position);
            return terrainHeight + waterLevel;
        }

        /// <summary>
        /// Get terrain height at a specific position (without water level offset)
        /// </summary>
        public float GetTerrainHeightAt(Vector3 position)
        {
            // Try Unity Terrain first
            if (terrain != null && terrain.terrainData != null)
            {
                float terrainHeight = terrain.SampleHeight(position);
                return terrainHeight * terrainHeightMultiplier;
            }

            // Fallback to mesh terrain sampler
            if (meshTerrainSampler != null)
            {
                float meshHeight = meshTerrainSampler.SampleHeight(position);
                return meshHeight * terrainHeightMultiplier;
            }

            return 0f;
        }

        /// <summary>
        /// Check if a position is within the terrain bounds
        /// </summary>
        public bool IsPositionInTerrain(Vector3 position)
        {
            // Try Unity Terrain first
            if (terrain != null && terrain.terrainData != null)
            {
                TerrainData terrainData = terrain.terrainData;
                Vector3 terrainPos = terrain.transform.position;
                
                // Check if position is within terrain bounds
                return position.x >= terrainPos.x && position.x <= terrainPos.x + terrainData.size.x &&
                       position.z >= terrainPos.z && position.z <= terrainPos.z + terrainData.size.z;
            }

            // Fallback to mesh terrain sampler
            if (meshTerrainSampler != null)
            {
                return meshTerrainSampler.IsPositionInBounds(position);
            }

            return false;
        }

        /// <summary>
        /// Calculate flow rates using Finite Volume Method
        /// </summary>
        public void CalculateFlow()
        {
            // Update flow for all rivers
            foreach (var river in rivers)
            {
                if (river != null)
                {
                    river.CalculateFlowRate();
                }
            }
        }

        /// <summary>
        /// Update all water bodies
        /// </summary>
        private void UpdateWaterBodies(float deltaTime)
        {
            // Update dams first (they block water)
            foreach (var dam in dams)
            {
                if (dam != null)
                {
                    dam.ServiceUpdate(deltaTime);
                }
            }

            // Update rivers
            foreach (var river in rivers)
            {
                if (river != null)
                {
                    river.ServiceUpdate(deltaTime);
                }
            }

            // Update ponds
            foreach (var pond in ponds)
            {
                if (pond != null)
                {
                    pond.ServiceUpdate(deltaTime);
                }
            }
        }

        /// <summary>
        /// Distribute water to ponds and rivers
        /// </summary>
        private void DistributeWater(float volumeM3)
        {
            // Simple distribution: split evenly between all water bodies
            int bodyCount = ponds.Count + rivers.Count;
            if (bodyCount == 0)
                return;

            float volumePerBody = volumeM3 / bodyCount;

            foreach (var pond in ponds)
            {
                if (pond != null)
                {
                    pond.AddWater(volumePerBody);
                }
            }

            foreach (var river in rivers)
            {
                if (river != null)
                {
                    river.AddWater(volumePerBody);
                }
            }
        }

        /// <summary>
        /// Find all water bodies in the scene
        /// </summary>
        private void FindWaterBodies()
        {
            ponds.Clear();
            ponds.AddRange(FindObjectsByType<Pond>(FindObjectsSortMode.None));

            rivers.Clear();
            rivers.AddRange(FindObjectsByType<River>(FindObjectsSortMode.None));

            dams.Clear();
            dams.AddRange(FindObjectsByType<Dam>(FindObjectsSortMode.None));
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Color waterColor = new Color(0f, 0.5f, 1f, 0.4f);

            // Terrain bounds visualization
            if (terrain != null && terrain.terrainData != null)
            {
                TerrainData terrainData = terrain.terrainData;
                Vector3 terrainPos = terrain.transform.position;
                Vector3 terrainSize = terrainData.size;

                Gizmos.color = waterColor;
                Gizmos.DrawWireCube(
                    terrainPos + terrainSize * 0.5f,
                    terrainSize
                );
            }
            else if (meshTerrainSampler != null)
            {
                // Draw mesh terrain bounds
                Bounds? bounds = meshTerrainSampler.GetMeshBounds(meshTerrainSampler.meshTerrain);
                if (bounds.HasValue)
                {
                    Gizmos.color = waterColor;
                    Gizmos.DrawWireCube(bounds.Value.center, bounds.Value.size);
                }
            }

            // Water level plane
            waterColor.a = 0.3f;
            Gizmos.color = waterColor;
            Gizmos.DrawCube(
                transform.position + Vector3.up * waterLevel,
                new Vector3(100f, 0.1f, 100f) // Default size, could be based on terrain
            );
        }
    }
}
