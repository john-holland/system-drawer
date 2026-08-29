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
    Custom = 6,
    Cells = 7,
    Walls = 8,
    Doors = 9,
    Yard = 10,
    Support = 11,
    TunnelStress = 12,
    SurfaceMaterial = 13,
    Sidewalk = 14,
    Street = 15,
    Driveway = 16,
    Garage = 17,
    HouseFront = 18,
    HouseLeft = 19,
    HouseRight = 20,
    HouseBack = 21,
    Highway = 22,
    Overpass = 23,
    Underpass = 24,
    Debris = 25,
    StreetLight = 26,
    GrassStrip = 27,
    CampusQuad = 28,
    CampusPath = 29,
    CampusDorm = 30,
    CampusLecture = 31,
    CampusLibrary = 32,
    CampusDining = 33,
    CampusMaintenance = 34,
    CampusParking = 35,
    CourtBench = 36,
    CourtWell = 37,
    CourtJury = 38,
    CourtGallery = 39,
    CourtBar = 40
}

public enum CityPixelCrowdHint
{
    None = 0,
    Flock = 1,
    Congregate = 2,
    Commute = 3
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
    PlaceableTypeSeparator = 11,
    Cell = 12,
    DrivewayLot = 13,
    GarageLot = 14,
    RoadLanes = 15,
    Overpass = 16,
    Bridge = 17,
    BridgeAndUnderpass = 18,
    Debris = 19,
    StreetLight = 20,
    TrafficSignal = 21,
    PhonePole = 22,
    PedCallButton = 23,
    Crosswalk = 24,
    Sidewalk = 25,
    GrassStrip = 26,
    WireEnd = 27,
    JerseyBarrier = 28,
    GuardRail = 29,
    Select = 30
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
    [CronExpr] public string scheduleCron = "0 7-9 * * 1-5";
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
    [Range(0f, 1f)] public float tunnelStress01;
    public bool wallDestructible = true;
    public bool diggable = true;
    public RoadLaneConfigAsset laneConfig;
    public StreetLightKind streetLightKind = StreetLightKind.Luminaire;
    public int shoulderSign = 1;
    public float spacingAlongM = 28f;
    public string approachId = "main";
    [Range(0f, 2f)] public float stopPotential01 = 1f;
    public bool bendWithRoad = true;
    public string poleId;
    public string wireId;
    public float wireT01 = 0.5f;
    public StreetWireEndKind wireEndKind = StreetWireEndKind.TrafficSignal;
    public int barCount = 6;
    public float barWidthM = 0.4f;
    public bool acrossLanes = true;
    public float stripWidthM = 0.8f;
    [Tooltip("Crowd / RTS hint for campus and city cells.")]
    public CityPixelCrowdHint crowdHint = CityPixelCrowdHint.None;
    public string flockGroupId;
    public string ambulationCacheKey;
    [Range(0f, 1f)] public float cacheLikelihood01 = 0.35f;
    public float cacheToleranceM = 1.5f;
    public TravelAuthoringRow travelHintRow;
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

    public void SetBrushStampStacked(CityPixelBrushStamp stamp)
    {
        if (stamp == null) return;
        if (brushStamps == null) brushStamps = new List<CityPixelBrushStamp>();
        int maxFloor = -1;
        for (int i = 0; i < brushStamps.Count; i++)
        {
            var s = brushStamps[i];
            if (s != null && s.frameIndex == stamp.frameIndex && s.cellX == stamp.cellX && s.cellY == stamp.cellY)
                maxFloor = Mathf.Max(maxFloor, s.floorIndex);
        }
        if (maxFloor >= 0)
            stamp.floorIndex = maxFloor + 1;
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
            case CityPixelBrushKind.Cell: return new Color(0.45f, 0.35f, 0.2f);
            case CityPixelBrushKind.DrivewayLot: return new Color(0.45f, 0.45f, 0.48f);
            case CityPixelBrushKind.GarageLot: return new Color(0.4f, 0.38f, 0.35f);
            case CityPixelBrushKind.RoadLanes: return new Color(0.25f, 0.25f, 0.28f);
            case CityPixelBrushKind.Overpass: return new Color(0.45f, 0.45f, 0.5f);
            case CityPixelBrushKind.Bridge: return new Color(0.4f, 0.42f, 0.48f);
            case CityPixelBrushKind.BridgeAndUnderpass: return new Color(0.35f, 0.3f, 0.25f);
            case CityPixelBrushKind.Debris: return new Color(0.55f, 0.4f, 0.2f);
            case CityPixelBrushKind.StreetLight: return new Color(0.95f, 0.9f, 0.4f);
            case CityPixelBrushKind.TrafficSignal: return new Color(0.2f, 0.85f, 0.35f);
            case CityPixelBrushKind.PhonePole: return new Color(0.55f, 0.4f, 0.2f);
            case CityPixelBrushKind.PedCallButton: return new Color(0.3f, 0.6f, 0.9f);
            case CityPixelBrushKind.Crosswalk: return new Color(0.95f, 0.95f, 0.9f);
            case CityPixelBrushKind.Sidewalk: return new Color(0.72f, 0.72f, 0.7f);
            case CityPixelBrushKind.GrassStrip: return new Color(0.25f, 0.6f, 0.25f);
            case CityPixelBrushKind.WireEnd: return new Color(0.15f, 0.15f, 0.18f);
            case CityPixelBrushKind.JerseyBarrier: return new Color(0.75f, 0.72f, 0.65f);
            case CityPixelBrushKind.GuardRail: return new Color(0.55f, 0.58f, 0.5f);
            case CityPixelBrushKind.Select: return new Color(1f, 1f, 1f);
            default: return Color.gray;
        }
    }

    public void EnsurePrisonLayers()
    {
        EnsureLayersAndFrames();
        AddLayerIfMissing("cells", CityPixelLayerKind.Cells, new Color(0.45f, 0.35f, 0.2f));
        AddLayerIfMissing("walls", CityPixelLayerKind.Walls, new Color(0.35f, 0.35f, 0.38f));
        AddLayerIfMissing("doors", CityPixelLayerKind.Doors, new Color(0.7f, 0.45f, 0.2f));
        AddLayerIfMissing("yard", CityPixelLayerKind.Yard, new Color(0.25f, 0.55f, 0.25f));
        AddLayerIfMissing("support", CityPixelLayerKind.Support, new Color(0.55f, 0.55f, 0.6f));
        AddLayerIfMissing("tunnel_stress", CityPixelLayerKind.TunnelStress, new Color(0.85f, 0.2f, 0.15f));
        AddLayerIfMissing("surface_material", CityPixelLayerKind.SurfaceMaterial, new Color(0.5f, 0.4f, 0.3f));
        EnsureLayersAndFrames();
    }

    public void EnsureHouseLayers()
    {
        EnsureLayersAndFrames();
        AddLayerIfMissing("street", CityPixelLayerKind.Street, new Color(0.32f, 0.32f, 0.36f));
        AddLayerIfMissing("sidewalk", CityPixelLayerKind.Sidewalk, new Color(0.72f, 0.72f, 0.7f));
        AddLayerIfMissing("yard", CityPixelLayerKind.Yard, new Color(0.25f, 0.55f, 0.25f));
        AddLayerIfMissing("driveway", CityPixelLayerKind.Driveway, new Color(0.45f, 0.45f, 0.48f));
        AddLayerIfMissing("garage", CityPixelLayerKind.Garage, new Color(0.4f, 0.38f, 0.35f));
        AddLayerIfMissing("house_front", CityPixelLayerKind.HouseFront, new Color(0.75f, 0.55f, 0.4f));
        AddLayerIfMissing("house_left", CityPixelLayerKind.HouseLeft, new Color(0.7f, 0.5f, 0.38f));
        AddLayerIfMissing("house_right", CityPixelLayerKind.HouseRight, new Color(0.68f, 0.48f, 0.36f));
        AddLayerIfMissing("house_back", CityPixelLayerKind.HouseBack, new Color(0.62f, 0.44f, 0.34f));
        EnsureLayersAndFrames();
    }

    public void EnsureHighwayLayers()
    {
        EnsureLayersAndFrames();
        AddLayerIfMissing("highway", CityPixelLayerKind.Highway, new Color(0.28f, 0.28f, 0.32f));
        AddLayerIfMissing("overpass", CityPixelLayerKind.Overpass, new Color(0.45f, 0.45f, 0.5f));
        AddLayerIfMissing("underpass", CityPixelLayerKind.Underpass, new Color(0.22f, 0.2f, 0.18f));
        AddLayerIfMissing("debris", CityPixelLayerKind.Debris, new Color(0.55f, 0.4f, 0.2f));
        AddLayerIfMissing("street_light", CityPixelLayerKind.StreetLight, new Color(0.95f, 0.9f, 0.4f));
        AddLayerIfMissing("grass_strip", CityPixelLayerKind.GrassStrip, new Color(0.25f, 0.6f, 0.25f));
        AddLayerIfMissing("support", CityPixelLayerKind.Support, new Color(0.55f, 0.55f, 0.6f));
        EnsureHouseLayers();
        EnsureLayersAndFrames();
    }

    public void EnsureCampusLayers()
    {
        EnsureLayersAndFrames();
        AddLayerIfMissing("campus_quad", CityPixelLayerKind.CampusQuad, new Color(0.28f, 0.62f, 0.32f));
        AddLayerIfMissing("campus_path", CityPixelLayerKind.CampusPath, new Color(0.55f, 0.52f, 0.45f));
        AddLayerIfMissing("campus_dorm", CityPixelLayerKind.CampusDorm, new Color(0.62f, 0.42f, 0.32f));
        AddLayerIfMissing("campus_lecture", CityPixelLayerKind.CampusLecture, new Color(0.35f, 0.45f, 0.72f));
        AddLayerIfMissing("campus_library", CityPixelLayerKind.CampusLibrary, new Color(0.5f, 0.35f, 0.65f));
        AddLayerIfMissing("campus_dining", CityPixelLayerKind.CampusDining, new Color(0.8f, 0.55f, 0.25f));
        AddLayerIfMissing("campus_maintenance", CityPixelLayerKind.CampusMaintenance, new Color(0.7f, 0.7f, 0.3f));
        AddLayerIfMissing("campus_parking", CityPixelLayerKind.CampusParking, new Color(0.28f, 0.28f, 0.32f));
        EnsureLayersAndFrames();
    }

    public void EnsureCourtroomLayers()
    {
        EnsureLayersAndFrames();
        AddLayerIfMissing("court_bench", CityPixelLayerKind.CourtBench, new Color(0.45f, 0.28f, 0.18f));
        AddLayerIfMissing("court_well", CityPixelLayerKind.CourtWell, new Color(0.72f, 0.68f, 0.55f));
        AddLayerIfMissing("court_jury", CityPixelLayerKind.CourtJury, new Color(0.35f, 0.5f, 0.72f));
        AddLayerIfMissing("court_gallery", CityPixelLayerKind.CourtGallery, new Color(0.55f, 0.42f, 0.62f));
        AddLayerIfMissing("court_bar", CityPixelLayerKind.CourtBar, new Color(0.62f, 0.38f, 0.22f));
        EnsureLayersAndFrames();
    }

    void AddLayerIfMissing(string id, CityPixelLayerKind kind, Color color)
    {
        for (int i = 0; i < layers.Count; i++)
            if (layers[i] != null && layers[i].layerId == id) return;
        layers.Add(new CityPixelLayer { layerId = id, kind = kind, color = color });
    }

    public void PaintLayerCell(CityPixelLayerKind kind, int frameIndex, int x, int y, byte value = 1)
    {
        EnsureLayersAndFrames();
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (layer == null || layer.kind != kind) continue;
            if (frameIndex < 0 || frameIndex >= layer.frames.Count) return;
            layer.frames[frameIndex].Set(x, y, width, value);
            return;
        }
    }

    /// <summary>Export painted cell/door/wall clusters as Bounds4 payloads (PrisonCellVolume / DigContactCentroid).</summary>
    public List<Bounds4> ExportPrisonClustersToBounds4(int frameIndex)
    {
        var volumes = new List<Bounds4>();
        EnsureLayersAndFrames();
        frameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, frameCount - 1));
        ExportKindClusters(CityPixelLayerKind.Cells, frameIndex, volumes);
        ExportKindClusters(CityPixelLayerKind.Doors, frameIndex, volumes);
        ExportKindClusters(CityPixelLayerKind.Walls, frameIndex, volumes);
        return volumes;
    }

    /// <summary>Export painted courtroom clusters as Bounds4 payloads.</summary>
    public List<Bounds4> ExportCourtroomClustersToBounds4(int frameIndex)
    {
        var volumes = new List<Bounds4>();
        EnsureLayersAndFrames();
        frameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, frameCount - 1));
        ExportKindClusters(CityPixelLayerKind.CourtBench, frameIndex, volumes);
        ExportKindClusters(CityPixelLayerKind.CourtWell, frameIndex, volumes);
        ExportKindClusters(CityPixelLayerKind.CourtJury, frameIndex, volumes);
        ExportKindClusters(CityPixelLayerKind.CourtGallery, frameIndex, volumes);
        ExportKindClusters(CityPixelLayerKind.CourtBar, frameIndex, volumes);
        return volumes;
    }

    void ExportKindClusters(CityPixelLayerKind kind, int frameIndex, List<Bounds4> into)
    {
        CityPixelLayer layer = null;
        for (int i = 0; i < layers.Count; i++)
            if (layers[i] != null && layers[i].kind == kind) { layer = layers[i]; break; }
        if (layer == null || layer.frames == null || frameIndex >= layer.frames.Count) return;
        var frame = layer.frames[frameIndex];
        bool[] seen = new bool[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int i = x + y * width;
            if (seen[i] || frame.Get(x, y, width) == 0) continue;
            int minX = x, maxX = x, minY = y, maxY = y;
            var q = new Queue<Vector2Int>();
            q.Enqueue(new Vector2Int(x, y));
            seen[i] = true;
            while (q.Count > 0)
            {
                var p = q.Dequeue();
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                TryFlood(frame, seen, q, p.x + 1, p.y);
                TryFlood(frame, seen, q, p.x - 1, p.y);
                TryFlood(frame, seen, q, p.x, p.y + 1);
                TryFlood(frame, seen, q, p.x, p.y - 1);
            }
            into.Add(CellClusterToBounds4(minX, minY, maxX, maxY, frameIndex));
        }
    }

    void TryFlood(CityPixelFrame frame, bool[] seen, Queue<Vector2Int> q, int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        int i = x + y * width;
        if (seen[i] || frame.Get(x, y, width) == 0) return;
        seen[i] = true;
        q.Enqueue(new Vector2Int(x, y));
    }
}
