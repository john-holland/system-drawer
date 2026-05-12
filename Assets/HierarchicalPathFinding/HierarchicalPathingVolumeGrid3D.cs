using UnityEngine;

/// <summary>
/// Uniform 3D occupancy grid for volume pathfinding (axis-aligned cells).
/// </summary>
public sealed class HierarchicalPathingVolumeGrid3D
{
    public readonly Bounds worldBounds;
    public readonly float cellSize;
    public readonly int width;
    public readonly int height;
    public readonly int depth;

    readonly bool[] blocked;

    public HierarchicalPathingVolumeGrid3D(Bounds worldBounds, float cellSize)
    {
        this.worldBounds = worldBounds;
        this.cellSize = Mathf.Max(0.05f, cellSize);
        width = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.x / this.cellSize));
        height = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.y / this.cellSize));
        depth = Mathf.Max(1, Mathf.CeilToInt(worldBounds.size.z / this.cellSize));
        blocked = new bool[width * height * depth];
    }

    public int Index(int x, int y, int z) => (z * height + y) * width + x;

    public bool IsInBounds(int x, int y, int z) =>
        x >= 0 && y >= 0 && z >= 0 && x < width && y < height && z < depth;

    public bool IsBlocked(int x, int y, int z)
    {
        if (!IsInBounds(x, y, z)) return true;
        return blocked[Index(x, y, z)];
    }

    public void SetBlocked(int x, int y, int z, bool value)
    {
        if (!IsInBounds(x, y, z)) return;
        blocked[Index(x, y, z)] = value;
    }

    public Vector3 CellCenterWorld(int x, int y, int z)
    {
        Vector3 min = worldBounds.min;
        float cx = min.x + (x + 0.5f) * cellSize;
        float cy = min.y + (y + 0.5f) * cellSize;
        float cz = min.z + (z + 0.5f) * cellSize;
        return new Vector3(cx, cy, cz);
    }

    public bool TryWorldToCell(Vector3 worldPos, out int x, out int y, out int z)
    {
        Vector3 min = worldBounds.min;
        float lx = worldPos.x - min.x;
        float ly = worldPos.y - min.y;
        float lz = worldPos.z - min.z;
        x = Mathf.FloorToInt(lx / cellSize);
        y = Mathf.FloorToInt(ly / cellSize);
        z = Mathf.FloorToInt(lz / cellSize);
        return IsInBounds(x, y, z);
    }
}
