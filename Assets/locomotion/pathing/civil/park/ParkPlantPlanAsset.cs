using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ParkPlantCell
{
    public int x;
    public int y;
    public string plantSpeciesId;
    public Color color = Color.green;
    [Range(0f, 1f)] public float stage01;
}

[Serializable]
public sealed class ParkPlantPlacementSquare
{
    public string squareId;
    public Vector2Int originCell;
    public Vector2Int sizeCells = new Vector2Int(1, 1);
    public Vector3 worldPosition;
    public Quaternion worldRotation = Quaternion.identity;
    public Vector3 worldScale = Vector3.one;
    public string plantSpeciesId;
    public bool snapped = true;
}

[Serializable]
public sealed class ParkPlantTimeLayer
{
    public string layerId = "t0";
    public float timeSec;
    public List<ParkPlantCell> cells = new List<ParkPlantCell>();
    public List<ParkPlantPlacementSquare> placementSquares = new List<ParkPlantPlacementSquare>();
}

/// <summary>Serialized park plant grid for ParkPlantPlannerWindow.</summary>
[CreateAssetMenu(fileName = "ParkPlantPlan", menuName = "Locomotion/Civil/Park Plant Plan")]
public sealed class ParkPlantPlanAsset : ScriptableObject
{
    public int width = 32;
    public int height = 32;
    public float cellWorldSize = 0.5f;
    public Vector3 worldOrigin;
    public float plantMinSizeM = 0.5f;
    public List<string> plantSpeciesIds = new List<string> { "lot_grass", "oak", "flower_bed" };
    public List<ParkPlantTimeLayer> timeLayers = new List<ParkPlantTimeLayer>();

    public void EnsureDefaults()
    {
        if (cellWorldSize < plantMinSizeM)
            cellWorldSize = plantMinSizeM;
        if (timeLayers == null) timeLayers = new List<ParkPlantTimeLayer>();
        if (timeLayers.Count == 0)
            timeLayers.Add(new ParkPlantTimeLayer { layerId = "t0", timeSec = 0f });
        if (plantSpeciesIds == null || plantSpeciesIds.Count == 0)
            plantSpeciesIds = new List<string> { "lot_grass", "oak", "flower_bed" };
    }

    public ParkPlantTimeLayer GetLayer(int index)
    {
        EnsureDefaults();
        index = Mathf.Clamp(index, 0, timeLayers.Count - 1);
        return timeLayers[index];
    }

    public void SetCell(int layer, int x, int y, string speciesId, Color color, float stage01)
    {
        var L = GetLayer(layer);
        for (int i = 0; i < L.cells.Count; i++)
        {
            if (L.cells[i] != null && L.cells[i].x == x && L.cells[i].y == y)
            {
                L.cells[i].plantSpeciesId = speciesId;
                L.cells[i].color = color;
                L.cells[i].stage01 = stage01;
                return;
            }
        }
        L.cells.Add(new ParkPlantCell
        {
            x = x, y = y, plantSpeciesId = speciesId, color = color, stage01 = stage01
        });
    }

    public void ClearCell(int layer, int x, int y)
    {
        var L = GetLayer(layer);
        for (int i = L.cells.Count - 1; i >= 0; i--)
            if (L.cells[i] != null && L.cells[i].x == x && L.cells[i].y == y)
                L.cells.RemoveAt(i);
    }

    public ParkPlantCell GetCell(int layer, int x, int y)
    {
        var L = GetLayer(layer);
        for (int i = 0; i < L.cells.Count; i++)
            if (L.cells[i] != null && L.cells[i].x == x && L.cells[i].y == y)
                return L.cells[i];
        return null;
    }

    public ParkPlantPlacementSquare AddOrMovePlacementSquare(int layer, Vector2Int origin, string speciesId, Vector3 worldPos)
    {
        var L = GetLayer(layer);
        for (int i = 0; i < L.placementSquares.Count; i++)
        {
            var sq = L.placementSquares[i];
            if (sq == null) continue;
            if (sq.originCell == origin)
            {
                sq.worldPosition = worldPos;
                sq.plantSpeciesId = speciesId;
                sq.snapped = true;
                return sq;
            }
        }
        var created = new ParkPlantPlacementSquare
        {
            squareId = "sq_" + L.placementSquares.Count,
            originCell = origin,
            sizeCells = Vector2Int.one,
            worldPosition = worldPos,
            plantSpeciesId = speciesId,
            snapped = true
        };
        L.placementSquares.Add(created);
        return created;
    }

    public void FloodFill(int layer, int x, int y, string speciesId, Color color, float stage01)
    {
        EnsureDefaults();
        if (x < 0 || y < 0 || x >= width || y >= height) return;
        var target = GetCell(layer, x, y);
        string fromId = target?.plantSpeciesId;
        Color fromColor = target != null ? target.color : Color.clear;
        var stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(x, y));
        var seen = new HashSet<Vector2Int>();
        int guard = width * height + 8;
        while (stack.Count > 0 && guard-- > 0)
        {
            var p = stack.Pop();
            if (!seen.Add(p)) continue;
            if (p.x < 0 || p.y < 0 || p.x >= width || p.y >= height) continue;
            var c = GetCell(layer, p.x, p.y);
            bool match = c == null
                ? string.IsNullOrEmpty(fromId)
                : c.plantSpeciesId == fromId && ColorsClose(c.color, fromColor);
            if (!match) continue;
            SetCell(layer, p.x, p.y, speciesId, color, stage01);
            stack.Push(new Vector2Int(p.x + 1, p.y));
            stack.Push(new Vector2Int(p.x - 1, p.y));
            stack.Push(new Vector2Int(p.x, p.y + 1));
            stack.Push(new Vector2Int(p.x, p.y - 1));
        }
    }

    static bool ColorsClose(Color a, Color b) =>
        Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) < 0.05f;
}
