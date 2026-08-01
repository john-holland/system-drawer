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

        [Header("Advection (far field)")]
        public bool enableFarFieldAdvection = true;
        public int advectionStride = 4;
        public int advectionEveryNFrames = 4;
        public Transform advectionFocus;
        public float skipAdvectionAboveAltitudeM = 80000f;

        [Header("Egg LOD")]
        [Tooltip("When true, far-field advection only runs inside active egg bounds.")]
        public bool useEggLodMode = true;

        bool _eggLodActive;
        Bounds _activeEggBounds;
        bool _hasActiveEggBounds;

        [Header("Near field")]
        public NearField.NearFieldWindInteractionGraph nearFieldGraph;

        int _advectionFrame;
        ManifoldCellData[] _advectionScratch;

        void Awake()
        {
            EnsureCellDataAllocatedCore();
            if (nearFieldGraph == null)
                nearFieldGraph = FindAnyObjectByType<NearField.NearFieldWindInteractionGraph>();
            TryRegisterSceneService();
        }

        void OnEnable() => TryRegisterSceneService();

        void Start() => TryRegisterSceneService();

        void OnDestroy()
        {
            TryUnregisterSceneService();
        }

        /// <summary>Register under weather.physicsManifold without a SystemDrawer asmdef reference.</summary>
        void TryRegisterSceneService()
        {
            try
            {
                var serviceType = System.Type.GetType("SystemDrawerService, SystemDrawer");
                if (serviceType == null) return;
                var instanceProp = serviceType.GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var instance = instanceProp?.GetValue(null);
                if (instance == null) return;
                var register = serviceType.GetMethod("Register",
                    new[] { typeof(string), typeof(UnityEngine.Object) });
                register?.Invoke(instance, new object[] { "weather.physicsManifold", this });
            }
            catch
            {
                // Optional SystemDrawer package / service not present
            }
        }

        void TryUnregisterSceneService()
        {
            try
            {
                var serviceType = System.Type.GetType("SystemDrawerService, SystemDrawer");
                if (serviceType == null) return;
                var instanceProp = serviceType.GetProperty("Instance",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var instance = instanceProp?.GetValue(null);
                if (instance == null) return;
                var unregister = serviceType.GetMethod("Unregister", new[] { typeof(string) });
                unregister?.Invoke(instance, new object[] { "weather.physicsManifold" });
            }
            catch
            {
                // ignore
            }
        }

        void EnsureCellDataAllocated()
        {
            using (PerfTrace.Scope("EnsureCellDataAllocated"))
                EnsureCellDataAllocatedCore();
        }

        void EnsureCellDataAllocatedCore()
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
            using (PerfTrace.Scope("WeatherPhysicsManifold.ServiceUpdate"))
            {
                WindStampPass();
                AggregateSubsystemData();
                UpdateManifold(deltaTime);
            }
        }

        public void SetEggLodActive(bool active, Bounds eggBounds)
        {
            _eggLodActive = active;
            _hasActiveEggBounds = active;
            _activeEggBounds = eggBounds;
        }

        public void ServiceUpdateInBounds(float deltaTime, Bounds bounds)
        {
            using (PerfTrace.Scope("WeatherPhysicsManifold.ServiceUpdateInBounds"))
            {
                WindStampPassInBounds(bounds);
                AdvectFieldsInBounds(deltaTime, bounds);
            }
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
            if (useEggLodMode && _eggLodActive)
                return;

            AdvectFields(deltaTime);
            ProjectPressure(deltaTime);
            DiffuseFields(deltaTime);
            ApplyForces(deltaTime);
            ApplyMaterialInteractions(deltaTime);
        }

        /// <summary>Stamp analytic wind into cell velocities (far-field source term).</summary>
        public void WindStampPass()
        {
            if (_eggLodActive && _hasActiveEggBounds)
            {
                WindStampPassInBounds(_activeEggBounds);
                return;
            }
            WindStampPassInBounds(worldBounds);
        }

        public void WindStampPassInBounds(Bounds bounds)
        {
            using (PerfTrace.Scope("WindStampPass"))
            {
                if (wind == null || cellData == null || cellData.Length == 0)
                    return;

                EnsureCellDataAllocatedCore();
                int stride = Mathf.Max(1, advectionStride);
                IterateCellsInBounds(bounds, stride, (x, y, z) =>
                {
                    Vector3 cellCenter = CellIndexToWorldCenter(new Vector3Int(x, y, z));
                    float altitude = cellCenter.y;
                    Vector3 windVel = wind.GetWindAtPosition(cellCenter, altitude);
                    int flat = CellIndexToFlat(new Vector3Int(x, y, z));
                    if (flat < 0 || flat >= cellData.Length)
                        return;
                    ManifoldCellData d = cellData[flat];
                    d.velocity = windVel;
                    if (d.mode == WeatherMode.Air)
                        d.mode = WeatherMode.Wind;
                    cellData[flat] = d;
                });
            }
        }

        delegate void CellIterator(int x, int y, int z);

        void IterateCellsInBounds(Bounds bounds, int stride, CellIterator iterator)
        {
            Vector3Int minCell = WorldToCellIndex(bounds.min);
            Vector3Int maxCell = WorldToCellIndex(bounds.max);
            minCell = new Vector3Int(
                Mathf.Max(0, minCell.x),
                Mathf.Max(0, minCell.y),
                Mathf.Max(0, minCell.z));
            maxCell = new Vector3Int(
                Mathf.Min(cellCount.x - 1, maxCell.x),
                Mathf.Min(cellCount.y - 1, maxCell.y),
                Mathf.Min(cellCount.z - 1, maxCell.z));

            for (int z = minCell.z; z <= maxCell.z; z += stride)
            for (int y = minCell.y; y <= maxCell.y; y += stride)
            for (int x = minCell.x; x <= maxCell.x; x += stride)
                iterator(x, y, z);
        }

        Vector3 CellIndexToWorldCenter(Vector3Int cellIndex)
        {
            Vector3 t = new Vector3(
                (cellIndex.x + 0.5f) / cellCount.x,
                (cellIndex.y + 0.5f) / cellCount.y,
                (cellIndex.z + 0.5f) / cellCount.z);
            return worldBounds.min + Vector3.Scale(t, worldBounds.size);
        }

        private void AdvectFields(float deltaTime)
        {
            AdvectFieldsInBounds(deltaTime, worldBounds);
        }

        public void AdvectFieldsInBounds(float deltaTime, Bounds bounds)
        {
            using (PerfTrace.Scope("AdvectFields"))
            {
                if (!enableFarFieldAdvection || cellData == null || cellData.Length == 0 || deltaTime <= 0f)
                    return;

                if (useEggLodMode && !_eggLodActive)
                    return;

                if (advectionFocus != null && advectionFocus.position.y > skipAdvectionAboveAltitudeM)
                    return;

                _advectionFrame++;
                if (_advectionFrame % Mathf.Max(1, advectionEveryNFrames) != 0)
                    return;

                int stride = Mathf.Max(1, advectionStride);

                if (_advectionScratch == null || _advectionScratch.Length != cellData.Length)
                    _advectionScratch = new ManifoldCellData[cellData.Length];

                if (_hasActiveEggBounds && _eggLodActive)
                {
                    IterateCellsInBounds(bounds, stride, (x, y, z) =>
                    {
                        int flat = CellIndexToFlat(new Vector3Int(x, y, z));
                        if (flat >= 0 && flat < cellData.Length)
                            _advectionScratch[flat] = cellData[flat];
                    });
                }
                else
                {
                    System.Array.Copy(cellData, _advectionScratch, cellData.Length);
                }

                IterateCellsInBounds(bounds, stride, (x, y, z) =>
                {
                    Vector3Int idx = new Vector3Int(x, y, z);
                    int flat = CellIndexToFlat(idx);
                    if (flat < 0 || flat >= cellData.Length)
                        return;

                    Vector3 world = CellIndexToWorldCenter(idx);
                    Vector3 vel = _advectionScratch[flat].velocity;
                    Vector3 back = world - vel * deltaTime;
                    cellData[flat] = SampleCellAtWorld(back, _advectionScratch);
                });
            }
        }

        ManifoldCellData SampleCellAtWorld(Vector3 world, ManifoldCellData[] buffer)
        {
            Vector3Int idx = WorldToCellIndex(world);
            int flat = CellIndexToFlat(idx);
            if (flat < 0 || flat >= buffer.Length)
                return new ManifoldCellData();
            return buffer[flat];
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
            Vector3 v = GetDataAtPosition(position).velocity;
            if (nearFieldGraph != null)
                v = nearFieldGraph.GetBlendedVelocity(position, v);
            return v;
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
