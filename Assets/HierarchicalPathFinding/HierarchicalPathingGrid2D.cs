using System;
using UnityEngine;

/// <summary>
/// Simple 2D (XZ) occupancy grid used as a first-pass hierarchical pathing backend.
/// This is intentionally conservative: if a cell is even partially blocked, we mark it blocked.
/// When fit-to-terrain is used, per-cell heights are stored so paths and queries use terrain surface Y.
/// </summary>
public sealed class HierarchicalPathingGrid2D
{
    public readonly Bounds worldBounds;
    public readonly float cellSize;
    public readonly int width;
    public readonly int height;

    // Flattened [z * width + x]
    private readonly bool[] blocked;

    /// <summary>Per-cell world Y (terrain surface). Null when not using fit-to-terrain.</summary>
    private readonly float[] cellHeights;

    public HierarchicalPathingGrid2D(Bounds worldBounds, float cellSize, bool storeHeights = false)
    {
        this.worldBounds = worldBounds;
        this.cellSize = Mathf.Max(0.1f, cellSize);

        width = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.x / this.cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.z / this.cellSize));
        blocked = new bool[width * height];
        cellHeights = storeHeights ? new float[width * height] : null;
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

    /// <summary>Set world Y for a cell (used when fit-to-terrain is enabled).</summary>
    public void SetCellHeight(int x, int z, float worldY)
    {
        if (cellHeights == null || !IsInBounds(x, z)) return;
        cellHeights[Index(x, z)] = worldY;
    }

    /// <summary>Get world Y for a cell; returns defaultY when no per-cell height is stored.</summary>
    public float GetCellHeight(int x, int z, float defaultY)
    {
        if (cellHeights == null || !IsInBounds(x, z)) return defaultY;
        return cellHeights[Index(x, z)];
    }

    /// <summary>True if this grid has per-cell heights (fit-to-terrain).</summary>
    public bool HasCellHeights => cellHeights != null;

    public Vector3 CellCenterWorld(int x, int z, float defaultY)
    {
        Vector3 min = worldBounds.min;
        float cx = min.x + (x + 0.5f) * cellSize;
        float cz = min.z + (z + 0.5f) * cellSize;
        float y = GetCellHeight(x, z, defaultY);
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

