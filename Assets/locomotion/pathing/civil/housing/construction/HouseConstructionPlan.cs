using System;
using System.Collections.Generic;
using Locomotion.Narrative;
using SdfMax;
using UnityEngine;

public enum HouseConstructionLayerKind
{
    DigSite = 0,
    Foundation = 1,
    Footings = 2,
    Studs = 3,
    Insulation = 4,
    Openings = 5,
    RoughMEP = 6,
    Sheathing = 7,
    EavesGutters = 8,
    YardPatioWalks = 9,
    DeckRailings = 10,
    Fences = 11,
    Finish = 12,
    Furnishings = 13
}

public enum HouseFinishFloorKind
{
    Slab = 0,
    Wood = 1,
    Deck = 2
}

public enum HouseEnvelopeSide
{
    Front = 0,
    Right = 1,
    Back = 2,
    Left = 3
}

[Serializable]
public sealed class HouseConstructionFloorParams
{
    public int floorIndex = 1;
    public string label = "first";
    public float storyHeightM = 2.7f;
    public HouseFinishFloorKind finishFloorKind = HouseFinishFloorKind.Wood;
    public int pixelLightGridW = 8;
    public int pixelLightGridH = 8;
    public float pixelLightCellSize = 0.25f;
    public string railingPreset = "default";
    public string deckWallPreset = "default";
}

[Serializable]
public sealed class HouseConstructionLayer
{
    public string layerId = "foundation";
    public HouseConstructionLayerKind kind = HouseConstructionLayerKind.Foundation;
    public Color color = new Color(0.55f, 0.45f, 0.3f);
    public List<CityPixelFrame> frames = new List<CityPixelFrame>();
}

[Serializable]
public sealed class HouseEnvelopeDisplacementLayer
{
    public string layerId = "height";
    public HouseEnvelopeSide side = HouseEnvelopeSide.Front;
    public int floorIndex = 1;
    public PixelLightLayerComposite composite = PixelLightLayerComposite.Max;
    public float[,] height;
    public PixelLightPatternAsset luminance;
}

/// <summary>Layered house construction plan (city-pixel UX, construction timeline frames).</summary>
[CreateAssetMenu(fileName = "HouseConstructionPlan", menuName = "Locomotion/Civil/House Construction Plan")]
public sealed class HouseConstructionPlan : ScriptableObject
{
    public Vector3 worldOrigin;
    public float cellWorldSize = 1f;
    public int width = 16;
    public int height = 16;
    public int frameCount = 8;
    public float frameGranularitySec = 3600f;
    public float storyHeightM = 2.7f;
    public int activeFloorIndex = 1;
    public string floorText = "first";
    public List<HouseConstructionFloorParams> floors = new List<HouseConstructionFloorParams>();
    public List<HouseConstructionLayer> layers = new List<HouseConstructionLayer>();
    public SdfMaxCompositionAsset hardSdf;
    public List<HouseEnvelopeDisplacementLayer> envelopeLayers = new List<HouseEnvelopeDisplacementLayer>();
    public WallBrushCatalog wallBrushes;

    public void ApplyFloorText()
    {
        if (HouseFloorIndex.TryParse(floorText, out int idx))
            activeFloorIndex = idx;
        floorText = HouseFloorIndex.Format(activeFloorIndex);
    }

    public HouseConstructionFloorParams GetOrCreateFloor(int floorIndex)
    {
        if (floors == null) floors = new List<HouseConstructionFloorParams>();
        for (int i = 0; i < floors.Count; i++)
            if (floors[i] != null && floors[i].floorIndex == floorIndex)
                return floors[i];
        var p = new HouseConstructionFloorParams
        {
            floorIndex = floorIndex,
            label = HouseFloorIndex.Format(floorIndex),
            storyHeightM = storyHeightM
        };
        floors.Add(p);
        return p;
    }

    public void EnsureDefaultLayers()
    {
        if (layers == null) layers = new List<HouseConstructionLayer>();
        AddIfMissing("dig_site", HouseConstructionLayerKind.DigSite, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.DigSite));
        AddIfMissing("foundation", HouseConstructionLayerKind.Foundation, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Foundation));
        AddIfMissing("footings", HouseConstructionLayerKind.Footings, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Footings));
        AddIfMissing("studs", HouseConstructionLayerKind.Studs, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Studs));
        AddIfMissing("insulation", HouseConstructionLayerKind.Insulation, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Insulation));
        AddIfMissing("openings", HouseConstructionLayerKind.Openings, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Openings));
        AddIfMissing("rough_mep", HouseConstructionLayerKind.RoughMEP, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.RoughMEP));
        AddIfMissing("sheathing", HouseConstructionLayerKind.Sheathing, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Sheathing));
        AddIfMissing("eaves_gutters", HouseConstructionLayerKind.EavesGutters, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.EavesGutters));
        AddIfMissing("yard", HouseConstructionLayerKind.YardPatioWalks, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.YardPatioWalks));
        AddIfMissing("deck_railings", HouseConstructionLayerKind.DeckRailings, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.DeckRailings));
        AddIfMissing("fences", HouseConstructionLayerKind.Fences, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Fences));
        AddIfMissing("finish", HouseConstructionLayerKind.Finish, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Finish));
        AddIfMissing("furnishings", HouseConstructionLayerKind.Furnishings, HouseFoundationPalette.LayerColor(HouseConstructionLayerKind.Furnishings));
        frameCount = Mathf.Max(1, frameCount);
        width = Mathf.Max(1, width);
        height = Mathf.Max(1, height);
        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (layer == null) continue;
            layer.color = HouseFoundationPalette.LayerColor(layer.kind);
            if (layer.frames == null) layer.frames = new List<CityPixelFrame>();
            while (layer.frames.Count < frameCount)
                layer.frames.Add(new CityPixelFrame());
            for (int f = 0; f < frameCount; f++)
                layer.frames[f].EnsureSize(width, height);
        }
        if (floors == null || floors.Count == 0)
        {
            floors = new List<HouseConstructionFloorParams>
            {
                GetOrCreateFloor(-1),
                GetOrCreateFloor(0),
                GetOrCreateFloor(1)
            };
        }
        wallBrushes?.EnsureBuiltins();
    }

    void AddIfMissing(string id, HouseConstructionLayerKind kind, Color color)
    {
        for (int i = 0; i < layers.Count; i++)
            if (layers[i] != null && layers[i].layerId == id) return;
        layers.Add(new HouseConstructionLayer { layerId = id, kind = kind, color = color });
    }

    public List<Bounds4> ExportLayerClustersToBounds4(HouseConstructionLayerKind kind, int frameIndex)
    {
        var volumes = new List<Bounds4>();
        EnsureDefaultLayers();
        frameIndex = Mathf.Clamp(frameIndex, 0, Mathf.Max(0, frameCount - 1));
        for (int li = 0; li < layers.Count; li++)
        {
            var layer = layers[li];
            if (layer == null || layer.kind != kind || frameIndex >= layer.frames.Count) continue;
            var frame = layer.frames[frameIndex];
            bool[,] seen = new bool[width, height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (seen[x, y] || frame.Get(x, y, width) == 0) continue;
                int maxX = x, maxY = y;
                Flood(frame, seen, x, y, ref maxX, ref maxY);
                volumes.Add(CellClusterToBounds4(x, y, maxX, maxY, frameIndex));
            }
        }
        return volumes;
    }

    public Bounds4 CellClusterToBounds4(int minX, int minY, int maxX, int maxY, int frameIndex)
    {
        float c = Mathf.Max(0.25f, cellWorldSize);
        float y0 = HouseFloorIndex.FloorY(activeFloorIndex, storyHeightM, worldOrigin.y);
        Vector3 min = worldOrigin + new Vector3(minX * c, y0 - 0.2f, minY * c);
        Vector3 max = worldOrigin + new Vector3((maxX + 1) * c, y0 + storyHeightM, (maxY + 1) * c);
        float t0 = frameIndex * frameGranularitySec;
        float t1 = (frameIndex + 1) * frameGranularitySec;
        return new Bounds4((min + max) * 0.5f, max - min, t0, t1);
    }

    public SdfMaxCompositionAsset BakeSoftToHard()
    {
        hardSdf = SdfMaxSoftToHardBaker.BakeBoxUnionWithOpenings(new Vector3(width * 0.5f, storyHeightM * 0.5f, height * 0.5f), null, 0.4f);
        StampEnvelopeOntoHardSdf();
        return hardSdf;
    }

    /// <summary>Max-union toroid displacement stamps (filtered by floor/side at authoring time) onto the house box SDF.</summary>
    public void StampEnvelopeOntoHardSdf()
    {
        if (hardSdf == null)
            hardSdf = SdfMaxSoftToHardBaker.BakeBoxUnionWithOpenings(
                new Vector3(width * 0.5f, storyHeightM * 0.5f, height * 0.5f), null, 0.4f);
        if (envelopeLayers == null || hardSdf.nodes == null)
            return;
        for (int i = 0; i < envelopeLayers.Count; i++)
        {
            var layer = envelopeLayers[i];
            if (layer == null || layer.height == null)
                continue;
            var torus = SdfMaxSoftToHardBaker.BakeDisplacedTorus(2f, 0.35f, layer.height, 0.2f);
            if (torus?.nodes == null || torus.nodes.Count == 0)
                continue;
            int torusIdx = hardSdf.nodes.Count;
            var src = torus.nodes[0];
            hardSdf.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.DisplacedTorus,
                torusMajorRadius = src.torusMajorRadius,
                torusMinorRadius = src.torusMinorRadius,
                weight = src.weight,
                constantValue = src.constantValue,
                halfExtents = src.halfExtents
            });
            int prev = hardSdf.ResolveRootIndex();
            hardSdf.nodes.Add(new SdfMaxNode
            {
                op = SdfMaxOp.Max,
                childIndexA = prev,
                childIndexB = torusIdx
            });
            hardSdf.rootNodeIndex = hardSdf.nodes.Count - 1;
            UnityEngine.Object.DestroyImmediate(torus);
        }
    }

    public int ExportNarrativeEvents(NarrativeCalendarAsset calendar)
    {
        if (calendar == null) return 0;
        if (calendar.events == null) calendar.events = new List<NarrativeCalendarEvent>();
        int n = 0;
        EnsureDefaultLayers();
        for (int f = 0; f < frameCount; f++)
        {
            calendar.events.Add(new NarrativeCalendarEvent
            {
                id = $"house_build_{f}",
                title = $"Construction frame {f}",
                durationSeconds = Mathf.RoundToInt(frameGranularitySec),
                tags = new List<string> { "construction", "house" },
                spatiotemporalVolume = CellClusterToBounds4(0, 0, Mathf.Max(0, width - 1), Mathf.Max(0, height - 1), f)
            });
            n++;
        }
        return n;
    }

    void Flood(CityPixelFrame frame, bool[,] seen, int x, int y, ref int maxX, ref int maxY)
    {
        var stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(x, y));
        while (stack.Count > 0)
        {
            var p = stack.Pop();
            if (p.x < 0 || p.y < 0 || p.x >= width || p.y >= height) continue;
            if (seen[p.x, p.y] || frame.Get(p.x, p.y, width) == 0) continue;
            seen[p.x, p.y] = true;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
            stack.Push(new Vector2Int(p.x + 1, p.y));
            stack.Push(new Vector2Int(p.x - 1, p.y));
            stack.Push(new Vector2Int(p.x, p.y + 1));
            stack.Push(new Vector2Int(p.x, p.y - 1));
        }
    }
}
