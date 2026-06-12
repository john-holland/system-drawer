using System.Collections.Generic;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Weather mode for manifold cells
    /// </summary>
    public enum WeatherMode
    {
        Air,
        Water,
        Rain,
        Cloud,
        Wind,
        Lava,
        MagmaPlume,
        RoadSurface,
        RoadShoulder,
        RoadDirt
    }

    public enum RoadSurfaceType
    {
        None,
        RoadSurface,
        RoadShoulder,
        RoadDirt
    }

    /// <summary>
    /// Manifold data for a single cell
    /// </summary>
    [System.Serializable]
    public struct ManifoldCellData
    {
        public Vector3 velocity; // m/s
        public float pressure; // hPa
        public float temperature; // °C
        public float density; // kg/m³
        public WeatherMode mode;
        public Vector3 lavaVelocity;
        public float gasPressure;
        public float surfaceTensionCoeff;
        public RoadSurfaceType roadSurfaceType;
        public float surfaceFriction;
        public float surfacePorosity;
    }

    /// <summary>
    /// WeatherPhysicsManifold: 3D matrix of velocities and mode coordinates in an oct tree.
    /// Includes water, rain, cloud, and wind data. Accessible from weather shaders.
    /// Enhanced with physical blocking and material interactions.
    /// </summary>
    public class WeatherPhysicsManifold : MonoBehaviour
    {
        [Header("Manifold Configuration")]
        [Tooltip("World bounds of the manifold")]
        public Bounds worldBounds = new Bounds(Vector3.zero, Vector3.one * 100f);

        [Tooltip("Cell resolution (world units per cell)")]
        public float cellResolution = 1f;

        [Tooltip("Number of cells in each dimension")]
        public Vector3Int cellCount = new Vector3Int(100, 100, 100);

        [Header("Spatial Tree")]
        [Tooltip("Use OctTree for spatial queries")]
        public bool useOctTree = true;

        // Note: Actual OctTree implementation would use SGOctTree from BedogaGenerator
        // For now, using a simplified structure

        [Header("Gizmos")]
        [Tooltip("Show gizmos in scene view")]
        public bool showGizmos = true;

        [Tooltip("Show cell grid visualization")]
        public bool showCellGrid = false;

        [Header("Data Storage")]
        [Tooltip("Manifold cell data (flattened 3D array)")]
        private ManifoldCellData[] cellData;

        [Header("Integration")]
        [Tooltip("Reference to Wind system")]
        public Wind wind;

        [Tooltip("Reference to Cloud system")]
        public Cloud cloud;

        [Tooltip("Reference to Precipitation system")]
        public Precipitation precipitation;

        [Tooltip("Reference to Water system")]
        public Water water;

        private void Awake()
        {
            EnsureCellDataAllocated();
        }

        void EnsureCellDataAllocated()
        {
            int totalCells = cellCount.x * cellCount.y * cellCount.z;
            if (totalCells <= 0)
            {
                cellData = System.Array.Empty<ManifoldCellData>();
                return;
            }

            if (cellData != null && cellData.Length == totalCells)
                return;

            cellData = new ManifoldCellData[totalCells];
            for (int i = 0; i < totalCells; i++)
            {
                cellData[i] = new ManifoldCellData
                {
                    velocity = Vector3.zero,
                    pressure = 1013.25f,
                    temperature = 20f,
                    density = 1.225f,
                    mode = WeatherMode.Air
                };
            }
        }

        /// <summary>
        /// Service update called by WeatherSystem (last in update order)
        /// </summary>
        public void ServiceUpdate(float deltaTime)
        {
            // Aggregate data from all subsystems
            AggregateSubsystemData();

            // Update manifold using operator splitting
            UpdateManifold(deltaTime);
        }

        /// <summary>
        /// Aggregate data from all weather subsystems
        /// </summary>
        private void AggregateSubsystemData()
        {
            // This would aggregate data from Wind, Cloud, Precipitation, Water systems
            // For now, placeholder implementation
        }

        /// <summary>
        /// Update manifold using operator splitting method
        /// </summary>
        private void UpdateManifold(float deltaTime)
        {
            // Operator splitting update scheme:
            // 1. Advection (Semi-Lagrangian)
            AdvectFields(deltaTime);

            // 2. Pressure Projection (Implicit pressure-Poisson solver)
            ProjectPressure(deltaTime);

            // 3. Diffusion (Implicit viscosity)
            DiffuseFields(deltaTime);

            // 4. Forces (External forces: wind, gravity, blocking objects)
            ApplyForces(deltaTime);

            // 5. Material Interactions (Apply material-specific effects)
            ApplyMaterialInteractions(deltaTime);
        }

        /// <summary>
        /// Advect fields using Semi-Lagrangian method
        /// </summary>
        private void AdvectFields(float deltaTime)
        {
            // Semi-Lagrangian advection: backtrace particles through velocity field
            // For each cell, trace back in time and sample from previous state
            // This is a simplified placeholder - full implementation would use proper advection
        }

        /// <summary>
        /// Project pressure to make velocity divergence-free
        /// </summary>
        private void ProjectPressure(float deltaTime)
        {
            // Pressure-Poisson solver to ensure mass conservation
            // This is a simplified placeholder - full implementation would solve pressure equation
        }

        /// <summary>
        /// Diffuse fields (temperature, moisture)
        /// </summary>
        private void DiffuseFields(float deltaTime)
        {
            // Implicit viscosity/diffusion solver
            // This is a simplified placeholder - full implementation would use implicit method
        }

        /// <summary>
        /// Apply external forces (wind, gravity, blocking objects)
        /// </summary>
        private void ApplyForces(float deltaTime)
        {
            // Apply wind forces
            if (wind != null)
            {
                // Would apply wind field to manifold cells
            }

            // Apply gravity
            Vector3 gravity = Physics.gravity;
            // Would apply gravity to velocity field
        }

        /// <summary>
        /// Apply material-specific interactions (dew, condensation, etc.)
        /// </summary>
        private void ApplyMaterialInteractions(float deltaTime)
        {
            // Would query blocking objects and apply material effects
            // This integrates with PhysicalWeatherObject components
        }

        /// <summary>
        /// Get manifold data at a specific world position
        /// </summary>
        public ManifoldCellData GetDataAtPosition(Vector3 position)
        {
            EnsureCellDataAllocated();
            if (cellData == null || cellData.Length == 0)
                return new ManifoldCellData();

            Vector3Int cellIndex = WorldToCellIndex(position);
            if (!IsValidCellIndex(cellIndex))
                return new ManifoldCellData();

            int flatIndex = CellIndexToFlat(cellIndex);
            if (flatIndex < 0 || flatIndex >= cellData.Length)
                return new ManifoldCellData();

            return cellData[flatIndex];
        }

        /// <summary>
        /// Set manifold data at a specific world position
        /// </summary>
        public void SetDataAtPosition(Vector3 position, ManifoldCellData data)
        {
            EnsureCellDataAllocated();
            if (cellData == null || cellData.Length == 0)
                return;

            Vector3Int cellIndex = WorldToCellIndex(position);
            if (!IsValidCellIndex(cellIndex))
                return;

            int flatIndex = CellIndexToFlat(cellIndex);
            if (flatIndex < 0 || flatIndex >= cellData.Length)
                return;

            cellData[flatIndex] = data;
        }

        /// <summary>
        /// Get velocity at a specific position
        /// </summary>
        public Vector3 GetVelocityAtPosition(Vector3 position)
        {
            return GetDataAtPosition(position).velocity;
        }

        /// <summary>
        /// Get pressure at a specific position
        /// </summary>
        public float GetPressureAtPosition(Vector3 position)
        {
            return GetDataAtPosition(position).pressure;
        }

        /// <summary>
        /// Get temperature at a specific position
        /// </summary>
        public float GetTemperatureAtPosition(Vector3 position)
        {
            return GetDataAtPosition(position).temperature;
        }

        /// <summary>
        /// Get shader parameters for rendering
        /// </summary>
        public ShaderParameters GetShaderParameters()
        {
            return new ShaderParameters
            {
                bounds = worldBounds,
                cellResolution = cellResolution,
                cellCount = cellCount
                // Would include texture/buffer references for GPU access
            };
        }

        /// <summary>
        /// Convert world position to cell index
        /// </summary>
        private Vector3Int WorldToCellIndex(Vector3 worldPos)
        {
            Vector3 localPos = worldPos - worldBounds.min;
            Vector3 size = worldBounds.size;
            int x = size.x > 0f ? Mathf.FloorToInt(localPos.x / size.x * cellCount.x) : 0;
            int y = size.y > 0f ? Mathf.FloorToInt(localPos.y / size.y * cellCount.y) : 0;
            int z = size.z > 0f ? Mathf.FloorToInt(localPos.z / size.z * cellCount.z) : 0;
            x = Mathf.Clamp(x, 0, Mathf.Max(0, cellCount.x - 1));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, cellCount.y - 1));
            z = Mathf.Clamp(z, 0, Mathf.Max(0, cellCount.z - 1));
            return new Vector3Int(x, y, z);
        }

        /// <summary>
        /// Convert cell index to flat array index
        /// </summary>
        private int CellIndexToFlat(Vector3Int cellIndex)
        {
            return cellIndex.x + cellIndex.y * cellCount.x + cellIndex.z * cellCount.x * cellCount.y;
        }

        /// <summary>
        /// Check if cell index is valid
        /// </summary>
        private bool IsValidCellIndex(Vector3Int cellIndex)
        {
            return cellIndex.x >= 0 && cellIndex.x < cellCount.x &&
                   cellIndex.y >= 0 && cellIndex.y < cellCount.y &&
                   cellIndex.z >= 0 && cellIndex.z < cellCount.z;
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos)
                return;

            Color manifoldColor = Color.magenta;
            manifoldColor.a = 0.6f;

            // Bounds wireframe box
            Gizmos.color = manifoldColor;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);

            // Optional cell grid visualization
            if (showCellGrid && cellResolution > 0f)
            {
                manifoldColor.a = 0.2f;
                Gizmos.color = manifoldColor;

                // Draw grid lines (simplified - only show a few cells for performance)
                int gridSteps = Mathf.Min(10, Mathf.Max(cellCount.x, cellCount.y, cellCount.z));
                Vector3 stepSize = worldBounds.size / gridSteps;

                for (int i = 0; i <= gridSteps; i++)
                {
                    // X-axis lines
                    Vector3 startX = worldBounds.min + new Vector3(0f, i * stepSize.y, 0f);
                    Vector3 endX = startX + new Vector3(worldBounds.size.x, 0f, 0f);
                    Gizmos.DrawLine(startX, endX);

                    // Z-axis lines
                    Vector3 startZ = worldBounds.min + new Vector3(0f, i * stepSize.y, 0f);
                    Vector3 endZ = startZ + new Vector3(0f, 0f, worldBounds.size.z);
                    Gizmos.DrawLine(startZ, endZ);
                }
            }
        }
    }

    /// <summary>
    /// Shader parameters structure
    /// </summary>
    public struct ShaderParameters
    {
        public Bounds bounds;
        public float cellResolution;
        public Vector3Int cellCount;
        // Would include texture/buffer references
    }
}
