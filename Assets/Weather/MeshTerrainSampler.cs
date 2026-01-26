using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Represents a loop of vertices that form an opening/entrance to an enclosed space.
    /// </summary>
    [System.Serializable]
    public class VertexLoop
    {
        [Tooltip("World space vertex positions forming the loop")]
        public List<Vector3> vertices = new List<Vector3>();

        [Tooltip("UV coordinates for each vertex")]
        public List<Vector2> uvs = new List<Vector2>();

        [Tooltip("Average normal of the opening")]
        public Vector3 normal = Vector3.up;

        [Tooltip("Center point of the loop")]
        public Vector3 center = Vector3.zero;

        [Tooltip("Estimated area of the opening")]
        public float area = 0f;

        [Tooltip("True if opening is primarily vertical")]
        public bool isVertical = false;

        /// <summary>
        /// Calculate center and area from vertices.
        /// </summary>
        public void CalculateProperties()
        {
            if (vertices == null || vertices.Count < 3)
            {
                center = Vector3.zero;
                area = 0f;
                return;
            }

            // Calculate center
            center = Vector3.zero;
            foreach (var vertex in vertices)
            {
                center += vertex;
            }
            center /= vertices.Count;

            // Calculate area using cross product (polygon area formula)
            area = 0f;
            for (int i = 0; i < vertices.Count; i++)
            {
                int next = (i + 1) % vertices.Count;
                area += Vector3.Cross(vertices[i] - center, vertices[next] - center).magnitude * 0.5f;
            }

            // Calculate average normal
            normal = Vector3.zero;
            for (int i = 0; i < vertices.Count; i++)
            {
                int next = (i + 1) % vertices.Count;
                Vector3 edge1 = vertices[i] - center;
                Vector3 edge2 = vertices[next] - center;
                normal += Vector3.Cross(edge1, edge2).normalized;
            }
            normal = (normal / vertices.Count).normalized;

            // Check if vertical (normal is primarily up/down)
            isVertical = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.7f;
        }
    }

    /// <summary>
    /// Represents a detected enclosed or concave space in the mesh.
    /// </summary>
    [System.Serializable]
    public class EnclosedSpace
    {
        [Tooltip("Bounding box of the enclosed space")]
        public Bounds bounds;

        [Tooltip("Center point of the space")]
        public Vector3 center = Vector3.zero;

        [Tooltip("Estimated volume")]
        public float volume = 0f;

        [Tooltip("List of openings/entrances")]
        public List<VertexLoop> openings = new List<VertexLoop>();

        [Tooltip("True if space has vertical entrances")]
        public bool hasVerticalEntrance = false;

        [Tooltip("Lowest Y coordinate in the space (floor level)")]
        public float lowestPoint = 0f;

        [Tooltip("Highest Y coordinate of any opening")]
        public float highestOpening = 0f;

        [Tooltip("True if space can drain (has opening at or below lowest point)")]
        public bool willDrain = false;

        [Tooltip("True if space will fill (all openings above lowest point)")]
        public bool willFill = false;

        [Tooltip("Spaces connected through portals (recursive)")]
        [System.NonSerialized]
        public List<EnclosedSpace> connectedSpaces = new List<EnclosedSpace>();

        [Tooltip("Path of spaces water would drain through")]
        [System.NonSerialized]
        public List<EnclosedSpace> drainPath = new List<EnclosedSpace>();

        /// <summary>
        /// Calculate properties from bounds.
        /// </summary>
        public void CalculateProperties()
        {
            center = bounds.center;
            volume = bounds.size.x * bounds.size.y * bounds.size.z;

            // Check for vertical entrances
            hasVerticalEntrance = false;
            foreach (var opening in openings)
            {
                if (opening.isVertical)
                {
                    hasVerticalEntrance = true;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Portal type classification.
    /// </summary>
    public enum PortalType
    {
        Horizontal,  // Primarily horizontal opening (doorway, window)
        Vertical,    // Primarily vertical opening (hatch, skylight)
        Mixed        // Mixed orientation
    }

    /// <summary>
    /// Component that provides height sampling for mesh terrains using raycasting.
    /// Detects enclosed spaces and creates portal GameObjects with vertex loop information.
    /// </summary>
    public class MeshTerrainSampler : MonoBehaviour
    {
        [Header("Mesh Terrain")]
        [Tooltip("Reference to mesh terrain GameObject (auto-found if null)")]
        public Transform meshTerrain;

        [Header("Raycast Settings")]
        [Tooltip("Maximum raycast distance (meters)")]
        public float raycastDistance = 1000f;

        [Tooltip("Layers to raycast against")]
        public LayerMask layerMask = ~0;

        [Header("Height Cache")]
        [Tooltip("Enable spatial caching for performance")]
        public bool useHeightCache = true;

        [Tooltip("Grid resolution for height cache (cells per axis)")]
        public int cacheResolution = 100;

        [Header("Enclosed Space Detection")]
        [Tooltip("Minimum volume for enclosed space detection (m³)")]
        public float minEnclosedVolume = 1f;

        [Tooltip("Minimum opening area to consider (m²)")]
        public float minOpeningArea = 0.1f;

        [Tooltip("Number of samples per axis for space detection")]
        public int detectionSamples = 20;

        [Tooltip("Enable recursive drain/flood analysis and portal creation")]
        public bool recursiveDrainFloodPortalCreation = false;

        [Tooltip("Number of samples for finding lowest point in space")]
        public int lowestPointSamples = 50;

        [Header("Detected Data")]
        [Tooltip("List of detected enclosed spaces")]
        public List<EnclosedSpace> enclosedSpaces = new List<EnclosedSpace>();

        [Tooltip("List of created portal GameObjects")]
        public List<MeshTerrainPortal> portals = new List<MeshTerrainPortal>();

        // Internal state
        private Dictionary<Vector2Int, float> heightCache = new Dictionary<Vector2Int, float>();
        private Bounds? cachedBounds = null;
        private float cacheCellSize = 1f;

        private void Awake()
        {
            // Auto-find mesh terrain if not assigned
            if (meshTerrain == null)
            {
                meshTerrain = transform;
            }

            // Initialize cache
            if (useHeightCache)
            {
                UpdateCacheBounds();
            }
        }

        /// <summary>
        /// Sample height at a world position using top-down raycasting.
        /// </summary>
        public float SampleHeight(Vector3 worldPosition)
        {
            // Check cache first
            if (useHeightCache)
            {
                Vector2Int cacheKey = GetCacheKey(worldPosition);
                if (heightCache.TryGetValue(cacheKey, out float cachedHeight))
                {
                    return cachedHeight;
                }
            }

            // Raycast from above
            Vector3 rayStart = worldPosition + Vector3.up * raycastDistance;
            RaycastHit hit;

            if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance * 2f, layerMask))
            {
                float height = hit.point.y;

                // Cache result
                if (useHeightCache)
                {
                    Vector2Int cacheKey = GetCacheKey(worldPosition);
                    heightCache[cacheKey] = height;
                }

                return height;
            }

            // No hit - return original Y position
            return worldPosition.y;
        }

        /// <summary>
        /// Get height at position (public API, same as SampleHeight).
        /// </summary>
        public float GetHeightAt(Vector3 position)
        {
            return SampleHeight(position);
        }

        /// <summary>
        /// Detect enclosed spaces in the mesh terrain.
        /// </summary>
        public void DetectEnclosedSpaces()
        {
            enclosedSpaces.Clear();

            if (meshTerrain == null)
            {
                Debug.LogWarning("MeshTerrainSampler: No mesh terrain assigned");
                return;
            }

            // Get all mesh renderers in the terrain
            MeshRenderer[] renderers = meshTerrain.GetComponentsInChildren<MeshRenderer>();
            if (renderers == null || renderers.Length == 0)
            {
                Debug.LogWarning("MeshTerrainSampler: No mesh renderers found");
                return;
            }

            // Get overall bounds
            Bounds? boundsNullable = GetMeshBounds(meshTerrain);
            if (!boundsNullable.HasValue)
            {
                Debug.LogWarning("MeshTerrainSampler: Could not determine mesh bounds");
                return;
            }
            Bounds overallBounds = boundsNullable.Value;
            if (overallBounds.size.magnitude < 0.001f)
            {
                Debug.LogWarning("MeshTerrainSampler: Invalid mesh bounds");
                return;
            }

            // Sample points in 3D grid to find enclosed regions
            List<Vector3> enclosedPoints = new List<Vector3>();
            float sampleSpacing = Mathf.Min(overallBounds.size.x, overallBounds.size.y, overallBounds.size.z) / detectionSamples;

            for (int x = 0; x < detectionSamples; x++)
            {
                for (int y = 0; y < detectionSamples; y++)
                {
                    for (int z = 0; z < detectionSamples; z++)
                    {
                        Vector3 samplePos = overallBounds.min + new Vector3(
                            x * sampleSpacing,
                            y * sampleSpacing,
                            z * sampleSpacing
                        );

                        // Check if point is inside mesh (raycast in multiple directions)
                        if (IsPointInsideMesh(samplePos, renderers))
                        {
                            enclosedPoints.Add(samplePos);
                        }
                    }
                }
            }

            // Cluster enclosed points into spaces
            List<List<Vector3>> clusters = ClusterPoints(enclosedPoints, sampleSpacing * 2f);

            // Create EnclosedSpace for each cluster
            foreach (var cluster in clusters)
            {
                if (cluster.Count < 3)
                    continue;

                EnclosedSpace space = new EnclosedSpace();
                
                // Calculate bounds
                Vector3 min = cluster[0];
                Vector3 max = cluster[0];
                foreach (var point in cluster)
                {
                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                }
                space.bounds = new Bounds((min + max) * 0.5f, max - min);
                space.CalculateProperties();

                // Check minimum volume
                if (space.volume < minEnclosedVolume)
                    continue;

                // Find openings for this space
                FindOpenings(space, renderers);
                
                // Analyze drain/fill behavior
                AnalyzeDrainFill(space);
                
                enclosedSpaces.Add(space);
            }

            // Recursively explore connected spaces if enabled
            if (recursiveDrainFloodPortalCreation)
            {
                RecursivelyExploreConcaveSpaces();
            }

            Debug.Log($"MeshTerrainSampler: Detected {enclosedSpaces.Count} enclosed spaces");
        }

        /// <summary>
        /// Find openings/vertex loops for an enclosed space.
        /// </summary>
        public void FindOpenings(EnclosedSpace space, MeshRenderer[] renderers)
        {
            space.openings.Clear();

            // Raycast from space center outward in multiple directions
            int rayCount = 32;
            float rayDistance = space.bounds.size.magnitude * 2f;

            List<RaycastHit> exitHits = new List<RaycastHit>();

            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i / (float)rayCount) * 360f;
                float elevation = (i % 8) / 8f * 180f - 90f; // -90 to 90 degrees

                Vector3 direction = Quaternion.Euler(elevation, angle, 0f) * Vector3.forward;
                RaycastHit hit;

                if (Physics.Raycast(space.center, direction, out hit, rayDistance, layerMask))
                {
                    // Check if this is an exit point (hit is far from center)
                    if (Vector3.Distance(hit.point, space.center) > space.bounds.size.magnitude * 0.5f)
                    {
                        exitHits.Add(hit);
                    }
                }
            }

            // Group exit hits by proximity to find openings
            List<List<RaycastHit>> openingGroups = GroupHitsByProximity(exitHits, 0.5f);

            // Extract vertex loops from opening groups
            foreach (var group in openingGroups)
            {
                if (group.Count < 3)
                    continue;

                VertexLoop loop = ExtractVertexLoop(group, renderers);
                if (loop != null && loop.area >= minOpeningArea)
                {
                    loop.CalculateProperties();
                    space.openings.Add(loop);
                }
            }

            space.CalculateProperties();
        }

        /// <summary>
        /// Create portal GameObjects for all detected openings.
        /// </summary>
        public void CreatePortals()
        {
            // Clear existing portals
            foreach (var portal in portals)
            {
                if (portal != null)
                {
                    DestroyImmediate(portal.gameObject);
                }
            }
            portals.Clear();

            // Create portal for each opening
            for (int spaceIdx = 0; spaceIdx < enclosedSpaces.Count; spaceIdx++)
            {
                var space = enclosedSpaces[spaceIdx];
                for (int openingIdx = 0; openingIdx < space.openings.Count; openingIdx++)
                {
                    var opening = space.openings[openingIdx];
                    GameObject portalObj = new GameObject($"Portal_{spaceIdx}_{openingIdx}");
                    portalObj.transform.SetParent(meshTerrain != null ? meshTerrain : transform);
                    portalObj.transform.position = opening.center;
                    portalObj.transform.rotation = Quaternion.LookRotation(opening.normal);

                    MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
                    portal.vertexLoop = opening;
                    portal.enclosedSpace = space;
                    portal.portalType = opening.isVertical ? PortalType.Vertical : PortalType.Horizontal;

                    portals.Add(portal);
                }
            }

            // If recursive mode enabled, create portals for inter-space connections
            if (recursiveDrainFloodPortalCreation)
            {
                CreateInterSpacePortals();
            }

            Debug.Log($"MeshTerrainSampler: Created {portals.Count} portal GameObjects");
        }

        /// <summary>
        /// Create portals for connections between spaces (recursive mode).
        /// </summary>
        private void CreateInterSpacePortals()
        {
            for (int i = 0; i < enclosedSpaces.Count; i++)
            {
                var space1 = enclosedSpaces[i];
                foreach (var space2 in space1.connectedSpaces)
                {
                    int space2Idx = enclosedSpaces.IndexOf(space2);
                    if (space2Idx <= i) // Avoid duplicate portals
                        continue;

                    // Find the opening that connects these spaces
                    VertexLoop connectionLoop = FindConnectionLoop(space1, space2);
                    if (connectionLoop != null)
                    {
                        GameObject portalObj = new GameObject($"InterSpacePortal_{i}_{space2Idx}");
                        portalObj.transform.SetParent(meshTerrain != null ? meshTerrain : transform);
                        portalObj.transform.position = connectionLoop.center;
                        portalObj.transform.rotation = Quaternion.LookRotation(connectionLoop.normal);

                        MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
                        portal.vertexLoop = connectionLoop;
                        portal.enclosedSpace = space1;
                        portal.portalType = connectionLoop.isVertical ? PortalType.Vertical : PortalType.Horizontal;
                        portal.gizmoColor = new Color(0f, 1f, 0.5f, 0.8f); // Green for inter-space portals

                        portals.Add(portal);
                    }
                }
            }
        }

        /// <summary>
        /// Find the vertex loop that connects two spaces.
        /// </summary>
        private VertexLoop FindConnectionLoop(EnclosedSpace space1, EnclosedSpace space2)
        {
            // Find opening in space1 that points toward space2
            foreach (var opening in space1.openings)
            {
                Vector3 direction = (space2.center - opening.center).normalized;
                RaycastHit hit;
                if (Physics.Raycast(opening.center, direction, out hit, Vector3.Distance(opening.center, space2.center) * 1.5f, layerMask))
                {
                    if (space2.bounds.Contains(hit.point))
                    {
                        return opening;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Analyze if an enclosed space will drain or fill based on opening heights.
        /// </summary>
        public void AnalyzeDrainFill(EnclosedSpace space)
        {
            if (space == null)
                return;

            // Find lowest point in the space
            space.lowestPoint = FindLowestPointInSpace(space);

            // Find highest and lowest opening heights
            space.highestOpening = float.MinValue;
            float lowestOpening = float.MaxValue;

            foreach (var opening in space.openings)
            {
                if (opening.vertices == null || opening.vertices.Count == 0)
                    continue;

                // Find lowest Y coordinate in this opening
                float openingMinY = opening.vertices.Min(v => v.y);
                float openingMaxY = opening.vertices.Max(v => v.y);

                if (openingMinY < lowestOpening)
                    lowestOpening = openingMinY;
                if (openingMaxY > space.highestOpening)
                    space.highestOpening = openingMaxY;
            }

            // Determine drain/fill behavior
            // If any opening is at or below the lowest point, space can drain
            space.willDrain = lowestOpening <= space.lowestPoint + 0.01f; // Small tolerance for floating point

            // If all openings are above the lowest point, space will fill
            space.willFill = !space.willDrain && space.openings.Count > 0;

            // If no openings, it's fully enclosed (will fill)
            if (space.openings.Count == 0)
            {
                space.willFill = true;
                space.willDrain = false;
            }
        }

        /// <summary>
        /// Find the lowest point (floor) in an enclosed space.
        /// </summary>
        private float FindLowestPointInSpace(EnclosedSpace space)
        {
            float lowestY = float.MaxValue;

            // Sample points within the space bounds
            int samplesX = Mathf.CeilToInt(Mathf.Sqrt(lowestPointSamples));
            int samplesY = Mathf.CeilToInt(Mathf.Sqrt(lowestPointSamples));
            int samplesZ = Mathf.CeilToInt(Mathf.Sqrt(lowestPointSamples));

            Vector3 min = space.bounds.min;
            Vector3 max = space.bounds.max;
            Vector3 size = space.bounds.size;

            for (int x = 0; x < samplesX; x++)
            {
                for (int y = 0; y < samplesY; y++)
                {
                    for (int z = 0; z < samplesZ; z++)
                    {
                        Vector3 samplePos = min + new Vector3(
                            (x / (float)(samplesX - 1)) * size.x,
                            (y / (float)(samplesY - 1)) * size.y,
                            (z / (float)(samplesZ - 1)) * size.z
                        );

                        // Check if point is inside mesh
                        if (IsPointInsideMesh(samplePos, meshTerrain.GetComponentsInChildren<MeshRenderer>()))
                        {
                            // Raycast down to find actual floor
                            RaycastHit hit;
                            if (Physics.Raycast(samplePos + Vector3.up * 0.1f, Vector3.down, out hit, 10f, layerMask))
                            {
                                if (hit.point.y < lowestY)
                                {
                                    lowestY = hit.point.y;
                                }
                            }
                            else if (samplePos.y < lowestY)
                            {
                                lowestY = samplePos.y;
                            }
                        }
                    }
                }
            }

            // Fallback to bounds min if no valid samples
            if (lowestY == float.MaxValue)
            {
                lowestY = space.bounds.min.y;
            }

            return lowestY;
        }

        /// <summary>
        /// Recursively explore concave spaces and build connections.
        /// </summary>
        public void RecursivelyExploreConcaveSpaces()
        {
            if (enclosedSpaces == null || enclosedSpaces.Count == 0)
                return;

            // Clear existing connections
            foreach (var space in enclosedSpaces)
            {
                space.connectedSpaces.Clear();
                space.drainPath.Clear();
            }

            // Build adjacency graph of connected spaces
            for (int i = 0; i < enclosedSpaces.Count; i++)
            {
                var space1 = enclosedSpaces[i];
                for (int j = i + 1; j < enclosedSpaces.Count; j++)
                {
                    var space2 = enclosedSpaces[j];

                    // Check if spaces are connected through openings
                    if (AreSpacesConnected(space1, space2))
                    {
                        if (!space1.connectedSpaces.Contains(space2))
                            space1.connectedSpaces.Add(space2);
                        if (!space2.connectedSpaces.Contains(space1))
                            space2.connectedSpaces.Add(space1);
                    }
                }
            }

            // Build drain paths for each space
            foreach (var space in enclosedSpaces)
            {
                BuildDrainPath(space);
            }

            Debug.Log($"MeshTerrainSampler: Explored {enclosedSpaces.Count} spaces, found connections");
        }

        /// <summary>
        /// Check if two spaces are connected through openings.
        /// </summary>
        private bool AreSpacesConnected(EnclosedSpace space1, EnclosedSpace space2)
        {
            // Check if any opening in space1 points toward space2
            foreach (var opening in space1.openings)
            {
                // Raycast from opening center toward space2 center
                Vector3 direction = (space2.center - opening.center).normalized;
                float distance = Vector3.Distance(opening.center, space2.center);

                RaycastHit hit;
                if (Physics.Raycast(opening.center, direction, out hit, distance * 1.5f, layerMask))
                {
                    // Check if hit point is inside space2
                    if (IsPointInsideMesh(hit.point, meshTerrain.GetComponentsInChildren<MeshRenderer>()))
                    {
                        // Verify hit point is within space2 bounds
                        if (space2.bounds.Contains(hit.point))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Build drain path for a space (sequence of spaces water would flow through).
        /// </summary>
        public void BuildDrainPath(EnclosedSpace startSpace)
        {
            if (startSpace == null || !startSpace.willDrain)
            {
                startSpace.drainPath.Clear();
                return;
            }

            startSpace.drainPath.Clear();
            HashSet<EnclosedSpace> visited = new HashSet<EnclosedSpace>();
            List<EnclosedSpace> path = new List<EnclosedSpace>();

            // Use depth-first search to find drain path
            BuildDrainPathRecursive(startSpace, visited, path);

            startSpace.drainPath = path;
        }

        /// <summary>
        /// Recursive helper for building drain path.
        /// </summary>
        private void BuildDrainPathRecursive(EnclosedSpace currentSpace, HashSet<EnclosedSpace> visited, List<EnclosedSpace> path)
        {
            if (currentSpace == null || visited.Contains(currentSpace))
                return;

            visited.Add(currentSpace);
            path.Add(currentSpace);

            // Find connected space with lowest opening (water flows to lowest point)
            EnclosedSpace nextSpace = null;
            float lowestConnection = float.MaxValue;

            foreach (var connectedSpace in currentSpace.connectedSpaces)
            {
                if (visited.Contains(connectedSpace))
                    continue;

                // Find the connecting opening's lowest point
                float connectionHeight = FindConnectionHeight(currentSpace, connectedSpace);
                if (connectionHeight < lowestConnection)
                {
                    lowestConnection = connectionHeight;
                    nextSpace = connectedSpace;
                }
            }

            // Continue path if found lower connected space
            if (nextSpace != null && nextSpace.willDrain)
            {
                BuildDrainPathRecursive(nextSpace, visited, path);
            }
        }

        /// <summary>
        /// Find the height of the connection between two spaces.
        /// </summary>
        private float FindConnectionHeight(EnclosedSpace space1, EnclosedSpace space2)
        {
            float lowestHeight = float.MaxValue;

            // Check openings in space1 that might connect to space2
            foreach (var opening in space1.openings)
            {
                Vector3 direction = (space2.center - opening.center).normalized;
                RaycastHit hit;
                if (Physics.Raycast(opening.center, direction, out hit, Vector3.Distance(opening.center, space2.center) * 1.5f, layerMask))
                {
                    if (space2.bounds.Contains(hit.point))
                    {
                        float openingMinY = opening.vertices.Min(v => v.y);
                        if (openingMinY < lowestHeight)
                            lowestHeight = openingMinY;
                    }
                }
            }

            return lowestHeight == float.MaxValue ? space1.lowestPoint : lowestHeight;
        }

        /// <summary>
        /// Clear height cache.
        /// </summary>
        public void ClearCache()
        {
            heightCache.Clear();
            cachedBounds = null;
        }

        /// <summary>
        /// Update cache bounds from mesh terrain.
        /// </summary>
        private void UpdateCacheBounds()
        {
            if (meshTerrain == null)
                return;

            Bounds? bounds = GetMeshBounds(meshTerrain);
            if (bounds.HasValue)
            {
                cachedBounds = bounds;
                cacheCellSize = Mathf.Max(bounds.Value.size.x, bounds.Value.size.z) / cacheResolution;
            }
        }

        /// <summary>
        /// Get cache key for a position.
        /// </summary>
        private Vector2Int GetCacheKey(Vector3 position)
        {
            if (!cachedBounds.HasValue)
            {
                UpdateCacheBounds();
            }

            if (cachedBounds.HasValue)
            {
                Vector3 localPos = position - cachedBounds.Value.min;
                int x = Mathf.FloorToInt(localPos.x / cacheCellSize);
                int z = Mathf.FloorToInt(localPos.z / cacheCellSize);
                return new Vector2Int(x, z);
            }

            return Vector2Int.zero;
        }

        /// <summary>
        /// Get mesh bounds from a Transform (recursive).
        /// </summary>
        public Bounds? GetMeshBounds(Transform transform)
        {
            if (transform == null)
                return null;

            Bounds? combinedBounds = null;

            MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();
            if (meshRenderer != null && meshRenderer.bounds.size.magnitude > 0.001f)
            {
                combinedBounds = meshRenderer.bounds;
            }
            else
            {
                MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null)
                {
                    Bounds meshBounds = meshFilter.sharedMesh.bounds;
                    Vector3 center = transform.TransformPoint(meshBounds.center);
                    Vector3 size = transform.TransformVector(meshBounds.size);
                    combinedBounds = new Bounds(center, size);
                }
            }

            // Check children
            for (int i = 0; i < transform.childCount; i++)
            {
                Bounds? childBounds = GetMeshBounds(transform.GetChild(i));
                if (childBounds.HasValue)
                {
                    if (combinedBounds.HasValue)
                    {
                        combinedBounds.Value.Encapsulate(childBounds.Value);
                    }
                    else
                    {
                        combinedBounds = childBounds;
                    }
                }
            }

            return combinedBounds;
        }

        /// <summary>
        /// Check if a point is inside the mesh by raycasting in multiple directions.
        /// </summary>
        private bool IsPointInsideMesh(Vector3 point, MeshRenderer[] renderers)
        {
            // Raycast in 6 directions (up, down, left, right, forward, back)
            Vector3[] directions = {
                Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back
            };

            int hitCount = 0;
            foreach (var direction in directions)
            {
                RaycastHit hit;
                if (Physics.Raycast(point, direction, out hit, raycastDistance, layerMask))
                {
                    hitCount++;
                }
            }

            // If we hit in all directions, we're likely inside
            // If we hit in most directions, we're probably inside
            return hitCount >= 4;
        }

        /// <summary>
        /// Cluster points by proximity.
        /// </summary>
        private List<List<Vector3>> ClusterPoints(List<Vector3> points, float threshold)
        {
            List<List<Vector3>> clusters = new List<List<Vector3>>();
            HashSet<Vector3> used = new HashSet<Vector3>();

            foreach (var point in points)
            {
                if (used.Contains(point))
                    continue;

                List<Vector3> cluster = new List<Vector3> { point };
                used.Add(point);

                // Find nearby points
                bool foundMore = true;
                while (foundMore)
                {
                    foundMore = false;
                    foreach (var otherPoint in points)
                    {
                        if (used.Contains(otherPoint))
                            continue;

                        // Check if any point in cluster is nearby
                        foreach (var clusterPoint in cluster)
                        {
                            if (Vector3.Distance(clusterPoint, otherPoint) <= threshold)
                            {
                                cluster.Add(otherPoint);
                                used.Add(otherPoint);
                                foundMore = true;
                                break;
                            }
                        }
                    }
                }

                clusters.Add(cluster);
            }

            return clusters;
        }

        /// <summary>
        /// Group raycast hits by proximity.
        /// </summary>
        private List<List<RaycastHit>> GroupHitsByProximity(List<RaycastHit> hits, float threshold)
        {
            List<List<RaycastHit>> groups = new List<List<RaycastHit>>();
            HashSet<int> used = new HashSet<int>();

            for (int i = 0; i < hits.Count; i++)
            {
                if (used.Contains(i))
                    continue;

                List<RaycastHit> group = new List<RaycastHit> { hits[i] };
                used.Add(i);

                // Find nearby hits
                for (int j = i + 1; j < hits.Count; j++)
                {
                    if (used.Contains(j))
                        continue;

                    if (Vector3.Distance(hits[i].point, hits[j].point) <= threshold)
                    {
                        group.Add(hits[j]);
                        used.Add(j);
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        /// <summary>
        /// Extract vertex loop from a group of raycast hits.
        /// </summary>
        private VertexLoop ExtractVertexLoop(List<RaycastHit> hits, MeshRenderer[] renderers)
        {
            if (hits == null || hits.Count < 3)
                return null;

            VertexLoop loop = new VertexLoop();

            // Use hit points as vertices (simplified - in practice would extract from mesh)
            foreach (var hit in hits)
            {
                loop.vertices.Add(hit.point);
                
                // Try to get UV from hit
                if (hit.collider != null)
                {
                    MeshCollider meshCollider = hit.collider as MeshCollider;
                    if (meshCollider != null && meshCollider.sharedMesh != null)
                    {
                        // Get UV at hit point (simplified)
                        Vector2 uv = GetUVAtHitPoint(hit, meshCollider);
                        loop.uvs.Add(uv);
                    }
                    else
                    {
                        loop.uvs.Add(Vector2.zero);
                    }
                }
                else
                {
                    loop.uvs.Add(Vector2.zero);
                }
            }

            // Order vertices to form a loop (convex hull approximation)
            loop.vertices = OrderVerticesForLoop(loop.vertices);

            return loop;
        }

        /// <summary>
        /// Get UV coordinates at a raycast hit point.
        /// </summary>
        private Vector2 GetUVAtHitPoint(RaycastHit hit, MeshCollider meshCollider)
        {
            Mesh mesh = meshCollider.sharedMesh;
            if (mesh == null || !mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord0))
                return Vector2.zero;

            // Get triangle indices
            int[] triangles = mesh.triangles;
            Vector3[] vertices = mesh.vertices;
            Vector2[] uvs = mesh.uv;

            // Find closest triangle
            int closestTri = -1;
            float closestDist = float.MaxValue;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = meshCollider.transform.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = meshCollider.transform.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = meshCollider.transform.TransformPoint(vertices[triangles[i + 2]]);

                Vector3 triCenter = (v0 + v1 + v2) / 3f;
                float dist = Vector3.Distance(hit.point, triCenter);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestTri = i / 3;
                }
            }

            if (closestTri >= 0)
            {
                int idx = closestTri * 3;
                // Barycentric interpolation (simplified - use triangle center)
                Vector2 uv = (uvs[triangles[idx]] + uvs[triangles[idx + 1]] + uvs[triangles[idx + 2]]) / 3f;
                return uv;
            }

            return Vector2.zero;
        }

        /// <summary>
        /// Order vertices to form a proper loop (convex hull or simple ordering).
        /// </summary>
        private List<Vector3> OrderVerticesForLoop(List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count < 3)
                return vertices;

            // Calculate center
            Vector3 center = Vector3.zero;
            foreach (var v in vertices)
            {
                center += v;
            }
            center /= vertices.Count;

            // Sort by angle around center
            return vertices.OrderBy(v =>
            {
                Vector3 dir = (v - center).normalized;
                return Mathf.Atan2(dir.z, dir.x);
            }).ToList();
        }

        /// <summary>
        /// Check if a position is within the mesh terrain bounds.
        /// </summary>
        public bool IsPositionInBounds(Vector3 position)
        {
            Bounds? bounds = GetMeshBounds(meshTerrain);
            if (bounds.HasValue)
            {
                return bounds.Value.Contains(position);
            }
            return false;
        }
    }
}
