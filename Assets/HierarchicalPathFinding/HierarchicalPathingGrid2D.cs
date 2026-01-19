using System;
using UnityEngine;

/// <summary>
/// Simple 2D (XZ) occupancy grid used as a first-pass hierarchical pathing backend.
/// This is intentionally conservative: if a cell is even partially blocked, we mark it blocked.
/// </summary>
public sealed class HierarchicalPathingGrid2D
{
    public readonly Bounds worldBounds;
    public readonly float cellSize;
    public readonly int width;
    public readonly int height;

    // Flattened [z * width + x]
    private readonly bool[] blocked;

    public HierarchicalPathingGrid2D(Bounds worldBounds, float cellSize)
    {
        this.worldBounds = worldBounds;
        this.cellSize = Mathf.Max(0.1f, cellSize);

        width = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.x / this.cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.z / this.cellSize));
        blocked = new bool[width * height];
    }

    public int Index(int x, int z) => z * width + x;

    public bool IsInBounds(int x, int z) => x >= 0 && z >= 0 && x < width && z < height;

    public bool IsBlocked(int x, int z)
    {
        if (!IsInBounds(x, z)) return true;
        return blocked[Index(x, z)];
    }

    public void SetBlocked(int x, int z, bool value)
    {
        if (!IsInBounds(x, z)) return;
        blocked[Index(x, z)] = value;
    }

    public Vector3 CellCenterWorld(int x, int z, float y)
    {
        Vector3 min = worldBounds.min;
        float cx = min.x + (x + 0.5f) * cellSize;
        float cz = min.z + (z + 0.5f) * cellSize;
        return new Vector3(cx, y, cz);
    }

    public bool TryWorldToCell(Vector3 worldPos, out int x, out int z)
    {
        Vector3 min = worldBounds.min;
        float lx = worldPos.x - min.x;
        float lz = worldPos.z - min.z;
        x = Mathf.FloorToInt(lx / cellSize);
        z = Mathf.FloorToInt(lz / cellSize);
        return IsInBounds(x, z);
    }
}

