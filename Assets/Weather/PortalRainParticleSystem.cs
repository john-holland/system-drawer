using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Represents a segment of the accordion particle system along a portal edge.
    /// </summary>
    [System.Serializable]
    public class AccordionSegment
    {
        [Tooltip("Vertices in this segment")]
        public List<Vector3> vertices = new List<Vector3>();

        [Tooltip("UV coordinates for vertices")]
        public List<Vector2> uvs = new List<Vector2>();

        [Tooltip("Average angle from up for this segment")]
        public float angleFromUp = 0f;

        [Tooltip("Particle system for this segment")]
        public ParticleSystem particleSystem;

        [Tooltip("Center position of segment")]
        public Vector3 center = Vector3.zero;

        [Tooltip("Normal direction of segment")]
        public Vector3 normal = Vector3.up;

        [Tooltip("Segment mode (Sheet or Dribble)")]
        public ParticleMode mode = ParticleMode.Sheet;
    }

    /// <summary>
    /// Particle emission mode based on surface angle.
    /// </summary>
    public enum ParticleMode
    {
        Sheet,      // Continuous sheets (>270 degrees from up)
        Dribble     // Dribbling drops (180-270 degrees from up)
    }

    /// <summary>
    /// Component that creates and manages particle effects for portal openings based on surface angles.
    /// Creates accordion-like particle systems that transition from sheets to dribbles based on angle.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class PortalRainParticleSystem : MonoBehaviour
    {
        [Header("Portal Reference")]
        [Tooltip("Reference to the portal this particle system is attached to")]
        public MeshTerrainPortal portal;

        [Header("Particle System Settings")]
        [Tooltip("Particle system component (auto-found if null)")]
        public new ParticleSystem particleSystem;

        [Tooltip("Number of segments in accordion-like system")]
        [Range(2, 20)]
        public int accordionSegments = 5;

        [Tooltip("Size of each accordion segment")]
        public float segmentSize = 1f;

        [Tooltip("Particle emission rate multiplier")]
        [Range(0.1f, 10f)]
        public float emissionMultiplier = 1f;

        [Header("Drip Line Detection")]
        [Tooltip("Automatically detect drip line from portal")]
        public bool autoDetectDripLine = true;

        [Tooltip("Angle threshold for drip line detection (degrees from up)")]
        [Range(0f, 360f)]
        public float dripLineAngleThreshold = 180f;

        [Tooltip("Vertices along the detected drip line")]
        public List<Vector3> dripLineVertices = new List<Vector3>();

        [Header("Particle Bounds")]
        [Tooltip("Bounds for particle system emission")]
        public Bounds particleBounds;

        [Tooltip("Auto-calculate bounds from portal")]
        public bool autoCalculateBounds = true;

        [Header("Mode Settings")]
        [Tooltip("True if in sheet mode (>270 degrees from up)")]
        public bool sheetMode = false;

        [Tooltip("True if in dribble mode (180-270 degrees)")]
        public bool dribbleMode = false;

        [Header("Sheet Mode Properties")]
        [Tooltip("Emission rate for sheet mode (particles/second)")]
        public float sheetEmissionRate = 2000f;

        [Tooltip("Particle velocity for sheet mode")]
        public float sheetVelocity = 15f;

        [Tooltip("Horizontal spread for sheet mode")]
        [Range(0f, 1f)]
        public float sheetSpread = 0.1f;

        [Header("Dribble Mode Properties")]
        [Tooltip("Emission rate for dribble mode (particles/second)")]
        public float dribbleEmissionRate = 500f;

        [Tooltip("Particle velocity for dribble mode")]
        public float dribbleVelocity = 5f;

        [Tooltip("Horizontal spread for dribble mode")]
        [Range(0f, 1f)]
        public float dribbleSpread = 0.5f;

        [Header("Terrain Detection")]
        [Tooltip("Terrain for height map cutoff detection (optional)")]
        public Terrain terrain;

        [Tooltip("Mesh terrain for mesh cutoff detection (optional)")]
        public Transform meshTerrain;

        [Tooltip("Cutoff detection threshold (height drop in meters)")]
        public float cutoffThreshold = 0.5f;

        // Internal state
        private List<AccordionSegment> segments = new List<AccordionSegment>();
        private List<Vector3> cutoffPoints = new List<Vector3>();

        private void Awake()
        {
            // Auto-find particle system
            if (particleSystem == null)
            {
                particleSystem = GetComponent<ParticleSystem>();
            }

            // Auto-find portal if not assigned
            if (portal == null)
            {
                portal = GetComponent<MeshTerrainPortal>();
            }

            // Auto-find terrain
            if (terrain == null)
            {
                terrain = FindFirstObjectByType<Terrain>();
            }
        }

        private void Start()
        {
            if (autoDetectDripLine && portal != null)
            {
                DetectDripLine();
            }

            if (autoCalculateBounds)
            {
                CalculateParticleBounds();
            }

            CreateAccordionParticleSystem();
        }

        /// <summary>
        /// Detect drip line from portal vertices.
        /// </summary>
        public void DetectDripLine()
        {
            dripLineVertices.Clear();
            cutoffPoints.Clear();

            if (portal == null || portal.vertexLoop == null || portal.vertexLoop.vertices == null)
                return;

            VertexLoop loop = portal.vertexLoop;
            List<Vector3> vertices = loop.vertices;
            List<Vector2> uvs = loop.uvs;

            // Detect cutoffs first (immediate drops)
            if (terrain != null)
            {
                cutoffPoints.AddRange(DetectTerrainCutoffs(terrain, portal.transform.position));
            }
            else if (meshTerrain != null)
            {
                cutoffPoints.AddRange(DetectMeshCutoffs(meshTerrain, loop));
            }

            // Analyze each vertex for drip line
            for (int i = 0; i < vertices.Count; i++)
            {
                Vector3 vertex = vertices[i];
                Vector3 normal = loop.normal; // Use average normal, or calculate per-vertex if available

                // Calculate angle from up
                float angleFromUp = CalculateSurfaceAngle(vertex, normal);

                // Check if vertex is on drip line (180-270 degrees from up)
                if (IsDripLineVertex(vertex, normal, dripLineAngleThreshold))
                {
                    dripLineVertices.Add(vertex);
                }

                // Check for cutoffs (immediate height drops)
                if (IsCutoffPoint(vertex))
                {
                    cutoffPoints.Add(vertex);
                }
            }

            Debug.Log($"PortalRainParticleSystem: Detected {dripLineVertices.Count} drip line vertices and {cutoffPoints.Count} cutoff points");
        }

        /// <summary>
        /// Calculate surface angle from up vector, using UV coordinates if available.
        /// </summary>
        public float CalculateSurfaceAngle(Vector3 vertex, Vector3 normal)
        {
            // Calculate angle between normal and up vector
            float angle = Vector3.Angle(normal, Vector3.up);

            // Determine if overhanging (normal pointing down)
            if (Vector3.Dot(normal, Vector3.up) < 0)
            {
                // Overhanging: angle is 180 + (180 - angle) = 360 - angle
                angle = 360f - angle;
            }

            // If portal has UVs, check for height map cutoffs encoded in UV
            if (portal != null && portal.vertexLoop != null && portal.vertexLoop.uvs != null)
            {
                int vertexIndex = portal.vertexLoop.vertices.IndexOf(vertex);
                if (vertexIndex >= 0 && vertexIndex < portal.vertexLoop.uvs.Count)
                {
                    Vector2 uv = portal.vertexLoop.uvs[vertexIndex];
                    // UV.y can encode angle information (180 = sudden drop, 0 = normal)
                    // If UV.y is near 180 (or encoded as 0.5 in normalized UV), it's a cutoff
                    if (uv.y > 0.4f && uv.y < 0.6f) // Normalized: 0.5 = 180 degrees
                    {
                        angle = 180f; // Mark as cutoff/drip line
                    }
                }
            }

            return angle;
        }

        /// <summary>
        /// Calculate surface angle using UV coordinates from vertex loop.
        /// </summary>
        public float CalculateSurfaceAngleFromUV(Vector3 vertex, Vector2 uv)
        {
            // UV.y can encode angle: 0 = up (0°), 0.5 = horizontal (90°), 1.0 = down (180°)
            // For height map cutoffs, UV.y = 0.5 (180 degrees) indicates sudden drop
            float normalizedAngle = uv.y * 360f;
            
            // Clamp to valid range
            return Mathf.Clamp(normalizedAngle, 0f, 360f);
        }

        /// <summary>
        /// Check if vertex is on drip line.
        /// </summary>
        private bool IsDripLineVertex(Vector3 vertex, Vector3 normal, float threshold)
        {
            float angle = CalculateSurfaceAngle(vertex, normal);
            // Drip line is between 180 and 270 degrees from up
            return angle >= 180f && angle <= 270f;
        }

        /// <summary>
        /// Check if point is a cutoff (immediate drop).
        /// </summary>
        private bool IsCutoffPoint(Vector3 point)
        {
            // Check if point is near any detected cutoff
            foreach (var cutoff in cutoffPoints)
            {
                if (Vector3.Distance(point, cutoff) < cutoffThreshold)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Detect terrain cutoffs for height map terrains.
        /// Sets UV coordinates to encode cutoff information (180 degrees = sudden drop).
        /// </summary>
        private List<Vector3> DetectTerrainCutoffs(Terrain terrain, Vector3 portalCenter)
        {
            List<Vector3> cutoffs = new List<Vector3>();

            if (terrain == null || terrain.terrainData == null)
                return cutoffs;

            TerrainData terrainData = terrain.terrainData;
            Vector3 terrainPos = terrain.transform.position;

            // Sample height map around portal
            int sampleRadius = 10; // Sample points in radius
            float sampleSpacing = 1f;

            for (int x = -sampleRadius; x <= sampleRadius; x++)
            {
                for (int z = -sampleRadius; z <= sampleRadius; z++)
                {
                    Vector3 samplePos = portalCenter + new Vector3(x * sampleSpacing, 0f, z * sampleSpacing);

                    // Check if within terrain bounds
                    if (samplePos.x < terrainPos.x || samplePos.x > terrainPos.x + terrainData.size.x ||
                        samplePos.z < terrainPos.z || samplePos.z > terrainPos.z + terrainData.size.z)
                        continue;

                    float height = terrain.SampleHeight(samplePos);
                    Vector3 worldPos = new Vector3(samplePos.x, height, samplePos.z);

                    // Sample nearby points to detect immediate drops
                    float currentHeight = height;
                    float nearbyHeight = terrain.SampleHeight(samplePos + Vector3.right * sampleSpacing);

                    // If height drops significantly, it's a cutoff
                    if (currentHeight - nearbyHeight > cutoffThreshold)
                    {
                        cutoffs.Add(worldPos);

                        // Update UV for this vertex if it's in the portal loop
                        if (portal != null && portal.vertexLoop != null)
                        {
                            UpdateUVForCutoff(worldPos, 180f); // 180 degrees = sudden drop
                        }
                    }
                }
            }

            return cutoffs;
        }

        /// <summary>
        /// Update UV coordinate for a cutoff point to encode angle (180 degrees).
        /// </summary>
        private void UpdateUVForCutoff(Vector3 cutoffPoint, float angleDegrees)
        {
            if (portal == null || portal.vertexLoop == null || portal.vertexLoop.vertices == null)
                return;

            // Find closest vertex in portal loop
            int closestIndex = -1;
            float closestDist = float.MaxValue;

            for (int i = 0; i < portal.vertexLoop.vertices.Count; i++)
            {
                float dist = Vector3.Distance(portal.vertexLoop.vertices[i], cutoffPoint);
                if (dist < closestDist && dist < 0.5f) // Within 0.5m
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }

            // Update UV to encode angle (normalized: 180° = 0.5)
            if (closestIndex >= 0)
            {
                if (portal.vertexLoop.uvs == null)
                {
                    portal.vertexLoop.uvs = new List<Vector2>();
                }

                // Ensure UVs list is same size as vertices
                while (portal.vertexLoop.uvs.Count < portal.vertexLoop.vertices.Count)
                {
                    portal.vertexLoop.uvs.Add(Vector2.zero);
                }

                // Set UV.y to encode angle (normalized: 0-1 maps to 0-360 degrees)
                Vector2 uv = portal.vertexLoop.uvs[closestIndex];
                uv.y = angleDegrees / 360f; // Normalize angle to 0-1
                portal.vertexLoop.uvs[closestIndex] = uv;
            }
        }

        /// <summary>
        /// Detect mesh cutoffs for mesh terrains.
        /// </summary>
        private List<Vector3> DetectMeshCutoffs(Transform meshTerrain, VertexLoop portal)
        {
            List<Vector3> cutoffs = new List<Vector3>();

            if (meshTerrain == null || portal == null || portal.vertices == null)
                return cutoffs;

            // Analyze portal vertices for sharp angle changes
            for (int i = 0; i < portal.vertices.Count; i++)
            {
                int prev = (i - 1 + portal.vertices.Count) % portal.vertices.Count;
                int next = (i + 1) % portal.vertices.Count;

                Vector3 current = portal.vertices[i];
                Vector3 prevVertex = portal.vertices[prev];
                Vector3 nextVertex = portal.vertices[next];

                // Calculate height differences
                float heightDiff1 = current.y - prevVertex.y;
                float heightDiff2 = current.y - nextVertex.y;

                // If immediate drop detected
                if (heightDiff1 < -cutoffThreshold || heightDiff2 < -cutoffThreshold)
                {
                    cutoffs.Add(current);
                }
            }

            return cutoffs;
        }

        /// <summary>
        /// Create accordion-like particle system along portal edge.
        /// </summary>
        public void CreateAccordionParticleSystem()
        {
            if (portal == null || portal.vertexLoop == null || portal.vertexLoop.vertices == null)
                return;

            // Clear existing segments
            foreach (var segment in segments)
            {
                if (segment.particleSystem != null)
                {
                    DestroyImmediate(segment.particleSystem.gameObject);
                }
            }
            segments.Clear();

            // Segment vertices by angle
            List<List<Vector3>> vertexGroups = SegmentVerticesByAngle(
                portal.vertexLoop.vertices,
                portal.vertexLoop.normal
            );

            // Create particle system segment for each group
            for (int i = 0; i < vertexGroups.Count && i < accordionSegments; i++)
            {
                AccordionSegment segment = new AccordionSegment();
                segment.vertices = vertexGroups[i];

                // Calculate segment properties
                segment.center = CalculateSegmentCenter(segment.vertices);
                segment.normal = portal.vertexLoop.normal;
                segment.angleFromUp = CalculateSurfaceAngle(segment.center, segment.normal);

                // Determine mode based on angle
                if (segment.angleFromUp > 270f)
                {
                    segment.mode = ParticleMode.Sheet;
                }
                else if (segment.angleFromUp >= 180f && segment.angleFromUp <= 270f)
                {
                    segment.mode = ParticleMode.Dribble;
                }
                else
                {
                    segment.mode = ParticleMode.Sheet; // Default to sheet
                }

                // Create particle system for segment
                GameObject segmentObj = new GameObject($"AccordionSegment_{i}");
                segmentObj.transform.SetParent(transform);
                segmentObj.transform.position = segment.center;
                segmentObj.transform.rotation = Quaternion.LookRotation(segment.normal);

                ParticleSystem segPS = segmentObj.AddComponent<ParticleSystem>();
                ConfigureParticleSystemForSegment(segPS, segment);

                segment.particleSystem = segPS;
                segments.Add(segment);
            }

            Debug.Log($"PortalRainParticleSystem: Created {segments.Count} accordion segments");
        }

        /// <summary>
        /// Segment vertices by angle ranges.
        /// </summary>
        private List<List<Vector3>> SegmentVerticesByAngle(List<Vector3> vertices, Vector3 averageNormal)
        {
            List<List<Vector3>> groups = new List<List<Vector3>>();

            if (vertices == null || vertices.Count == 0)
                return groups;

            // Group consecutive vertices by angle
            List<Vector3> currentGroup = new List<Vector3> { vertices[0] };

            for (int i = 1; i < vertices.Count; i++)
            {
                Vector3 prev = vertices[i - 1];
                Vector3 current = vertices[i];

                float prevAngle = CalculateSurfaceAngle(prev, averageNormal);
                float currentAngle = CalculateSurfaceAngle(current, averageNormal);

                // If angle changes significantly, start new group
                if (Mathf.Abs(currentAngle - prevAngle) > 30f) // 30 degree threshold
                {
                    if (currentGroup.Count > 0)
                    {
                        groups.Add(new List<Vector3>(currentGroup));
                    }
                    currentGroup.Clear();
                }

                currentGroup.Add(current);
            }

            // Add last group
            if (currentGroup.Count > 0)
            {
                groups.Add(currentGroup);
            }

            // If no groups created, create one with all vertices
            if (groups.Count == 0)
            {
                groups.Add(new List<Vector3>(vertices));
            }

            return groups;
        }

        /// <summary>
        /// Calculate center of a vertex group.
        /// </summary>
        private Vector3 CalculateSegmentCenter(List<Vector3> vertices)
        {
            if (vertices == null || vertices.Count == 0)
                return Vector3.zero;

            Vector3 center = Vector3.zero;
            foreach (var vertex in vertices)
            {
                center += vertex;
            }
            return center / vertices.Count;
        }

        /// <summary>
        /// Configure particle system for a segment based on its mode.
        /// </summary>
        private void ConfigureParticleSystemForSegment(ParticleSystem ps, AccordionSegment segment)
        {
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var velocityOverLifetime = ps.velocityOverLifetime;

            // Set mode-based properties
            if (segment.mode == ParticleMode.Sheet)
            {
                // Sheet mode: high rate, fast velocity, narrow spread
                emission.rateOverTime = sheetEmissionRate * emissionMultiplier;
                main.startSpeed = sheetVelocity;
                main.startLifetime = 2f;
                main.startSize = 0.1f;

                // Narrow horizontal spread
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = sheetSpread * 10f; // Convert to degrees
                shape.radius = segmentSize * 0.5f;
            }
            else // Dribble mode
            {
                // Dribble mode: lower rate, slower velocity, wider spread
                emission.rateOverTime = dribbleEmissionRate * emissionMultiplier;
                main.startSpeed = dribbleVelocity;
                main.startLifetime = 3f;
                main.startSize = 0.15f;

                // Wider horizontal spread
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = dribbleSpread * 45f; // Convert to degrees
                shape.radius = segmentSize * 0.5f;
            }

            // Common settings
            main.startColor = new Color(0.8f, 0.8f, 1f, 0.8f);
            main.gravityModifier = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            // Orient shape along normal
            shape.rotation = Quaternion.LookRotation(segment.normal).eulerAngles;
        }

        /// <summary>
        /// Update particle system based on current state.
        /// </summary>
        public void UpdateParticleSystem()
        {
            if (segments == null || segments.Count == 0)
                return;

            foreach (var segment in segments)
            {
                if (segment.particleSystem == null)
                    continue;

                // Update based on mode
                var emission = segment.particleSystem.emission;
                if (segment.mode == ParticleMode.Sheet)
                {
                    emission.rateOverTime = sheetEmissionRate * emissionMultiplier;
                }
                else
                {
                    emission.rateOverTime = dribbleEmissionRate * emissionMultiplier;
                }
            }
        }

        /// <summary>
        /// Calculate particle bounds from portal.
        /// </summary>
        public void CalculateParticleBounds()
        {
            if (portal == null || portal.vertexLoop == null || portal.vertexLoop.vertices == null)
                return;

            Bounds bounds = new Bounds(portal.vertexLoop.vertices[0], Vector3.zero);
            foreach (var vertex in portal.vertexLoop.vertices)
            {
                bounds.Encapsulate(vertex);
            }

            // Expand bounds slightly
            bounds.Expand(segmentSize);
            particleBounds = bounds;
        }

        /// <summary>
        /// Segment along mesh line automatically.
        /// </summary>
        public void SegmentAlongMeshLine()
        {
            if (portal == null || portal.vertexLoop == null)
                return;

            // Re-segment based on current vertex loop
            CreateAccordionParticleSystem();
        }

        private void OnDrawGizmosSelected()
        {
            if (portal == null || portal.vertexLoop == null)
                return;

            // Draw drip line vertices
            Gizmos.color = Color.red;
            foreach (var vertex in dripLineVertices)
            {
                Gizmos.DrawSphere(vertex, 0.1f);
            }

            // Draw cutoff points
            Gizmos.color = Color.yellow;
            foreach (var cutoff in cutoffPoints)
            {
                Gizmos.DrawWireSphere(cutoff, 0.2f);
            }

            // Draw particle bounds
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(particleBounds.center, particleBounds.size);

            // Draw accordion segments
            foreach (var segment in segments)
            {
                if (segment.particleSystem != null)
                {
                    Gizmos.color = segment.mode == ParticleMode.Sheet ? Color.blue : Color.green;
                    Gizmos.DrawWireSphere(segment.center, 0.15f);
                }
            }
        }
    }
}
