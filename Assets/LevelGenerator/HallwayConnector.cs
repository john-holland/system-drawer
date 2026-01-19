using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Hallway connection system for connecting rooms using Delaunay triangulation.
/// Ensures all rooms are visitable by creating hallways between them.
/// </summary>
public class HallwayConnector : MonoBehaviour
{
    [Header("Hallway Configuration")]
    [Tooltip("Hallway prefab to instantiate")]
    public GameObject hallwayPrefab;

    [Tooltip("Width of hallways")]
    public float hallwayWidth = 2f;

    [Tooltip("Connection method")]
    public ConnectionMethod connectionMethod = ConnectionMethod.Delaunay;

    [Header("Pathfinding")]
    [Tooltip("Use pathfinding to avoid obstacles")]
    public bool usePathfinding = false;

    // Internal state
    private List<GameObject> generatedHallways = new List<GameObject>();
    private DelaunayTriangulation delaunay = new DelaunayTriangulation();

    /// <summary>
    /// Connect all rooms with hallways.
    /// </summary>
    public void ConnectRooms(List<RoomAsset> rooms)
    {
        if (rooms == null || rooms.Count < 2)
        {
            Debug.LogWarning("Not enough rooms to connect");
            return;
        }

        // Clear existing hallways
        ClearHallways();

        // Extract room centers as points
        List<Vector2> points = new List<Vector2>();
        foreach (var room in rooms)
        {
            if (room != null)
            {
                points.Add(room.GetCenter());
            }
        }

        if (points.Count < 2)
            return;

        // Perform Delaunay triangulation
        List<DelaunayTriangulation.Triangle> triangles = delaunay.Triangulate(points);

        // Get edges from triangles
        HashSet<DelaunayTriangulation.Edge> edges = delaunay.GetEdges(triangles);

        // Create hallways for each edge
        int pointIndex = 0;
        foreach (var room in rooms)
        {
            if (room == null)
                continue;

            foreach (var edge in edges)
            {
                if (edge.v0 == pointIndex || edge.v1 == pointIndex)
                {
                    int otherIndex = edge.v0 == pointIndex ? edge.v1 : edge.v0;
                    if (otherIndex < rooms.Count && rooms[otherIndex] != null)
                    {
                        RoomAsset room1 = room;
                        RoomAsset room2 = rooms[otherIndex];

                        // Avoid duplicate connections
                        if (!HallwayExists(room1, room2))
                        {
                            GenerateHallway(room1, room2);
                        }
                    }
                }
            }

            pointIndex++;
        }

        // Verify connectivity
        if (!IsFullyConnected(rooms))
        {
            Debug.LogWarning("Rooms are not fully connected, adding minimum spanning tree edges");
            ConnectDisconnectedComponents(rooms);
        }
    }

    /// <summary>
    /// Generate a hallway between two rooms.
    /// </summary>
    public void GenerateHallway(RoomAsset room1, RoomAsset room2)
    {
        if (room1 == null || room2 == null)
            return;

        Vector2 center1 = room1.GetCenter();
        Vector2 center2 = room2.GetCenter();

        // Calculate hallway position and size
        Vector2 hallwayCenter = (center1 + center2) * 0.5f;
        float distance = Vector2.Distance(center1, center2);
        Vector2 direction = (center2 - center1).normalized;

        // Create hallway
        if (hallwayPrefab != null)
        {
            GameObject hallway = Instantiate(hallwayPrefab, new Vector3(hallwayCenter.x, hallwayCenter.y, 0f), Quaternion.identity, transform);
            hallway.name = $"Hallway_{room1.name}_to_{room2.name}";

            // Scale hallway to fit distance
            hallway.transform.localScale = new Vector3(distance, hallwayWidth, 1f);
            hallway.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);

            generatedHallways.Add(hallway);
        }
        else
        {
            // Create simple hallway using a quad
            GameObject hallway = GameObject.CreatePrimitive(PrimitiveType.Quad);
            hallway.name = $"Hallway_{room1.name}_to_{room2.name}";
            hallway.transform.position = new Vector3(hallwayCenter.x, hallwayCenter.y, 0f);
            hallway.transform.localScale = new Vector3(distance, hallwayWidth, 1f);
            hallway.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            hallway.transform.SetParent(transform);

            // Remove collider if not needed
            Collider2D collider = hallway.GetComponent<Collider2D>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }

            generatedHallways.Add(hallway);
        }
    }

    /// <summary>
    /// Find path between two rooms (for pathfinding).
    /// </summary>
    public List<Vector2> FindPath(RoomAsset from, RoomAsset to)
    {
        // Simplified: direct path
        // In a full implementation, this would use A* pathfinding
        if (from == null || to == null)
            return new List<Vector2>();

        return new List<Vector2> { from.GetCenter(), to.GetCenter() };
    }

    /// <summary>
    /// Place hallway at specified position.
    /// </summary>
    public void PlaceHallway(Vector2 position, Vector2 direction, float length)
    {
        if (hallwayPrefab != null)
        {
            GameObject hallway = Instantiate(hallwayPrefab, new Vector3(position.x, position.y, 0f), Quaternion.identity, transform);
            hallway.transform.localScale = new Vector3(length, hallwayWidth, 1f);
            hallway.transform.rotation = Quaternion.LookRotation(Vector3.forward, direction);
            generatedHallways.Add(hallway);
        }
    }

    /// <summary>
    /// Check if hallway already exists between two rooms.
    /// </summary>
    private bool HallwayExists(RoomAsset room1, RoomAsset room2)
    {
        foreach (var hallway in generatedHallways)
        {
            if (hallway != null && hallway.name.Contains(room1.name) && hallway.name.Contains(room2.name))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Check if all rooms are fully connected (graph connectivity).
    /// </summary>
    private bool IsFullyConnected(List<RoomAsset> rooms)
    {
        if (rooms == null || rooms.Count < 2)
            return true;

        // Build adjacency list from hallways
        Dictionary<RoomAsset, HashSet<RoomAsset>> adjacency = new Dictionary<RoomAsset, HashSet<RoomAsset>>();
        
        foreach (var room in rooms)
        {
            if (room != null)
            {
                adjacency[room] = new HashSet<RoomAsset>();
            }
        }

        // Populate adjacency from hallways
        foreach (var hallway in generatedHallways)
        {
            if (hallway == null)
                continue;

            // Extract room names from hallway name
            string[] parts = hallway.name.Split('_');
            if (parts.Length >= 3)
            {
                string room1Name = parts[1];
                string room2Name = parts[3];

                RoomAsset room1 = rooms.FirstOrDefault(r => r != null && r.name == room1Name);
                RoomAsset room2 = rooms.FirstOrDefault(r => r != null && r.name == room2Name);

                if (room1 != null && room2 != null)
                {
                    adjacency[room1].Add(room2);
                    adjacency[room2].Add(room1);
                }
            }
        }

        // BFS to check connectivity
        if (rooms.Count == 0)
            return true;

        RoomAsset start = rooms.FirstOrDefault(r => r != null);
        if (start == null)
            return true;

        HashSet<RoomAsset> visited = new HashSet<RoomAsset>();
        Queue<RoomAsset> queue = new Queue<RoomAsset>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            RoomAsset current = queue.Dequeue();
            
            if (adjacency.TryGetValue(current, out HashSet<RoomAsset> neighbors))
            {
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        // Check if all rooms were visited
        return visited.Count == rooms.Count(r => r != null);
    }

    /// <summary>
    /// Connect disconnected components using minimum spanning tree.
    /// </summary>
    private void ConnectDisconnectedComponents(List<RoomAsset> rooms)
    {
        // Find all room pairs and their distances
        List<(RoomAsset, RoomAsset, float)> pairs = new List<(RoomAsset, RoomAsset, float)>();

        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i] == null)
                continue;

            for (int j = i + 1; j < rooms.Count; j++)
            {
                if (rooms[j] == null)
                    continue;

                float distance = rooms[i].GetDistanceTo(rooms[j]);
                pairs.Add((rooms[i], rooms[j], distance));
            }
        }

        // Sort by distance (for MST)
        pairs = pairs.OrderBy(p => p.Item3).ToList();

        // Kruskal's algorithm for MST
        Dictionary<RoomAsset, RoomAsset> parent = new Dictionary<RoomAsset, RoomAsset>();
        foreach (var room in rooms)
        {
            if (room != null)
            {
                parent[room] = room;
            }
        }

        RoomAsset Find(RoomAsset room)
        {
            if (parent[room] != room)
            {
                parent[room] = Find(parent[room]);
            }
            return parent[room];
        }

        void Union(RoomAsset a, RoomAsset b)
        {
            RoomAsset rootA = Find(a);
            RoomAsset rootB = Find(b);
            if (rootA != rootB)
            {
                parent[rootA] = rootB;
            }
        }

        // Add edges until all rooms are connected
        foreach (var (room1, room2, distance) in pairs)
        {
            if (Find(room1) != Find(room2))
            {
                Union(room1, room2);
                if (!HallwayExists(room1, room2))
                {
                    GenerateHallway(room1, room2);
                }
            }
        }
    }

    /// <summary>
    /// Clear all generated hallways.
    /// </summary>
    public void ClearHallways()
    {
        foreach (var hallway in generatedHallways)
        {
            if (hallway != null)
            {
                DestroyImmediate(hallway);
            }
        }
        generatedHallways.Clear();
    }
}

/// <summary>
/// Connection method for hallways.
/// </summary>
public enum ConnectionMethod
{
    Delaunay,      // Delaunay triangulation
    MST,           // Minimum spanning tree
    Grid           // Grid-based
}
