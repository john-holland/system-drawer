using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

public enum CityPixelLayerKind
{
    Roads = 0,
    PowerLinesDown = 1,
    Flood = 2,
    Protest = 3,
    Construction = 4,
    TrafficBlock = 5,
    Custom = 6
}

public enum CityPixelBrushKind
{
    Eraser = 0,
    PoliceDetail = 1,
    OneWay = 2,
    Detour = 3,
    StopSign = 4,
    Intersection = 5,
    SchoolBusStop = 6,
    Building = 7,
    Sign = 8,
    BuildingTypeSeparator = 9,
    IntersectionTypeSeparator = 10,
    PlaceableTypeSeparator = 11
}

[Serializable]
public sealed class CityPixelFrame
{
    public byte[] cells = Array.Empty<byte>();

    public void EnsureSize(int width, int height)
    {
        int n = Mathf.Max(1, width) * Mathf.Max(1, height);
        if (cells == null || cells.Length != n)
            cells = new byte[n];
    }

    public byte Get(int x, int y, int width) =>
        cells != null && x >= 0 && y >= 0 && x + y * width < cells.Length ? cells[x + y * width] : (byte)0;

    public void Set(int x, int y, int width, byte value)
    {
        if (cells == null) return;
        int i = x + y * width;
        if (i >= 0 && i < cells.Length) cells[i] = value;
    }
}

[Serializable]
public sealed class CityPixelLayer
{
    public string layerId = "roads";
    public CityPixelLayerKind kind = CityPixelLayerKind.Roads;
    public Color color = new Color(0.35f, 0.35f, 0.4f, 1f);
    public List<CityPixelFrame> frames = new List<CityPixelFrame>();
}

[Serializable]
public sealed class CityTrafficEventSpec
{
    public string eventId = "evt";
    public string title = "City Event";
    public string layerId;
    public int frameStart;
    public int frameEnd = 1;
    public NarrativeCalendarAsset calendar;
    public string narrativeActionHints;
    public Bounds4? volumeOverride;
}

[Serializable]
public sealed class CityPixelBrushStamp
{
    public int frameIndex;
    public int cellX;
    public int cellY;
    public CityPixelBrushKind kind = CityPixelBrushKind.Sign;
    public float yawDegrees;
    public string payloadJson;
    public GameObject signPrefab;
    public GameObject[] signagePrefabs;
    public TrafficDetailLadderAsset ladderAsset;
    public PixelLightPatternAsset pixelLightPattern;
    public PixelLightColorPackage pixelLightColors;
    public BuildingRequirementSpec buildingConfig;
    public CivilSystemKind buildingKind;
    public string buildingTypeId;
    public TASignKind signKind = TASignKind.Stop;
    public string placementPrompt;
    public int detourGoalCellX;
    public int detourGoalCellY;
    [Tooltip("When true, corridor cell for this stamp is treated as lane-disabled (airport/road repair).")]
    public bool laneDisabled;
    [Tooltip("Detour / roadside decor prefabs facing out from the street.")]
    public GameObject[] detourPrefabs;
    public float stopRadius = 8f;
    public string scheduleCron = "0 7-9 * * 1-5";
    public float avoidCostMultiplier = 3f;
    public float slowRadius = 12f;
    [Tooltip("Story height in cells for this stamp (chunk height = max).")]
    public int heightCells = 1;
    [Tooltip("Forced shell candidate id from CityPlaceableCatalog.")]
    public string candidateId;
    [Tooltip("Tenant / placeable type key (defaults from buildingKind / kind).")]
    public string typeKey;
    [Tooltip("Intended floor when painting into a shared shell (0 = ground).")]
    public int floorIndex;
    [Tooltip("Optional zone within floor for FloorPlanIndexMap.")]
    public string zoneId;
    [Tooltip("Optional floor-plan map override for this stamp/chunk.")]
    public FloorPlanIndexMap floorPlanIndexMap;
}

[Serializable]
public sealed class CityPixelBakedNode
{
    public int cellX;
    public int cellY;
    public Vector3 world;
    public long corridorId;
}

[Serializable]
public sealed class CityPixelBakedMstEdge
{
    public long a;
    public long b;
    public float length;
    public float demand;
}

[Serializable]
public sealed class CityPixelBakedCacheLayer
{
    public int frameIndex;
    public List<CityPixelBakedNode> nodes = new List<CityPixelBakedNode>();
    public List<CityPixelBakedMstEdge> mstEdges = new List<CityPixelBakedMstEdge>();
    public byte[] corridorCellMarks = Array.Empty<byte>();
}

/// <summary>City-scale pixel grid: location, frames, narrative layers, brush stamps, baked MST caches.</summary>
[CreateAssetMenu(fileName = "CityPixelGrid", menuName = "Locomotion/Civil/City Pixel Grid")]
public sealed class CityPixelGrid : ScriptableObject
{
    public Vector3 worldOrigin;
    public float cellWorldSize = 1f;
    public int width = 32;
    public int height = 32;
    public int frameCount = 1;
    public float frameGranularitySec = 60f;
    public List<BaseAmbulatingActor> actorsForSizing = new List<BaseAmbulatingActor>();
    public List<CityPixelLayer> layers = new List<CityPixelLayer>();
    public List<CityTrafficEventSpec> trafficEvents = new List<CityTrafficEventSpec>();
    public List<CityPixelBrushStamp> brushStamps = new List<CityPixelBrushStamp>();
    public List<CityPixelBakedCacheLayer> bakedCaches = new List<CityPixelBakedCacheLayer>();
    [Tooltip("Catalog of scalable placeable shells (buildings, intersections, bus stops).")]
    public CityPlaceableCatalog catalog;

    public int CellCount => Mathf.Max(1, width) * Mathf.Max(1, height);

    public Vector3 CellToWorld(int x, int y)
    {
        float c = Mathf.Max(0.25f, cellWorldSize);
        return worldOrigin + new Vector3((x + 0.5f) * c, 0f, (y + 0.5f) * c);
    }

    public bool WorldToCell(Vector3 world, out int x, out int y)
    {
        float c = Mathf.Max(0.25f, cellWorldSize);
        x = Mathf.FloorToInt((world.x - worldOrigin.x) / c);
        y = Mathf.FloorToInt((world.z - worldOrigin.z) / c);
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    public Bounds4 CellClusterToBounds4(int minX, int minY, int maxX, int maxY, int frameIndex)
    {
        float c = Mathf.Max(0.25f, cellWorldSize);
        Vector3 min = worldOrigin + new Vector3(minX * c, -1f, minY * c);
        Vector3 max = worldOrigin + new Vector3((maxX + 1) * c, 4f, (maxY + 1) * c);
        Vector3 center = (min + max) * 0.5f;
        Vector3 size = max - min;
        float t0 = frameIndex * frameGranularitySec;
        float t1 = (frameIndex + 1) * frameGranularitySec;
        return new Bounds4(center, size, t0, t1);
    }

    public void EnsureLayersAndFrames()
    {
        if (layers == null) layers = new List<CityPixelLayer>();
        if (layers.Count == 0)
        {
            layers.Add(new CityPixelLayer { layerId = "roads", kind = CityPixelLayerKind.Roads, color = new Color(0.3f, 0.3f, 0.35f) });
            layers.Add(new CityPixelLayer { layerId = "power_lines_down", kind = CityPixelLayerKind.PowerLinesDown, color = new Color(0.9f, 0.7f, 0.1f) });
            layers.Add(new CityPixelLayer { layerId = "traffic_block", kind = CityPixelLayerKind.TrafficBlock, color = new Color(0.85f, 0.2f, 0.15f) });
        }

        frameCount = Mathf.Max(1, frameCount);
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        for (int li = 0; li < layers.Count; li++)
        {
            var layer = layers[li];
            if (layer.frames == null) layer.frames = new List<CityPixelFrame>();
            while (layer.frames.Count < frameCount)
                layer.frames.Add(new CityPixelFrame());
            for (int f = 0; f < frameCount; f++)
                layer.frames[f].EnsureSize(width, height);
        }
    }

    /// <summary>Set cellWorldSize from the smallest actor footprint in actorsForSizing.</summary>
    public float RecalculateCellSize()
    {
        float minFoot = float.PositiveInfinity;
        if (actorsForSizing != null)
        {
            for (int i = 0; i < actorsForSizing.Count; i++)
            {
                var actor = actorsForSizing[i];
                if (actor == null) continue;
                if (!ActorPhysicalCentroid.TryBuildProfile(actor, out var profile)) continue;
                float footprint = Mathf.Min(profile.minSpace.x, profile.minSpace.z);
                footprint = Mathf.Min(footprint, profile.capsuleRadius * 2f);
                if (footprint > 0.01f && footprint < minFoot)
                    minFoot = footprint;
            }
        }

        if (float.IsInfinity(minFoot))
            minFoot = cellWorldSize > 0.01f ? cellWorldSize : 1f;
        cellWorldSize = Mathf.Max(0.25f, minFoot);
        return cellWorldSize;
    }

    public CityPixelBakedCacheLayer FindBake(int frameIndex)
    {
        if (bakedCaches == null) return null;
        for (int i = 0; i < bakedCaches.Count; i++)
            if (bakedCaches[i] != null && bakedCaches[i].frameIndex == frameIndex)
                return bakedCaches[i];
        return null;
    }

    public void UpsertBake(CityPixelBakedCacheLayer bake)
    {
        if (bake == null) return;
        if (bakedCaches == null) bakedCaches = new List<CityPixelBakedCacheLayer>();
        for (int i = 0; i < bakedCaches.Count; i++)
        {
            if (bakedCaches[i] != null && bakedCaches[i].frameIndex == bake.frameIndex)
            {
                bakedCaches[i] = bake;
                return;
            }
        }
        bakedCaches.Add(bake);
    }

    public void SetBrushStamp(CityPixelBrushStamp stamp)
    {
        if (brushStamps == null) brushStamps = new List<CityPixelBrushStamp>();
        for (int i = 0; i < brushStamps.Count; i++)
        {
            var s = brushStamps[i];
            if (s.frameIndex == stamp.frameIndex && s.cellX == stamp.cellX && s.cellY == stamp.cellY)
            {
                brushStamps[i] = stamp;
                return;
            }
        }
        brushStamps.Add(stamp);
    }

    public bool ClearBrushStamp(int frameIndex, int cellX, int cellY)
    {
        if (brushStamps == null) return false;
        for (int i = brushStamps.Count - 1; i >= 0; i--)
        {
            var s = brushStamps[i];
            if (s.frameIndex == frameIndex && s.cellX == cellX && s.cellY == cellY)
            {
                brushStamps.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    public static Color BrushColor(CityPixelBrushKind kind)
    {
        switch (kind)
        {
            case CityPixelBrushKind.PoliceDetail: return new Color(0.2f, 0.35f, 1f);
            case CityPixelBrushKind.OneWay: return new Color(0.2f, 0.8f, 0.9f);
            case CityPixelBrushKind.Detour: return new Color(1f, 0.55f, 0.1f);
            case CityPixelBrushKind.StopSign: return new Color(0.95f, 0.15f, 0.15f);
            case CityPixelBrushKind.Intersection: return new Color(0.9f, 0.9f, 0.2f);
            case CityPixelBrushKind.SchoolBusStop: return new Color(1f, 0.85f, 0.1f);
            case CityPixelBrushKind.Building: return new Color(0.55f, 0.4f, 0.25f);
            case CityPixelBrushKind.Sign: return new Color(0.7f, 0.7f, 0.75f);
            case CityPixelBrushKind.BuildingTypeSeparator: return new Color(0.35f, 0.15f, 0.1f, 0.85f);
            case CityPixelBrushKind.IntersectionTypeSeparator: return new Color(0.55f, 0.55f, 0.1f, 0.85f);
            case CityPixelBrushKind.PlaceableTypeSeparator: return new Color(0.4f, 0.2f, 0.45f, 0.85f);
            default: return Color.gray;
        }
    }
}
