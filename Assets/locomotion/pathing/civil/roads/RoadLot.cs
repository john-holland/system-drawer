using System;
using System.Collections.Generic;
using UnityEngine;

public enum RoadLotGradeMode
{
    Flat = 0,
    GradedHeightMap = 1,
    TerrainConform = 2
}

[Serializable]
public sealed class RoadLotOutlet
{
    public string roadSegmentId;
    [Range(0f, 1f)] public float distanceAlong01 = 0.5f;
    public float distanceAlongMeters = -1f;
    public float lateralSide = 1f;
    public float curbWidth = 2f;
}

/// <summary>Large flat/graded pad for TravelAgent — heightmap + 0..N road outlets.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Roads/Road Lot")]
public sealed class RoadLot : MonoBehaviour
{
    public string lotId;
    public string displayName;
    public RoadLotGradeMode gradeMode = RoadLotGradeMode.Flat;
    public Vector3 padSize = new Vector3(40f, 2f, 40f);
    public Texture2D heightMap;
    public float heightMapAmplitude = 4f;
    public Terrain terrainRef;
    public List<RoadLotOutlet> roadOutlets = new List<RoadLotOutlet>();
    public RoadLotBoundarySpline boundary;
    public LotGrassGrowthController grass;
    public bool registerCorridorOnAwake = true;

    static readonly List<RoadLot> Registry = new List<RoadLot>();

    public static IReadOnlyList<RoadLot> All => Registry;

    void Awake()
    {
        if (string.IsNullOrEmpty(lotId))
            lotId = gameObject.name;
        if (string.IsNullOrEmpty(displayName))
            displayName = lotId;
        if (boundary == null)
            boundary = GetComponent<RoadLotBoundarySpline>() ?? gameObject.AddComponent<RoadLotBoundarySpline>();
        boundary.EnsureClosedLoopDefault();
        if (!Registry.Contains(this))
            Registry.Add(this);
    }

    void OnDestroy() => Registry.Remove(this);

    public Bounds GetWorldBounds()
    {
        Vector3 size = Vector3.Scale(padSize, transform.lossyScale);
        return new Bounds(transform.position, size);
    }

    public float SampleHeight(Vector3 world)
    {
        switch (gradeMode)
        {
            case RoadLotGradeMode.TerrainConform:
                if (terrainRef != null)
                    return terrainRef.SampleHeight(world) + terrainRef.transform.position.y;
                break;
            case RoadLotGradeMode.GradedHeightMap:
                if (heightMap != null)
                {
                    Bounds b = GetWorldBounds();
                    float u = Mathf.InverseLerp(b.min.x, b.max.x, world.x);
                    float v = Mathf.InverseLerp(b.min.z, b.max.z, world.z);
                    Color c = heightMap.GetPixelBilinear(u, v);
                    return transform.position.y + c.r * heightMapAmplitude;
                }
                break;
        }
        return transform.position.y;
    }

    public bool ContainsXZ(Vector3 world)
    {
        Bounds b = GetWorldBounds();
        return world.x >= b.min.x && world.x <= b.max.x && world.z >= b.min.z && world.z <= b.max.z;
    }

    public bool HasOutletTo(string roadSegmentId)
    {
        if (string.IsNullOrEmpty(roadSegmentId) || roadOutlets == null) return false;
        for (int i = 0; i < roadOutlets.Count; i++)
            if (roadOutlets[i] != null && roadOutlets[i].roadSegmentId == roadSegmentId)
                return true;
        return false;
    }

    public Vector3 ArrivalWorld
    {
        get
        {
            if (boundary != null && boundary.controlPoints != null && boundary.controlPoints.Count > 0)
                return transform.TransformPoint(boundary.CentroidLocal());
            return transform.position;
        }
    }

    public static RoadLot FindById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        for (int i = 0; i < Registry.Count; i++)
            if (Registry[i] != null && Registry[i].lotId == id)
                return Registry[i];
        return null;
    }

    public static RoadLot FindNearest(Vector3 world, float maxDist = 200f)
    {
        RoadLot best = null;
        float bestSq = maxDist * maxDist;
        for (int i = 0; i < Registry.Count; i++)
        {
            var lot = Registry[i];
            if (lot == null) continue;
            float sq = (lot.ArrivalWorld - world).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = lot; }
        }
        return best;
    }

    public static RoadLot FindConnectedToRoad(string roadSegmentId, Vector3 near)
    {
        RoadLot best = null;
        float bestSq = float.MaxValue;
        for (int i = 0; i < Registry.Count; i++)
        {
            var lot = Registry[i];
            if (lot == null || !lot.HasOutletTo(roadSegmentId)) continue;
            float sq = (lot.ArrivalWorld - near).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; best = lot; }
        }
        return best;
    }
}
