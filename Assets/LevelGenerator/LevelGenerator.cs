using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Main level generator component for roguelikes/city layouts using 2D AABB physics.
/// Stacks rooms at origin, runs physics until stable, applies fireworks effects, then connects with hallways.
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Room Configuration")]
    [Tooltip("Room prefabs to use for generation")]
    public List<GameObject> roomPrefabs = new List<GameObject>();

    [Tooltip("Number of rooms to generate")]
    public int roomCount = 10;

    [Tooltip("Bounds for generation (optional constraint)")]
    public Bounds generationBounds = new Bounds(Vector3.zero, new Vector3(100f, 100f, 0f));

    [Header("Physics Solver")]
    [Tooltip("Use Unity 2D Physics (Rigidbody2D/Collider2D)")]
    public bool useUnity2DPhysics = true;

    [Tooltip("Maximum physics iterations before timeout")]
    public int maxPhysicsIterations = 1000;

    [Tooltip("Stability threshold (movement/velocity below this = stable)")]
    public float stabilityThreshold = 0.01f;

    [Header("Fireworks Effects")]
    [Tooltip("Enable fireworks effects (random room restacking)")]
    public bool enableFireworks = true;

    [Tooltip("Number of fireworks iterations")]
    [Range(0, 10)]
    public int fireworksIterations = 2;

    [Tooltip("Percentage of rooms to restack per fireworks iteration (0-1)")]
    [Range(0f, 1f)]
    public float fireworksRoomPercentage = 0.3f;

    [Tooltip("Add new room stacks during fireworks")]
    public bool addNewStacks = false;

    [Header("Hallway Configuration")]
    [Tooltip("Hallway prefab")]
    public GameObject hallwayPrefab;

    [Tooltip("Width of hallways")]
    public float hallwayWidth = 2f;

    [Header("Generation")]
    [Tooltip("Auto-generate on start")]
    public bool autoGenerateOnStart = false;

    // Internal state
    private List<RoomAsset> generatedRooms = new List<RoomAsset>();
    private PhysicsSolver2D physicsSolver;
    private HallwayConnector hallwayConnector;
    private bool isGenerating = false;

    private void Awake()
    {
        // Initialize physics solver
        physicsSolver = gameObject.AddComponent<PhysicsSolver2D>();
        physicsSolver.useUnity2DPhysics = useUnity2DPhysics;
        physicsSolver.stabilityThreshold = stabilityThreshold;

        // Initialize hallway connector
        hallwayConnector = gameObject.AddComponent<HallwayConnector>();
        hallwayConnector.hallwayPrefab = hallwayPrefab;
        hallwayConnector.hallwayWidth = hallwayWidth;
    }

    private void Start()
    {
        if (autoGenerateOnStart)
        {
            Generate();
        }
    }

    /// <summary>
    /// Main generation method - runs the full level generation process.
    /// </summary>
    public void Generate()
    {
        if (isGenerating)
        {
            Debug.LogWarning("Level generation already in progress");
            return;
        }

        StartCoroutine(GenerateCoroutine());
    }

    private IEnumerator GenerateCoroutine()
    {
        isGenerating = true;

        // Step 1: Initialize and stack rooms
        InitializeRooms();

        // Step 2: Run physics solver until stable
        yield return StartCoroutine(RunPhysicsSolverCoroutine());

        // Step 3: Apply fireworks effects if enabled
        if (enableFireworks)
        {
            yield return StartCoroutine(ApplyFireworksEffectsCoroutine());
        }

        // Step 4: Connect rooms with hallways
        ConnectHallways();

        isGenerating = false;
        Debug.Log("Level generation complete!");
    }

    /// <summary>
    /// Initialize rooms from prefabs and stack them at origin.
    /// </summary>
    public void InitializeRooms()
    {
        // Clear existing rooms
        ClearRooms();

        // Create rooms from prefabs
        for (int i = 0; i < roomCount; i++)
        {
            if (roomPrefabs == null || roomPrefabs.Count == 0)
            {
                Debug.LogError("No room prefabs assigned!");
                return;
            }

            // Select random prefab
            GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
            if (prefab == null)
                continue;

            // Instantiate room
            GameObject roomObj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            roomObj.name = $"Room_{i}";

            // Get or add RoomAsset component
            RoomAsset room = roomObj.GetComponent<RoomAsset>();
            if (room == null)
            {
                room = roomObj.AddComponent<RoomAsset>();
            }

            // Stack at origin (all rooms start at same position)
            room.transform.position = Vector3.zero;
            room.transform.rotation = Quaternion.identity;

            // Setup physics if using Unity 2D
            if (useUnity2DPhysics)
            {
                Rigidbody2D rb = roomObj.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = roomObj.AddComponent<Rigidbody2D>();
                }
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f; // No gravity for 2D level generation
            }

            generatedRooms.Add(room);
        }

        // Initialize physics solver
        physicsSolver.Initialize(generatedRooms);
    }

    /// <summary>
    /// Run physics solver until stable (coroutine version).
    /// </summary>
    private IEnumerator RunPhysicsSolverCoroutine()
    {
        int iterations = 0;
        bool stable = false;

        while (!stable && iterations < maxPhysicsIterations)
        {
            // Step physics
            physicsSolver.Step();

            // Check stability
            stable = physicsSolver.IsStable();

            iterations++;

            // Yield every few iterations to prevent freezing
            if (iterations % 10 == 0)
            {
                yield return null;
            }
        }

        if (!stable)
        {
            Debug.LogWarning($"Physics solver did not reach stability after {iterations} iterations");
        }
        else
        {
            Debug.Log($"Physics solver reached stability after {iterations} iterations");
        }
    }

    /// <summary>
    /// Run physics solver until stable (non-coroutine version).
    /// </summary>
    public void RunPhysicsSolver()
    {
        int iterations = 0;
        bool stable = false;

        while (!stable && iterations < maxPhysicsIterations)
        {
            physicsSolver.Step();
            stable = physicsSolver.IsStable();
            iterations++;
        }

        if (!stable)
        {
            Debug.LogWarning($"Physics solver did not reach stability after {iterations} iterations");
        }
    }

    /// <summary>
    /// Check if physics has reached stable state.
    /// </summary>
    public bool CheckStability()
    {
        return physicsSolver.IsStable();
    }

    /// <summary>
    /// Apply fireworks effects - randomly restack rooms and re-run physics.
    /// </summary>
    private IEnumerator ApplyFireworksEffectsCoroutine()
    {
        for (int iteration = 0; iteration < fireworksIterations; iteration++)
        {
            // Select random rooms to restack
            int count = Mathf.CeilToInt(generatedRooms.Count * fireworksRoomPercentage);
            var selectedRooms = SelectRandomRoomsForFireworks(count);

            // Restack at origin
            RestackRooms(selectedRooms);

            // Re-run physics until stable
            yield return StartCoroutine(RunPhysicsSolverCoroutine());

            // Optionally add new stacks
            if (addNewStacks && iteration < fireworksIterations - 1)
            {
                AddNewRoomStacks(1);
            }
        }
    }

    /// <summary>
    /// Select random rooms for fireworks effects.
    /// </summary>
    private List<RoomAsset> SelectRandomRoomsForFireworks(int count)
    {
        List<RoomAsset> selected = new List<RoomAsset>();
        List<RoomAsset> available = new List<RoomAsset>(generatedRooms);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = Random.Range(0, available.Count);
            selected.Add(available[index]);
            available.RemoveAt(index);
        }

        return selected;
    }

    /// <summary>
    /// Restack selected rooms at origin.
    /// </summary>
    public void RestackRooms(List<RoomAsset> rooms)
    {
        foreach (var room in rooms)
        {
            if (room != null)
            {
                room.transform.position = Vector3.zero;
                room.transform.rotation = Quaternion.identity;

                if (useUnity2DPhysics)
                {
                    Rigidbody2D rb = room.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.velocity = Vector2.zero;
                        rb.angularVelocity = 0f;
                    }
                }
            }
        }

        // Re-initialize physics solver with updated positions
        physicsSolver.Initialize(generatedRooms);
    }

    /// <summary>
    /// Add new room stacks after initial generation.
    /// </summary>
    public void AddNewRoomStacks(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (roomPrefabs == null || roomPrefabs.Count == 0)
                return;

            GameObject prefab = roomPrefabs[Random.Range(0, roomPrefabs.Count)];
            if (prefab == null)
                continue;

            GameObject roomObj = Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
            roomObj.name = $"Room_Fireworks_{generatedRooms.Count}";

            RoomAsset room = roomObj.GetComponent<RoomAsset>();
            if (room == null)
            {
                room = roomObj.AddComponent<RoomAsset>();
            }

            room.transform.position = Vector3.zero;

            if (useUnity2DPhysics)
            {
                Rigidbody2D rb = roomObj.GetComponent<Rigidbody2D>();
                if (rb == null)
                {
                    rb = roomObj.AddComponent<Rigidbody2D>();
                }
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.gravityScale = 0f;
            }

            generatedRooms.Add(room);
        }

        // Re-initialize physics solver
        physicsSolver.Initialize(generatedRooms);
    }

    /// <summary>
    /// Connect all rooms with hallways using Delaunay triangulation.
    /// </summary>
    public void ConnectHallways()
    {
        if (hallwayConnector != null)
        {
            hallwayConnector.ConnectRooms(generatedRooms);
        }
    }

    /// <summary>
    /// Clear all generated rooms.
    /// </summary>
    public void ClearRooms()
    {
        foreach (var room in generatedRooms)
        {
            if (room != null)
            {
                DestroyImmediate(room.gameObject);
            }
        }
        generatedRooms.Clear();
    }

    /// <summary>
    /// Get all generated rooms.
    /// </summary>
    public List<RoomAsset> GetRooms()
    {
        return new List<RoomAsset>(generatedRooms);
    }
}
