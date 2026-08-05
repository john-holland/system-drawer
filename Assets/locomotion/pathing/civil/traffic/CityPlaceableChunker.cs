using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CityPlaceableChunkCell
{
    public int cellX;
    public int cellY;
    public string typeKey;
    public int floorIndex;
    public string zoneId;
    public CityPixelBrushStamp stamp;
}

[Serializable]
public sealed class CityPlaceableChunk
{
    public CityPlaceableKind placeableKind;
    public string typeKey;
    public bool isSharedShell;
    public List<string> tenantTypeKeys = new List<string>();
    public List<CityPlaceableChunkCell> cells = new List<CityPlaceableChunkCell>();
    public int minX, minY, maxX, maxY;
    public int heightCells = 1;
    public string forcedCandidateId;
    public FloorPlanIndexMap floorPlanOverride;
    public GameObject forcedPrefab;
}

public sealed class CityPlaceableChunkResult
{
    public List<CityPlaceableChunk> chunks = new List<CityPlaceableChunk>();
    public bool incompleteSeparation;
    public List<string> warnings = new List<string>();
}

/// <summary>Clusters scalable brush stamps with type separators and optional shared-building merge.</summary>
public static class CityPlaceableChunker
{
    public static readonly CityPixelBrushKind[] ScalableKinds =
    {
        CityPixelBrushKind.Building,
        CityPixelBrushKind.Intersection,
        CityPixelBrushKind.SchoolBusStop
    };

    public static bool IsScalable(CityPixelBrushKind kind)
    {
        for (int i = 0; i < ScalableKinds.Length; i++)
            if (ScalableKinds[i] == kind) return true;
        return false;
    }

    public static bool IsSeparator(CityPixelBrushKind kind) =>
        kind == CityPixelBrushKind.BuildingTypeSeparator
        || kind == CityPixelBrushKind.IntersectionTypeSeparator
        || kind == CityPixelBrushKind.PlaceableTypeSeparator;

    public static CityPixelBrushKind SeparatorFor(CityPlaceableKind kind)
    {
        switch (kind)
        {
            case CityPlaceableKind.Building: return CityPixelBrushKind.BuildingTypeSeparator;
            case CityPlaceableKind.Intersection: return CityPixelBrushKind.IntersectionTypeSeparator;
            default: return CityPixelBrushKind.PlaceableTypeSeparator;
        }
    }

    public static CityPlaceableKind ToPlaceableKind(CityPixelBrushKind kind)
    {
        switch (kind)
        {
            case CityPixelBrushKind.Building: return CityPlaceableKind.Building;
            case CityPixelBrushKind.Intersection: return CityPlaceableKind.Intersection;
            case CityPixelBrushKind.SchoolBusStop: return CityPlaceableKind.SchoolBusStop;
            default: return CityPlaceableKind.Custom;
        }
    }

    public static string ResolveTypeKey(CityPixelBrushStamp s)
    {
        if (s == null) return "Generic";
        if (!string.IsNullOrEmpty(s.typeKey)) return s.typeKey;
        if (s.kind == CityPixelBrushKind.Building)
        {
            if (!string.IsNullOrEmpty(s.buildingTypeId)) return s.buildingTypeId;
            return s.buildingKind.ToString();
        }
        if (s.kind == CityPixelBrushKind.Intersection) return "intersection";
        if (s.kind == CityPixelBrushKind.SchoolBusStop) return "school_bus_stop";
        return s.kind.ToString();
    }

    public static CityPlaceableChunkResult ChunkFrame(
        CityPixelGrid grid,
        int frameIndex,
        CityPlaceableCatalog catalog)
    {
        var result = new CityPlaceableChunkResult();
        if (grid?.brushStamps == null) return result;

        var scalable = new List<CityPixelBrushStamp>();
        var separators = new HashSet<long>();
        for (int i = 0; i < grid.brushStamps.Count; i++)
        {
            var s = grid.brushStamps[i];
            if (s == null || s.frameIndex != frameIndex) continue;
            if (IsSeparator(s.kind))
            {
                separators.Add(Pack(s.cellX, s.cellY));
                continue;
            }
            if (IsScalable(s.kind))
                scalable.Add(s);
        }

        // Group by placeable kind + typeKey for initial components
        var groups = new Dictionary<string, List<CityPixelBrushStamp>>();
        for (int i = 0; i < scalable.Count; i++)
        {
            var s = scalable[i];
            string key = ToPlaceableKind(s.kind) + "|" + ResolveTypeKey(s);
            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<CityPixelBrushStamp>();
                groups[key] = list;
            }
            list.Add(s);
        }

        var singleChunks = new List<CityPlaceableChunk>();
        foreach (var kv in groups)
        {
            var components = ConnectedComponents(kv.Value, separators, SeparatorFor(ToPlaceableKind(kv.Value[0].kind)), result);
            for (int c = 0; c < components.Count; c++)
                singleChunks.Add(BuildChunk(components[c], isShared: false));
        }

        // Shared-shell merge across different typeKeys (buildings only)
        result.chunks = MergeSharedShells(singleChunks, catalog, result);
        return result;
    }

    static List<List<CityPixelBrushStamp>> ConnectedComponents(
        List<CityPixelBrushStamp> stamps,
        HashSet<long> separators,
        CityPixelBrushKind separatorKind,
        CityPlaceableChunkResult result)
    {
        var byCell = new Dictionary<long, CityPixelBrushStamp>();
        for (int i = 0; i < stamps.Count; i++)
            byCell[Pack(stamps[i].cellX, stamps[i].cellY)] = stamps[i];

        var visited = new HashSet<long>();
        var comps = new List<List<CityPixelBrushStamp>>();
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };

        foreach (var kv in byCell)
        {
            if (visited.Contains(kv.Key)) continue;
            var comp = new List<CityPixelBrushStamp>();
            var q = new Queue<long>();
            q.Enqueue(kv.Key);
            visited.Add(kv.Key);
            while (q.Count > 0)
            {
                long cur = q.Dequeue();
                Unpack(cur, out int cx, out int cy);
                comp.Add(byCell[cur]);
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d];
                    int ny = cy + dy[d];
                    long nk = Pack(nx, ny);
                    if (!byCell.ContainsKey(nk) || visited.Contains(nk)) continue;
                    // Separator wall cells occupy intervening pixels; also cut edges when a
                    // separator is adjacent to both endpoints (incomplete cuts still merge).
                    if (EdgeBlockedBySeparator(cx, cy, nx, ny, separators))
                        continue;
                    visited.Add(nk);
                    q.Enqueue(nk);
                }
            }
            comps.Add(comp);
        }

        // Incomplete separation: separators touch the group but did not split connectivity.
        if (separators.Count > 0 && stamps.Count > 1
            && comps.Count == 1
            && HasSeparatorAdjacentToGroup(stamps, separators))
        {
            int cutComps = CountComponentsWithEdgeCuts(stamps, separators);
            int openComps = CountComponentsIgnoringSeparators(stamps);
            if (cutComps == openComps)
            {
                result.incompleteSeparation = true;
                result.warnings.Add(
                    $"Incomplete {separatorKind} separation at frame stamps near ({stamps[0].cellX},{stamps[0].cellY}): chunks still connected.");
                Debug.LogWarning(result.warnings[result.warnings.Count - 1]);
            }
        }

        return comps;
    }

    static bool HasSeparatorAdjacentToGroup(List<CityPixelBrushStamp> stamps, HashSet<long> separators)
    {
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        for (int i = 0; i < stamps.Count; i++)
        {
            for (int d = 0; d < 4; d++)
            {
                if (separators.Contains(Pack(stamps[i].cellX + dx[d], stamps[i].cellY + dy[d])))
                    return true;
            }
        }
        return false;
    }

    static int CountComponentsIgnoringSeparators(List<CityPixelBrushStamp> stamps) =>
        CountComponentsWithEdgeCuts(stamps, null);

    static int CountComponentsWithEdgeCuts(List<CityPixelBrushStamp> stamps, HashSet<long> separators)
    {
        var byCell = new Dictionary<long, CityPixelBrushStamp>();
        for (int i = 0; i < stamps.Count; i++)
            byCell[Pack(stamps[i].cellX, stamps[i].cellY)] = stamps[i];
        var visited = new HashSet<long>();
        int comps = 0;
        int[] dx = { 1, -1, 0, 0 };
        int[] dy = { 0, 0, 1, -1 };
        foreach (var kv in byCell)
        {
            if (visited.Contains(kv.Key)) continue;
            comps++;
            var q = new Queue<long>();
            q.Enqueue(kv.Key);
            visited.Add(kv.Key);
            while (q.Count > 0)
            {
                Unpack(q.Dequeue(), out int cx, out int cy);
                for (int d = 0; d < 4; d++)
                {
                    int nx = cx + dx[d];
                    int ny = cy + dy[d];
                    long nk = Pack(nx, ny);
                    if (!byCell.ContainsKey(nk) || visited.Contains(nk)) continue;
                    if (separators != null && EdgeBlockedBySeparator(cx, cy, nx, ny, separators))
                        continue;
                    visited.Add(nk);
                    q.Enqueue(nk);
                }
            }
        }
        return comps;
    }

    /// <summary>
    /// Edge cut when a separator wall cell lies on the shared edge, or when a separator
    /// is 4-adjacent to the edge (painted beside the join). Wall cells between stamps
    /// already prevent adjacency by occupying intervening cells.
    /// </summary>
    static bool EdgeBlockedBySeparator(int x0, int y0, int x1, int y1, HashSet<long> separators)
    {
        if (separators == null || separators.Count == 0) return false;
        // Separator painted on either stamp cell (overlay replace) cuts all its edges.
        if (separators.Contains(Pack(x0, y0)) || separators.Contains(Pack(x1, y1)))
            return true;
        // Full side-row of separators beside an orthogonal edge counts as a cut.
        if ((x0 == x1 && Mathf.Abs(y0 - y1) == 1) || (y0 == y1 && Mathf.Abs(x0 - x1) == 1))
        {
            int sx = x1 - x0;
            int sy = y1 - y0;
            int lx = -sy;
            int ly = sx;
            if (separators.Contains(Pack(x0 + lx, y0 + ly)) && separators.Contains(Pack(x1 + lx, y1 + ly)))
                return true;
            if (separators.Contains(Pack(x0 - lx, y0 - ly)) && separators.Contains(Pack(x1 - lx, y1 - ly)))
                return true;
        }
        return false;
    }

    static CityPlaceableChunk BuildChunk(List<CityPixelBrushStamp> stamps, bool isShared)
    {
        var chunk = new CityPlaceableChunk
        {
            placeableKind = ToPlaceableKind(stamps[0].kind),
            typeKey = ResolveTypeKey(stamps[0]),
            isSharedShell = isShared,
            minX = int.MaxValue,
            minY = int.MaxValue,
            maxX = int.MinValue,
            maxY = int.MinValue,
            heightCells = 1
        };
        var tenants = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < stamps.Count; i++)
        {
            var s = stamps[i];
            string tk = ResolveTypeKey(s);
            tenants.Add(tk);
            chunk.cells.Add(new CityPlaceableChunkCell
            {
                cellX = s.cellX,
                cellY = s.cellY,
                typeKey = tk,
                floorIndex = Mathf.Max(0, s.floorIndex),
                zoneId = s.zoneId,
                stamp = s
            });
            chunk.minX = Mathf.Min(chunk.minX, s.cellX);
            chunk.minY = Mathf.Min(chunk.minY, s.cellY);
            chunk.maxX = Mathf.Max(chunk.maxX, s.cellX);
            chunk.maxY = Mathf.Max(chunk.maxY, s.cellY);
            chunk.heightCells = Mathf.Max(chunk.heightCells, Mathf.Max(1, s.heightCells));
            if (!string.IsNullOrEmpty(s.candidateId) && string.IsNullOrEmpty(chunk.forcedCandidateId))
                chunk.forcedCandidateId = s.candidateId;
            if (s.signPrefab != null && chunk.forcedPrefab == null)
                chunk.forcedPrefab = s.signPrefab;
            if (s.floorPlanIndexMap != null && chunk.floorPlanOverride == null)
                chunk.floorPlanOverride = s.floorPlanIndexMap;
        }
        chunk.tenantTypeKeys = new List<string>(tenants);
        if (chunk.tenantTypeKeys.Count > 1)
            chunk.isSharedShell = true;
        return chunk;
    }

    static List<CityPlaceableChunk> MergeSharedShells(
        List<CityPlaceableChunk> input,
        CityPlaceableCatalog catalog,
        CityPlaceableChunkResult result)
    {
        if (catalog == null || input.Count < 2) return input;

        var building = new List<CityPlaceableChunk>();
        var other = new List<CityPlaceableChunk>();
        for (int i = 0; i < input.Count; i++)
        {
            if (input[i].placeableKind == CityPlaceableKind.Building)
                building.Add(input[i]);
            else
                other.Add(input[i]);
        }

        bool mergedAny = true;
        while (mergedAny)
        {
            mergedAny = false;
            for (int i = 0; i < building.Count && !mergedAny; i++)
            for (int j = i + 1; j < building.Count && !mergedAny; j++)
            {
                var a = building[i];
                var b = building[j];
                if (!AabbTouchesOrStacks(a, b)) continue;

                int minX = Mathf.Min(a.minX, b.minX);
                int minY = Mathf.Min(a.minY, b.minY);
                int maxX = Mathf.Max(a.maxX, b.maxX);
                int maxY = Mathf.Max(a.maxY, b.maxY);
                int h = Mathf.Max(a.heightCells, b.heightCells);
                int needW = maxX - minX + 1;
                int needD = maxY - minY + 1;

                var sharedCands = catalog.FindMatching(CityPlaceableKind.Building, null, sharedShell: true);
                CityPlaceableCandidate ok = null;
                for (int c = 0; c < sharedCands.Count; c++)
                {
                    var cand = sharedCands[c];
                    if (!cand.sg3dSharedBuildingCompatible && cand.sg3dPromptComposition != null)
                        continue;
                    if (!cand.Fits(needW, needD, h)) continue;
                    bool tenantsOk = true;
                    for (int t = 0; t < a.tenantTypeKeys.Count; t++)
                        if (!cand.AllowsTenant(a.tenantTypeKeys[t])) tenantsOk = false;
                    for (int t = 0; t < b.tenantTypeKeys.Count; t++)
                        if (!cand.AllowsTenant(b.tenantTypeKeys[t])) tenantsOk = false;
                    if (!tenantsOk) continue;
                    ok = cand;
                    break;
                }

                if (ok == null)
                {
                    result.warnings.Add(
                        $"Shared-building merge skipped for {a.typeKey}+{b.typeKey}: no compatible catalog candidate.");
                    continue;
                }

                var merged = new CityPlaceableChunk
                {
                    placeableKind = CityPlaceableKind.Building,
                    typeKey = "shared_building",
                    isSharedShell = true,
                    minX = minX,
                    minY = minY,
                    maxX = maxX,
                    maxY = maxY,
                    heightCells = h,
                    forcedCandidateId = ok.id,
                    floorPlanOverride = a.floorPlanOverride != null ? a.floorPlanOverride : b.floorPlanOverride
                };
                merged.cells.AddRange(a.cells);
                merged.cells.AddRange(b.cells);
                merged.tenantTypeKeys = new List<string>();
                var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for (int t = 0; t < a.tenantTypeKeys.Count; t++) set.Add(a.tenantTypeKeys[t]);
                for (int t = 0; t < b.tenantTypeKeys.Count; t++) set.Add(b.tenantTypeKeys[t]);
                merged.tenantTypeKeys.AddRange(set);

                building.RemoveAt(j);
                building.RemoveAt(i);
                building.Add(merged);
                mergedAny = true;
            }
        }

        other.AddRange(building);
        return other;
    }

    static bool AabbTouchesOrStacks(CityPlaceableChunk a, CityPlaceableChunk b)
    {
        bool xzOverlap = !(a.maxX < b.minX - 1 || b.maxX < a.minX - 1 || a.maxY < b.minY - 1 || b.maxY < a.minY - 1);
        if (xzOverlap) return true;
        // Vertical stack: same XZ footprint, different floor indices
        bool sameFootprint = a.minX == b.minX && a.maxX == b.maxX && a.minY == b.minY && a.maxY == b.maxY;
        return sameFootprint;
    }

    static long Pack(int x, int y) => ((long)x << 32) ^ (uint)y;

    static void Unpack(long k, out int x, out int y)
    {
        x = (int)(k >> 32);
        y = (int)(k & 0xffffffff);
    }
}
