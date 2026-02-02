using System;
using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;

/// <summary>
/// Order in which (region, time_window) slots are chosen for narrative/calendar placement.
/// </summary>
public enum TemporalPlacementStrategy
{
    Chronological,
    ReverseChronological,
    HeadTailAlternate,
    DivisionByX,
    PriorityQueue
}

/// <summary>
/// Fourth-dimensional spatial generator: places and queries (x,y,z,t) using time-sliced 3D OctTrees.
/// Use for narrative/calendar volumes; temporal strategy and buffer/padding applied in Temporal Placement.
/// </summary>
public class SpatialGenerator4D : SpatialGeneratorBase
{
    [Header("4D Bounds")]
    [Tooltip("Spatial bounds in local/world space (center and size).")]
    public Bounds spatialBounds = new Bounds(Vector3.zero, Vector3.one * 100f);
    [Tooltip("Time range [tMin, tMax] in narrative seconds.")]
    public float tMin = 0f;
    public float tMax = 3600f;
    [Tooltip("Number of time slices; each slice has its own 3D OctTree.")]
    public int sliceCount = 32;

    [Header("Temporal Strategy")]
    [Tooltip("When true, use temporalStrategy for placement order; when false, always chronological.")]
    public bool useTemporalStrategy = true;
    [Tooltip("When true, apply scheduleBuffer and schedulePadding; when false, ignore them.")]
    public bool useBufferPadding = true;
    [Tooltip("Order in which time slots are chosen when placing events.")]
    public TemporalPlacementStrategy temporalStrategy = TemporalPlacementStrategy.Chronological;
    [Tooltip("Minimum gap (narrative seconds) between end of one event's window and start of the next when placing/validating.")]
    public float scheduleBuffer = 0f;
    [Tooltip("Inset from nominal start/end; event is 'active' from start+padding to end-padding.")]
    public float schedulePadding = 0f;

    [Header("OctTree (per slice)")]
    public int maxObjectsPerNode = 8;
    public int maxDepth = 8;
    public float minCellSize = 0f;

    [Header("4D Grid & SDF")]
    [Tooltip("When true, build a 4D grid from placed volumes and register Sample4D query API.")]
    public bool buildGrid = false;
    [Tooltip("Grid resolution (x, y, z, t). Lower = faster, coarser.")]
    public int gridResX = 16;
    public int gridResY = 16;
    public int gridResZ = 16;
    public int gridResT = 32;

    [Header("Editor Gizmos / Slice")]
    [Tooltip("Draw 3D slice of 4D occupancy at editorSliceT when selected.")]
    public bool showGizmoSlice = true;
    [Tooltip("Time (narrative seconds) for the 3D slice.")]
    public float editorSliceT = 0f;
    [Tooltip("Stride for slice cells (1 = every cell; 2 = half resolution).")]
    public int gizmoSliceStride = 2;
    [Tooltip("Color for occupied cells; alpha scales with occupancy.")]
    public Color gizmoOccupancyColor = new Color(0.2f, 0.6f, 1f, 0.4f);
    [Tooltip("Color for causal gradient (depth); alpha scales with causal value.")]
    public Color gizmoCausalColor = new Color(1f, 0.5f, 0f, 0.3f);

    [Header("Emergence Visualization")]
    [Tooltip("Draw layered ventricular SDF max: multiple time slices with per-layer opacity (older = more transparent).")]
    public bool showEmergenceViz = false;
    [Tooltip("Number of time layers to draw (spaced between tMin and tMax).")]
    public int emergenceLayerCount = 8;
    [Tooltip("Opacity of the first (oldest) layer.")]
    [Range(0f, 1f)]
    public float emergenceOpacityOld = 0.12f;
    [Tooltip("Opacity of the last (newest) layer.")]
    [Range(0f, 1f)]
    public float emergenceOpacityNew = 0.4f;
    [Tooltip("Color tint for emergence layers (alpha is overridden per layer).")]
    public Color emergenceColor = new Color(0.3f, 0.7f, 1f, 0.5f);

    private List<SGOctTree> slices = new List<SGOctTree>();
    private Dictionary<GameObject, SpatiotemporalEntry> entryByGo = new Dictionary<GameObject, SpatiotemporalEntry>();
    private Transform markerParent;
    private bool initialized;
    private NarrativeVolumeGrid4D grid4D;

    private struct SpatiotemporalEntry
    {
        public Bounds4 volume;
        public object payload;

        public SpatiotemporalEntry(Bounds4 volume, object payload)
        {
            this.volume = volume;
            this.payload = payload;
        }
    }

    /// <inheritdoc />
    public override Bounds GetSpatialBounds()
    {
        Vector3 worldCenter = transform.TransformPoint(spatialBounds.center);
        Vector3 worldSize = Vector3.Scale(spatialBounds.size, transform.lossyScale);
        return new Bounds(worldCenter, worldSize);
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        NarrativeVolumeQuery.IsInsideNarrativeVolumeImpl = (position, t) =>
            Overlaps(new Bounds(position, Vector3.zero), t);
        NarrativeVolumeQuery.IsEventActiveAtImpl = (tStart, tEnd, t) => IsActiveAt(tStart, tEnd, t);
        if (buildGrid && grid4D != null && grid4D.IsBuilt)
            NarrativeVolumeQuery.Sample4DImpl = (position, t) =>
            {
                grid4D.Sample4D(position, t, out float occ, out float causal);
                return (occ, causal);
            };
    }

    private void OnDisable()
    {
        NarrativeVolumeQuery.IsInsideNarrativeVolumeImpl = null;
        NarrativeVolumeQuery.IsEventActiveAtImpl = null;
        NarrativeVolumeQuery.Sample4DImpl = null;
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        float duration = Mathf.Max(tMax - tMin, 0.001f);
        int count = Mathf.Max(1, sliceCount);
        slices.Clear();
        for (int i = 0; i < count; i++)
        {
            var tree = new SGOctTree(spatialBounds, maxObjectsPerNode, maxDepth, minCellSize);
            slices.Add(tree);
        }

        if (markerParent == null)
        {
            var go = new GameObject("SpatialGenerator4D_Markers");
            go.transform.SetParent(transform);
            go.hideFlags = HideFlags.HideAndDontSave;
            markerParent = go.transform;
        }

        initialized = true;
    }

    /// <summary>Map time t to slice index [0, sliceCount-1].</summary>
    public int TimeToSliceIndex(float t)
    {
        if (slices.Count == 0)
            return 0;
        float duration = Mathf.Max(tMax - tMin, 0.001f);
        float u = Mathf.Clamp01((t - tMin) / duration);
        int idx = Mathf.FloorToInt(u * slices.Count);
        return Mathf.Clamp(idx, 0, slices.Count - 1);
    }

    /// <summary>Get slice time range [tStart, tEnd] for slice index.</summary>
    public void SliceIndexToTimeRange(int sliceIndex, out float tStart, out float tEnd)
    {
        float duration = Mathf.Max(tMax - tMin, 0.001f);
        int count = Mathf.Max(1, slices.Count);
        float step = duration / count;
        tStart = tMin + sliceIndex * step;
        tEnd = (sliceIndex + 1 < count) ? tMin + (sliceIndex + 1) * step : tMax;
    }

    /// <summary>Insert a 4D volume; payload is stored for query. Returns true if inserted into at least one slice.</summary>
    public bool Insert(Bounds4 volume, object payload)
    {
        EnsureInitialized();
        if (slices.Count == 0)
            return false;

        Bounds bounds3 = volume.ToBounds();
        float duration = Mathf.Max(tMax - tMin, 0.001f);
        float step = duration / slices.Count;
        int startSlice = TimeToSliceIndex(volume.tMin);
        int endSlice = TimeToSliceIndex(volume.tMax);
        if (startSlice > endSlice)
            endSlice = startSlice;

        GameObject marker = new GameObject("NarrativeVolume");
        marker.transform.SetParent(markerParent);
        marker.transform.position = volume.center;
        marker.hideFlags = HideFlags.HideAndDontSave;

        for (int i = startSlice; i <= endSlice; i++)
            slices[i].Insert(bounds3, marker, payload);

        entryByGo[marker] = new SpatiotemporalEntry(volume, payload);
        if (buildGrid)
            BuildGrid();
        return true;
    }

    /// <summary>Search for objects overlapping (region, t). Returns GameObjects (markers); use GetPayload to get the stored payload.</summary>
    public List<GameObject> Search(Bounds region, float t)
    {
        EnsureInitialized();
        if (slices.Count == 0)
            return new List<GameObject>();

        int idx = TimeToSliceIndex(t);
        return slices[idx].Search(region);
    }

    /// <summary>Get the payload (and volume) for a marker GameObject returned by Search.</summary>
    public bool TryGetEntry(GameObject marker, out Bounds4 volume, out object payload)
    {
        if (entryByGo != null && marker != null && entryByGo.TryGetValue(marker, out SpatiotemporalEntry e))
        {
            volume = e.volume;
            payload = e.payload;
            return true;
        }
        volume = default;
        payload = null;
        return false;
    }

    /// <summary>True if (region, t) overlaps any placed volume.</summary>
    public bool Overlaps(Bounds region, float t)
    {
        var list = Search(region, t);
        return list != null && list.Count > 0;
    }

    /// <summary>Padded active window: (tStart + padding, tEnd - padding). Use for "is event active at t?". Respects useBufferPadding.</summary>
    public void GetActiveWindow(float tStart, float tEnd, out float activeStart, out float activeEnd)
    {
        if (!useBufferPadding)
        {
            activeStart = tStart;
            activeEnd = tEnd;
            return;
        }
        float pad = Mathf.Max(0f, schedulePadding);
        activeStart = tStart + pad;
        activeEnd = tEnd - pad;
        if (activeEnd < activeStart)
            activeEnd = activeStart;
    }

    /// <summary>True if t is inside the event's active window (with schedule padding).</summary>
    public bool IsActiveAt(float tStart, float tEnd, float t)
    {
        GetActiveWindow(tStart, tEnd, out float activeStart, out float activeEnd);
        return t >= activeStart && t <= activeEnd;
    }

    /// <summary>True if new window respects schedule buffer after previous end.</summary>
    public bool ValidateWindow(float previousEnd, float newStart, float newEnd)
    {
        return newStart >= previousEnd + scheduleBuffer && newEnd > newStart;
    }

    /// <summary>Returns indices into windows sorted by temporal strategy (for placement order).</summary>
    public List<int> SortWindowIndicesByStrategy(List<(float tStart, float tEnd)> windows)
    {
        if (windows == null || windows.Count == 0)
            return new List<int>();
        var indices = new List<int>(windows.Count);
        for (int i = 0; i < windows.Count; i++)
            indices.Add(i);
        if (!useTemporalStrategy)
        {
            indices.Sort((a, b) => windows[a].tStart.CompareTo(windows[b].tStart));
            return indices;
        }
        switch (temporalStrategy)
        {
            case TemporalPlacementStrategy.Chronological:
                indices.Sort((a, b) => windows[a].tStart.CompareTo(windows[b].tStart));
                break;
            case TemporalPlacementStrategy.ReverseChronological:
                indices.Sort((a, b) => windows[b].tStart.CompareTo(windows[a].tStart));
                break;
            case TemporalPlacementStrategy.HeadTailAlternate:
                indices.Sort((a, b) => windows[a].tStart.CompareTo(windows[b].tStart));
                var alt = new List<int>(indices.Count);
                int lo = 0, hi = indices.Count - 1;
                while (lo <= hi)
                {
                    alt.Add(indices[lo]);
                    if (lo != hi) alt.Add(indices[hi]);
                    lo++; hi--;
                }
                indices = alt;
                break;
            case TemporalPlacementStrategy.DivisionByX:
                indices.Sort((a, b) => windows[a].tStart.CompareTo(windows[b].tStart));
                break;
            case TemporalPlacementStrategy.PriorityQueue:
            default:
                indices.Sort((a, b) => windows[a].tStart.CompareTo(windows[b].tStart));
                break;
        }
        return indices;
    }

    public void Clear()
    {
        if (entryByGo != null)
        {
            foreach (var go in entryByGo.Keys)
            {
                if (go != null)
                    Destroy(go);
            }
            entryByGo.Clear();
        }

        if (slices != null)
        {
            for (int i = 0; i < slices.Count; i++)
                slices[i].Clear();
        }

        initialized = false;
        EnsureInitialized();
        if (buildGrid)
            BuildGrid();
    }

    /// <summary>Collect all placed Bounds4 volumes (for grid build).</summary>
    public List<Bounds4> GetPlacedVolumes()
    {
        var list = new List<Bounds4>(entryByGo != null ? entryByGo.Count : 0);
        if (entryByGo == null) return list;
        foreach (var e in entryByGo.Values)
            list.Add(e.volume);
        return list;
    }

    /// <summary>Build 4D grid from placed volumes when buildGrid is true. Registers Sample4D on next OnEnable.</summary>
    public void BuildGrid()
    {
        if (!buildGrid) return;
        var volumes = GetPlacedVolumes();
        if (grid4D == null)
            grid4D = new NarrativeVolumeGrid4D();
        grid4D.Configure(spatialBounds, tMin, tMax, gridResX, gridResY, gridResZ, gridResT);
        grid4D.BuildFromVolumes(volumes);
        if (isActiveAndEnabled)
        {
            NarrativeVolumeQuery.Sample4DImpl = (position, t) =>
            {
                grid4D.Sample4D(position, t, out float occ, out float causal);
                return (occ, causal);
            };
        }
    }

    /// <summary>Get 3D slice at time t for gizmo/editor. Returns true if grid is built; occupancy and causal are allocated and filled.</summary>
    public bool TryGetSliceAtT(float t, out Bounds bounds, out int sliceResX, out int sliceResY, out int sliceResZ, out float[] occupancy, out float[] causal)
    {
        bounds = spatialBounds;
        sliceResX = sliceResY = sliceResZ = 0;
        occupancy = null;
        causal = null;
        if (grid4D == null || !grid4D.IsBuilt)
            return false;
        sliceResX = grid4D.ResX;
        sliceResY = grid4D.ResY;
        sliceResZ = grid4D.ResZ;
        int count = sliceResX * sliceResY * sliceResZ;
        occupancy = new float[count];
        causal = new float[count];
        grid4D.GetSliceAtT(t, occupancy, causal);
        bounds = grid4D.SpatialBounds;
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (showEmergenceViz && grid4D != null && grid4D.IsBuilt)
            DrawEmergenceLayers();

        if (!showGizmoSlice || !TryGetSliceAtT(editorSliceT, out Bounds bounds, out int rx, out int ry, out int rz, out float[] occ, out float[] causal))
            return;
        int stride = Mathf.Max(1, gizmoSliceStride);
        float cellSizeX = bounds.size.x / rx;
        float cellSizeY = bounds.size.y / ry;
        float cellSizeZ = bounds.size.z / rz;
        Vector3 origin = bounds.min;
        for (int iz = 0; iz < rz; iz += stride)
        {
            for (int iy = 0; iy < ry; iy += stride)
            {
                for (int ix = 0; ix < rx; ix += stride)
                {
                    int idx = ix + rx * (iy + ry * iz);
                    float o = occ[idx];
                    float c = causal[idx];
                    if (o <= 0f && c <= 0f) continue;
                    Vector3 center = origin + new Vector3((ix + 0.5f) * cellSizeX, (iy + 0.5f) * cellSizeY, (iz + 0.5f) * cellSizeZ);
                    Vector3 size = new Vector3(cellSizeX * stride, cellSizeY * stride, cellSizeZ * stride);
                    if (o > 0f)
                    {
                        Gizmos.color = new Color(gizmoOccupancyColor.r, gizmoOccupancyColor.g, gizmoOccupancyColor.b, gizmoOccupancyColor.a * Mathf.Clamp01(o));
                        Gizmos.DrawCube(center, size);
                    }
                    if (c > 0f)
                    {
                        Gizmos.color = new Color(gizmoCausalColor.r, gizmoCausalColor.g, gizmoCausalColor.b, gizmoCausalColor.a * Mathf.Clamp01(c));
                        Gizmos.DrawWireCube(center, size);
                    }
                }
            }
        }
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    private void DrawEmergenceLayers()
    {
        int layers = Mathf.Max(1, emergenceLayerCount);
        float duration = Mathf.Max(tMax - tMin, 0.001f);
        int stride = Mathf.Max(2, gizmoSliceStride);
        for (int layer = 0; layer < layers; layer++)
        {
            float u = (layer + 0.5f) / layers;
            float t = tMin + u * duration;
            float alpha = Mathf.Lerp(emergenceOpacityOld, emergenceOpacityNew, u);
            if (!TryGetSliceAtT(t, out Bounds bounds, out int rx, out int ry, out int rz, out float[] occ, out float[] causal))
                continue;
            float cellSizeX = bounds.size.x / rx;
            float cellSizeY = bounds.size.y / ry;
            float cellSizeZ = bounds.size.z / rz;
            Vector3 origin = bounds.min;
            Color layerColor = new Color(emergenceColor.r, emergenceColor.g, emergenceColor.b, alpha);
            for (int iz = 0; iz < rz; iz += stride)
            {
                for (int iy = 0; iy < ry; iy += stride)
                {
                    for (int ix = 0; ix < rx; ix += stride)
                    {
                        int idx = ix + rx * (iy + ry * iz);
                        if (occ[idx] <= 0f) continue;
                        Vector3 center = origin + new Vector3((ix + 0.5f) * cellSizeX, (iy + 0.5f) * cellSizeY, (iz + 0.5f) * cellSizeZ);
                        Vector3 size = new Vector3(cellSizeX * stride, cellSizeY * stride, cellSizeZ * stride);
                        Gizmos.color = layerColor;
                        Gizmos.DrawCube(center, size);
                    }
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (markerParent != null)
            Destroy(markerParent.gameObject);
    }
}
