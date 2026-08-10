using System;
using System.Collections.Generic;
using UnityEngine;

public enum StreetBlocksLayerKind
{
    DeepSubsurface = 0,
    ShallowUtility = 1,
    StreetLevel = 2,
    Podium = 3,
    MidHighRise = 4,
    Skyscraper = 5,
    Airspace = 6
}

public enum StreetBlocksBrushKind
{
    Select = 0,
    TwoWayStreet = 1,
    Multilane = 2,
    OneWay = 3,
    Trash = 4,
    PhonePole = 5,
    Building = 6,
    Sewer = 7,
    DryWell = 8,
    Bioswale = 9,
    Eraser = 10
}

[Serializable]
public sealed class StreetBlocksCell
{
    public int x;
    public int y;
    public StreetBlocksBrushKind brush;
    public int laneCount = 2;
    public float oneWayYawDegrees;
    public string buildingTypeId;
    public float structureSizeM = 10f;
    public ScriptableObject phonePoleConfig;
    public bool show = true;
}

[Serializable]
public sealed class StreetBlocksLayer
{
    public string layerId;
    public StreetBlocksLayerKind kind;
    public float depthMinM;
    public float depthMaxM;
    public bool visible = true;
    public List<StreetBlocksCell> cells = new List<StreetBlocksCell>();
}

[Serializable]
public sealed class StreetBlocksLink
{
    public Vector2Int a;
    public Vector2Int b;
}

/// <summary>Street blocks pixel plan — depth layers + street auto-links.</summary>
[CreateAssetMenu(fileName = "StreetBlocksPlan", menuName = "Locomotion/Civil/Street Blocks Plan")]
public sealed class StreetBlocksPlanAsset : ScriptableObject
{
    public int width = 32;
    public int height = 32;
    public float cellWorldSize = 4f;
    public Vector3 worldOrigin;
    public List<StreetBlocksLayer> layers = new List<StreetBlocksLayer>();
    public List<StreetBlocksLink> streetLinks = new List<StreetBlocksLink>();
    public float trashMinZoom = 0.6f;
    public float phonePoleMinZoom = 0.5f;

    public void EnsureDefaultLayers()
    {
        if (layers != null && layers.Count > 0) return;
        layers = new List<StreetBlocksLayer>
        {
            Layer("deep", StreetBlocksLayerKind.DeepSubsurface, -50f, -10f),
            Layer("utility", StreetBlocksLayerKind.ShallowUtility, -5f, 0f),
            Layer("street", StreetBlocksLayerKind.StreetLevel, 0f, 0f),
            Layer("podium", StreetBlocksLayerKind.Podium, 15f, 20f),
            Layer("midrise", StreetBlocksLayerKind.MidHighRise, 20f, 150f),
            Layer("sky", StreetBlocksLayerKind.Skyscraper, 150f, 300f),
            Layer("air", StreetBlocksLayerKind.Airspace, 300f, 1000f)
        };
    }

    static StreetBlocksLayer Layer(string id, StreetBlocksLayerKind kind, float min, float max) =>
        new StreetBlocksLayer { layerId = id, kind = kind, depthMinM = min, depthMaxM = max };

    public StreetBlocksLayer GetLayer(int index)
    {
        EnsureDefaultLayers();
        return layers[Mathf.Clamp(index, 0, layers.Count - 1)];
    }

    public void SetCell(int layer, int x, int y, StreetBlocksCell cell)
    {
        var L = GetLayer(layer);
        for (int i = 0; i < L.cells.Count; i++)
        {
            if (L.cells[i] != null && L.cells[i].x == x && L.cells[i].y == y)
            {
                L.cells[i] = cell;
                return;
            }
        }
        cell.x = x;
        cell.y = y;
        L.cells.Add(cell);
    }

    public StreetBlocksCell GetCell(int layer, int x, int y)
    {
        var L = GetLayer(layer);
        for (int i = 0; i < L.cells.Count; i++)
            if (L.cells[i] != null && L.cells[i].x == x && L.cells[i].y == y)
                return L.cells[i];
        return null;
    }

    public void ClearCell(int layer, int x, int y)
    {
        var L = GetLayer(layer);
        for (int i = L.cells.Count - 1; i >= 0; i--)
            if (L.cells[i] != null && L.cells[i].x == x && L.cells[i].y == y)
                L.cells.RemoveAt(i);
    }

    /// <summary>Unforgiving overlap: keep largest structureSizeM when cells collide in show set.</summary>
    public List<StreetBlocksCell> RasterizeVisibleNoOverlap(int layer)
    {
        var L = GetLayer(layer);
        var list = new List<StreetBlocksCell>();
        for (int i = 0; i < L.cells.Count; i++)
            if (L.cells[i] != null && L.cells[i].show)
                list.Add(L.cells[i]);
        list.Sort((a, b) => b.structureSizeM.CompareTo(a.structureSizeM));
        var occupied = new HashSet<long>();
        var result = new List<StreetBlocksCell>();
        for (int i = 0; i < list.Count; i++)
        {
            long key = ((long)list[i].x << 32) ^ (uint)list[i].y;
            if (occupied.Contains(key)) continue;
            occupied.Add(key);
            result.Add(list[i]);
        }
        return result;
    }

    /// <summary>MST auto-connect for street-level street cells (Kruskal on grid neighbors + nearest).</summary>
    public int AutoConnectStreets()
    {
        EnsureDefaultLayers();
        int streetLayer = 2;
        for (int i = 0; i < layers.Count; i++)
            if (layers[i].kind == StreetBlocksLayerKind.StreetLevel)
                streetLayer = i;
        var L = GetLayer(streetLayer);
        var streetCells = new List<StreetBlocksCell>();
        for (int i = 0; i < L.cells.Count; i++)
        {
            var c = L.cells[i];
            if (c == null) continue;
            if (c.brush == StreetBlocksBrushKind.TwoWayStreet
                || c.brush == StreetBlocksBrushKind.Multilane
                || c.brush == StreetBlocksBrushKind.OneWay)
                streetCells.Add(c);
        }
        streetLinks.Clear();
        if (streetCells.Count < 2) return 0;

        // Union-find Kruskal on all pairs sorted by manhattan distance.
        var parent = new Dictionary<long, long>();
        long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
        long Find(long k)
        {
            if (!parent.ContainsKey(k)) parent[k] = k;
            if (parent[k] != k) parent[k] = Find(parent[k]);
            return parent[k];
        }
        void Union(long a, long b)
        {
            long ra = Find(a), rb = Find(b);
            if (ra != rb) parent[ra] = rb;
        }

        var edges = new List<(int i, int j, int dist)>();
        for (int i = 0; i < streetCells.Count; i++)
        for (int j = i + 1; j < streetCells.Count; j++)
        {
            int dist = Mathf.Abs(streetCells[i].x - streetCells[j].x)
                       + Mathf.Abs(streetCells[i].y - streetCells[j].y);
            edges.Add((i, j, dist));
        }
        edges.Sort((a, b) => a.dist.CompareTo(b.dist));
        int added = 0;
        for (int e = 0; e < edges.Count; e++)
        {
            var a = streetCells[edges[e].i];
            var b = streetCells[edges[e].j];
            long ka = Key(a.x, a.y);
            long kb = Key(b.x, b.y);
            if (Find(ka) == Find(kb)) continue;
            Union(ka, kb);
            streetLinks.Add(new StreetBlocksLink
            {
                a = new Vector2Int(a.x, a.y),
                b = new Vector2Int(b.x, b.y)
            });
            added++;
            if (added >= streetCells.Count - 1) break;
        }
        return added;
    }

    public void SeedSewerFromBuildings(SewerGraph graph)
    {
        if (graph == null) return;
        EnsureDefaultLayers();
        for (int li = 0; li < layers.Count; li++)
        {
            var L = layers[li];
            if (L == null) continue;
            for (int i = 0; i < L.cells.Count; i++)
            {
                var c = L.cells[i];
                if (c == null) continue;
                Vector3 world = worldOrigin + new Vector3(c.x * cellWorldSize, 0f, c.y * cellWorldSize);
                if (c.brush == StreetBlocksBrushKind.Building || c.brush == StreetBlocksBrushKind.Sewer)
                {
                    var go = new GameObject("sewer_seed_" + c.x + "_" + c.y);
                    go.transform.position = world;
                    graph.AddOrGetBuildingNode(go);
                }
                else if (c.brush == StreetBlocksBrushKind.DryWell || c.brush == StreetBlocksBrushKind.Bioswale)
                {
                    graph.nodes.Add(new SewerNode
                    {
                        nodeId = "runoff_" + c.x + "_" + c.y,
                        worldPosition = world,
                        isDryWell = c.brush == StreetBlocksBrushKind.DryWell
                    });
                }
            }
        }
        graph.EnsureFullyConnectedToPlant();
    }
}
