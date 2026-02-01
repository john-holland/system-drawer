#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections.Generic;

/// <summary>
/// Editor tests that assert correct placement using the new system:
/// - Root placements do not overlap (distinct slots along X when multiple rooms).
/// - Flush placement: walls (or flush children) touch parent face within tolerance.
/// </summary>
public class SpatialGeneratorPlacementTests
{
    private GameObject testSceneRoot;
    private SpatialGenerator spatialGenerator;
    private Transform sceneTreeParent;
    private const float OverlapVolumeEpsilon = 0.001f;  // allow only touching (zero volume)
    private const float FlushDistanceEpsilon = 0.05f;    // max distance between flush faces

    [SetUp]
    public void Setup()
    {
        testSceneRoot = new GameObject("PlacementTestRoot");
        GameObject generatorObj = new GameObject("SpatialGenerator");
        generatorObj.transform.SetParent(testSceneRoot.transform);
        spatialGenerator = generatorObj.AddComponent<SpatialGenerator>();
        spatialGenerator.mode = SpatialGenerator.GenerationMode.ThreeDimensional;
        spatialGenerator.seed = 42;
        spatialGenerator.autoGenerateOnStart = false;
        spatialGenerator.placementStrategy = SpatialGenerator.PlacementStrategy.UniformQueue;
        // Large enough so multiple root rooms get distinct X positions (e.g. 5 rooms × 10 width = 50, use 100)
        spatialGenerator.generationSize = new Vector3(100f, 20f, 100f);
        sceneTreeParent = new GameObject("SceneTreeParent").transform;
        sceneTreeParent.SetParent(testSceneRoot.transform);
        spatialGenerator.sceneTreeParent = sceneTreeParent;
    }

    [TearDown]
    public void TearDown()
    {
        if (testSceneRoot != null)
            Object.DestroyImmediate(testSceneRoot);
    }

    /// <summary>Get world-space bounds for a GameObject (Renderer, then Collider, then transform+scale).</summary>
    public static Bounds GetWorldBounds(GameObject go)
    {
        if (go == null) return new Bounds(Vector3.zero, Vector3.zero);
        Renderer r = go.GetComponent<Renderer>();
        if (r != null && r.enabled) return r.bounds;
        Collider c = go.GetComponent<Collider>();
        if (c != null && c.enabled) return c.bounds;
        Vector3 pos = go.transform.position;
        Vector3 scale = go.transform.lossyScale;
        return new Bounds(pos, new Vector3(
            Mathf.Max(scale.x, 0.01f),
            Mathf.Max(scale.y, 0.01f),
            Mathf.Max(scale.z, 0.01f)));
    }

    /// <summary>Volume of the intersection of two bounds (0 if disjoint).</summary>
    public static float IntersectionVolume(Bounds a, Bounds b)
    {
        float minX = Mathf.Max(a.min.x, b.min.x);
        float maxX = Mathf.Min(a.max.x, b.max.x);
        float minY = Mathf.Max(a.min.y, b.min.y);
        float maxY = Mathf.Min(a.max.y, b.max.y);
        float minZ = Mathf.Max(a.min.z, b.min.z);
        float maxZ = Mathf.Min(a.max.z, b.max.z);
        if (minX >= maxX || minY >= maxY || minZ >= maxZ) return 0f;
        return (maxX - minX) * (maxY - minY) * (maxZ - minZ);
    }

    /// <summary>Collect all root-level placed objects (direct children of sceneTreeParent).</summary>
    private List<GameObject> GetRootPlacedObjects()
    {
        var list = new List<GameObject>();
        if (spatialGenerator?.sceneTreeParent == null) return list;
        for (int i = 0; i < spatialGenerator.sceneTreeParent.childCount; i++)
        {
            Transform t = spatialGenerator.sceneTreeParent.GetChild(i);
            list.Add(t.gameObject);
        }
        return list;
    }

    /// <summary>Collect all placed objects recursively (for overlap check across hierarchy). Only objects with Renderer or Collider are included so container nodes (e.g. SceneTreeParent) are excluded.</summary>
    private static void CollectAllWithBounds(Transform root, List<(GameObject go, Bounds bounds)> outList)
    {
        GameObject go = root.gameObject;
        if (go.GetComponent<Renderer>() != null || go.GetComponent<Collider>() != null)
        {
            Bounds b = GetWorldBounds(go);
            if (b.size.sqrMagnitude > 0.0001f)
                outList.Add((go, b));
        }
        for (int i = 0; i < root.childCount; i++)
            CollectAllWithBounds(root.GetChild(i), outList);
    }

    [Test]
    public void RootPlacements_DoNotOverlap()
    {
        BuildBehaviorTree_RoomOnly(roomCount: 5);
        spatialGenerator.Generate();

        List<GameObject> roots = GetRootPlacedObjects();
        Assert.GreaterOrEqual(roots.Count, 2, "Should place at least 2 root rooms");

        for (int i = 0; i < roots.Count; i++)
        {
            Bounds bi = GetWorldBounds(roots[i]);
            for (int j = i + 1; j < roots.Count; j++)
            {
                Bounds bj = GetWorldBounds(roots[j]);
                float vol = IntersectionVolume(bi, bj);
                Assert.Less(vol, OverlapVolumeEpsilon,
                    $"Root placement {i} and {j} should not overlap. " +
                    $"Room{i} center={bi.center} size={bi.size}, Room{j} center={bj.center} size={bj.size}, intersection volume={vol}");
            }
        }
    }

    [Test]
    public void RootPlacements_AreSpreadAlongX()
    {
        BuildBehaviorTree_RoomOnly(roomCount: 5);
        spatialGenerator.Generate();

        List<GameObject> roots = GetRootPlacedObjects();
        Assert.GreaterOrEqual(roots.Count, 2, "Should place at least 2 root rooms");

        var centers = new List<float>();
        foreach (GameObject go in roots)
            centers.Add(GetWorldBounds(go).center.x);
        centers.Sort();

        for (int i = 1; i < centers.Count; i++)
        {
            float gap = centers[i] - centers[i - 1];
            Assert.Greater(gap, 1f, $"Root rooms should be spread along X: adjacent centers {centers[i - 1]} and {centers[i]} are too close (gap={gap})");
        }
    }

    [Test]
    public void AllPlacedObjects_NoUnintendedOverlap()
    {
        BuildBehaviorTree_RoomOnly(roomCount: 3);
        spatialGenerator.Generate();

        var all = new List<(GameObject go, Bounds bounds)>();
        CollectAllWithBounds(spatialGenerator.sceneTreeParent, all);

        for (int i = 0; i < all.Count; i++)
        {
            for (int j = i + 1; j < all.Count; j++)
            {
                // Siblings under same parent might be flush (touching); allow zero volume
                float vol = IntersectionVolume(all[i].bounds, all[j].bounds);
                Assert.Less(vol, OverlapVolumeEpsilon,
                    $"Placements '{all[i].go.name}' and '{all[j].go.name}' should not overlap. " +
                    $"Intersection volume={vol}");
            }
        }
    }

    [Test]
    public void PlacementConfig_FitLeft_FirstSlotNearMinX()
    {
        BuildBehaviorTree_RoomOnly_WithFit(roomCount: 1, fitX: SGBehaviorTreeNode.FitX.Left);
        spatialGenerator.Generate();

        List<GameObject> roots = GetRootPlacedObjects();
        Assert.GreaterOrEqual(roots.Count, 1, "Should place at least one root room");
        Bounds b = GetWorldBounds(roots[0]);
        // Generator uses local space: bounds min.x = -generationSize.x/2 = -50. With fitX=Left, first slot center.x = min.x + halfOpt.x = -50 + 5 = -45 (optimal 10).
        float localExpectedX = -spatialGenerator.generationSize.x * 0.5f + 10f * 0.5f;
        Vector3 localExpected = new Vector3(localExpectedX, 0f, 0f);
        float expectedWorldX = spatialGenerator.transform.TransformPoint(localExpected).x;
        float tolerance = 2f; // allow some tolerance for transform
        Assert.Less(Mathf.Abs(b.center.x - expectedWorldX), tolerance,
            $"Fit Left: first slot center.x should be near {expectedWorldX}, got {b.center.x}");
    }

    [Test]
    public void PlacementConfig_StackDirection_PlacementsOrderedAlongStackAxis()
    {
        BuildBehaviorTree_RoomOnly(roomCount: 3);
        spatialGenerator.Generate();

        List<GameObject> roots = GetRootPlacedObjects();
        Assert.GreaterOrEqual(roots.Count, 2, "Should place at least 2 root rooms");
        var centersX = new List<float>();
        foreach (GameObject go in roots)
            centersX.Add(GetWorldBounds(go).center.x);
        centersX.Sort();
        // Stack PosX: slot 0, 1, 2 should have increasing X
        for (int i = 1; i < centersX.Count; i++)
            Assert.Greater(centersX[i], centersX[i - 1], "With stack PosX, placements should have strictly increasing X");
    }

    [Test]
    public void FlushPlacement_WallFaceTouchesRoomFace()
    {
        BuildBehaviorTree_RoomWithOneWall();
        spatialGenerator.Generate();

        // One room, one wall child (Forward flush). Wall's Back face should touch Room's Forward face.
        List<GameObject> roots = GetRootPlacedObjects();
        Assert.GreaterOrEqual(roots.Count, 1, "Should place at least one room");
        GameObject room = roots[0];
        Assert.Greater(room.transform.childCount, 0, "Room should have wall child");
        Transform wallT = room.transform.GetChild(0);
        GameObject wall = wallT.gameObject;

        Bounds roomB = GetWorldBounds(room);
        Bounds wallB = GetWorldBounds(wall);
        // Forward = +Z. Room front face at roomB.max.z, wall back face at wallB.min.z (if wall is forward of room).
        // Actually wall is placed "forward" of room center, so wall center.z > room center.z; wall back face = wallB.min.z, room front = roomB.max.z. They should be equal or very close.
        float roomFrontZ = roomB.max.z;
        float wallBackZ = wallB.min.z;
        float distance = Mathf.Abs(roomFrontZ - wallBackZ);
        Assert.Less(distance, FlushDistanceEpsilon,
            $"Flush placement: wall back face (z={wallBackZ}) should touch room front face (z={roomFrontZ}). Distance={distance}");
    }

    private void BuildBehaviorTree_RoomOnly_WithFit(int roomCount, SGBehaviorTreeNode.FitX fitX)
    {
        GameObject treeObj = new GameObject("BehaviorTree");
        treeObj.transform.SetParent(spatialGenerator.transform);
        var container = treeObj.AddComponent<SGTreeNodeContainer>();

        GameObject rootObj = new GameObject("room");
        rootObj.transform.SetParent(treeObj.transform);
        var root = rootObj.AddComponent<SGBehaviorTreeNode>();
        root.fitX = fitX;
        root.fitY = SGBehaviorTreeNode.FitY.Center;
        root.fitZ = SGBehaviorTreeNode.FitZ.Center;
        root.stackDirection = SGBehaviorTreeNode.AxisDirection.PosX;
        root.wrapDirection = SGBehaviorTreeNode.AxisDirection.PosZ;
        root.minSpace = new Vector3(10f, 6f, 10f);
        root.maxSpace = new Vector3(10f, 6f, 10f);
        root.optimalSpace = new Vector3(10f, 6f, 10f);
        root.placementLimit = roomCount;
        root.gameObjectPrefabs = new List<GameObject> { CreateCubePrefab(10f, 6f, 10f) };
        container.rootNode = root;
        spatialGenerator.behaviorTreeParent = treeObj.transform;
    }

    private void BuildBehaviorTree_RoomOnly(int roomCount)
    {
        GameObject treeObj = new GameObject("BehaviorTree");
        treeObj.transform.SetParent(spatialGenerator.transform);
        var container = treeObj.AddComponent<SGTreeNodeContainer>();

        GameObject rootObj = new GameObject("room");
        rootObj.transform.SetParent(treeObj.transform);
        var root = rootObj.AddComponent<SGBehaviorTreeNode>();
        root.minSpace = new Vector3(10f, 6f, 10f);
        root.maxSpace = new Vector3(10f, 6f, 10f);
        root.optimalSpace = new Vector3(10f, 6f, 10f);
        root.fitX = SGBehaviorTreeNode.FitX.Center;
        root.fitY = SGBehaviorTreeNode.FitY.Center;
        root.fitZ = SGBehaviorTreeNode.FitZ.Center;
        root.stackDirection = SGBehaviorTreeNode.AxisDirection.PosX;
        root.wrapDirection = SGBehaviorTreeNode.AxisDirection.PosZ;
        root.placementLimit = roomCount;
        root.gameObjectPrefabs = new List<GameObject> { CreateCubePrefab(10f, 6f, 10f) };
        container.rootNode = root;
        spatialGenerator.behaviorTreeParent = treeObj.transform;
    }

    private void BuildBehaviorTree_RoomWithOneWall()
    {
        GameObject treeObj = new GameObject("BehaviorTree");
        treeObj.transform.SetParent(spatialGenerator.transform);
        var container = treeObj.AddComponent<SGTreeNodeContainer>();

        GameObject rootObj = new GameObject("room");
        rootObj.transform.SetParent(treeObj.transform);
        var root = rootObj.AddComponent<SGBehaviorTreeNode>();
        root.minSpace = new Vector3(10f, 6f, 10f);
        root.maxSpace = new Vector3(10f, 6f, 10f);
        root.optimalSpace = new Vector3(10f, 6f, 10f);
        root.fitX = SGBehaviorTreeNode.FitX.Center;
        root.fitY = SGBehaviorTreeNode.FitY.Center;
        root.fitZ = SGBehaviorTreeNode.FitZ.Center;
        root.stackDirection = SGBehaviorTreeNode.AxisDirection.PosX;
        root.wrapDirection = SGBehaviorTreeNode.AxisDirection.PosZ;
        root.placementLimit = 1;
        root.gameObjectPrefabs = new List<GameObject> { CreateCubePrefab(10f, 6f, 10f) };

        GameObject wallObj = new GameObject("foreground-wall");
        wallObj.transform.SetParent(rootObj.transform);
        var wall = wallObj.AddComponent<SGBehaviorTreeNode>();
        wall.minSpace = new Vector3(5f, 5f, 0.5f);
        wall.maxSpace = new Vector3(6f, 5f, 1f);
        wall.optimalSpace = new Vector3(6f, 5f, 1f);
        wall.fitX = SGBehaviorTreeNode.FitX.Center;
        wall.fitY = SGBehaviorTreeNode.FitY.Center;
        wall.fitZ = SGBehaviorTreeNode.FitZ.Forward;
        wall.placeFlush = true;
        wall.placementLimit = 1;
        wall.gameObjectPrefabs = new List<GameObject> { CreateCubePrefab(6f, 5f, 1f) };

        root.childNodes = new List<SGBehaviorTreeNode> { wall };
        container.rootNode = root;
        spatialGenerator.behaviorTreeParent = treeObj.transform;
    }

    private static GameObject CreateCubePrefab(float sx, float sy, float sz)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "CubePrefab";
        cube.transform.localScale = new Vector3(sx, sy, sz);
        cube.SetActive(false);
        return cube;
    }
}
#endif
