using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>Maps narrative time → frame; applies leases and materializes brush stamps.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/City Pixel Grid Runtime")]
public sealed class CityPixelGridRuntime : MonoBehaviour
{
    public CityPixelGrid grid;
    public TrafficWarden warden;
    public NarrativeClock clock;
    public Transform materializeRoot;
    public bool materializePrefabs = true;

    public int ActiveFrameIndex { get; private set; }
    int _lastMaterializedFrame = int.MinValue;
    readonly List<GameObject> _spawned = new List<GameObject>();
    readonly List<Vector3> _signAvoidPoints = new List<Vector3>();

    void Awake()
    {
        if (warden == null)
            warden = TrafficWarden.Instance ?? FindFirstObjectByType<TrafficWarden>();
        if (clock == null)
            clock = FindFirstObjectByType<NarrativeClock>();
        if (materializeRoot == null)
            materializeRoot = transform;
        if (warden != null && grid != null)
            warden.cityGrid = grid;
    }

    void Update()
    {
        Tick(Time.deltaTime);
    }

    public void Tick(float dt)
    {
        if (grid == null) return;
        grid.EnsureLayersAndFrames();
        ActiveFrameIndex = ResolveFrameIndex();
        ApplyLeases();
        PreferBakeOnWarden();
        if (_lastMaterializedFrame != ActiveFrameIndex)
        {
            MaterializeStampsForFrame(ActiveFrameIndex);
            _lastMaterializedFrame = ActiveFrameIndex;
        }
        ApplySignHintsToAgents();
    }

    public int ResolveFrameIndex()
    {
        float gran = Mathf.Max(0.01f, grid.frameGranularitySec);
        float t = 0f;
        if (clock != null)
            t = NarrativeCalendarMath.DateTimeToSeconds(clock.Now);
        int f = Mathf.FloorToInt(t / gran);
        if (grid.frameCount <= 0) return 0;
        f %= grid.frameCount;
        if (f < 0) f += grid.frameCount;
        return f;
    }

    public void ApplyLeases()
    {
        if (warden == null) return;
        bool lease = false;
        for (int li = 0; li < grid.layers.Count; li++)
        {
            var layer = grid.layers[li];
            if (layer == null || layer.kind == CityPixelLayerKind.Roads) continue;
            if (ActiveFrameIndex >= layer.frames.Count) continue;
            var frame = layer.frames[ActiveFrameIndex];
            for (int i = 0; i < frame.cells.Length; i++)
            {
                if (frame.cells[i] != 0)
                {
                    lease = true;
                    break;
                }
            }
            if (lease) break;
        }
        warden.narrativeLeaseActive = lease;
        if (lease)
            warden.stateMachine.Enter(TrafficWardenMode.NarrativeLease);
    }

    public List<Bounds> GetActiveLeases()
    {
        var list = new List<Bounds>();
        if (grid == null) return list;
        for (int li = 0; li < grid.layers.Count; li++)
        {
            var layer = grid.layers[li];
            if (layer == null || layer.kind == CityPixelLayerKind.Roads) continue;
            if (ActiveFrameIndex >= layer.frames.Count) continue;
            var frame = layer.frames[ActiveFrameIndex];
            for (int y = 0; y < grid.height; y++)
            for (int x = 0; x < grid.width; x++)
            {
                if (frame.Get(x, y, grid.width) == 0) continue;
                list.Add(new Bounds(grid.CellToWorld(x, y), Vector3.one * grid.cellWorldSize));
            }
        }
        return list;
    }

    void PreferBakeOnWarden()
    {
        if (warden == null) return;
        var bake = grid.FindBake(ActiveFrameIndex);
        if (bake != null && bake.mstEdges != null && bake.mstEdges.Count > 0)
            CityPixelGridBaker.ApplyBakeToWarden(grid, ActiveFrameIndex, warden);
    }

    public void MaterializeStampsForFrame(int frameIndex)
    {
        ClearSpawned();
        _signAvoidPoints.Clear();
        if (!materializePrefabs || grid.brushStamps == null) return;

        // Non-scalable stamps (signs, detail, detours). Separators spawn nothing.
        for (int i = 0; i < grid.brushStamps.Count; i++)
        {
            var s = grid.brushStamps[i];
            if (s.frameIndex != frameIndex) continue;
            if (CityPlaceableChunker.IsSeparator(s.kind) || CityPlaceableChunker.IsScalable(s.kind))
                continue;

            Vector3 world = grid.CellToWorld(s.cellX, s.cellY);
            Quaternion rot = Quaternion.Euler(0f, s.yawDegrees, 0f);
            switch (s.kind)
            {
                case CityPixelBrushKind.PoliceDetail:
                    warden?.RequestPoliceTrafficDetail(world);
                    break;
                case CityPixelBrushKind.OneWay:
                case CityPixelBrushKind.StopSign:
                case CityPixelBrushKind.Sign:
                    SpawnPrefab(s.signPrefab, world, rot);
                    _signAvoidPoints.Add(world);
                    break;
                case CityPixelBrushKind.Detour:
                    if (s.signagePrefabs != null)
                    {
                        for (int p = 0; p < s.signagePrefabs.Length; p++)
                            SpawnPrefab(s.signagePrefabs[p], world + Vector3.right * p * 0.5f, rot);
                    }
                    else
                        SpawnPrefab(s.signPrefab, world, rot);
                    break;
            }
        }

        var chunkResult = CityPlaceableChunker.ChunkFrame(grid, frameIndex, grid.catalog);
        for (int c = 0; c < chunkResult.chunks.Count; c++)
            MaterializeChunk(chunkResult.chunks[c]);
    }

    void MaterializeChunk(CityPlaceableChunk chunk)
    {
        if (chunk == null || chunk.cells == null || chunk.cells.Count == 0) return;

        Vector3 world = grid.CellToWorld(chunk.minX, chunk.minY);
        var candidate = CityPlaceableBestFit.Pick(chunk, grid.catalog);
        GameObject prefab = chunk.forcedPrefab
                            ?? candidate?.prefab
                            ?? FirstStampPrefab(chunk);

        GameObject go;
        string label = chunk.isSharedShell
            ? "SharedBuilding_" + chunk.minX + "_" + chunk.minY
            : chunk.placeableKind + "_" + chunk.typeKey + "_" + chunk.minX + "_" + chunk.minY;

        if (prefab != null)
            go = Object.Instantiate(prefab, world, Quaternion.identity, materializeRoot);
        else
        {
            go = new GameObject(label);
            go.transform.SetParent(materializeRoot, false);
            go.transform.position = world;
        }
        go.name = label;

        if (chunk.placeableKind == CityPlaceableKind.Intersection)
            AttachIntersection(go, chunk);
        else if (chunk.placeableKind == CityPlaceableKind.SchoolBusStop)
        {
            // Prefab already spawned when available; empty shell is fine.
        }
        else
            AttachBuildingTenants(go, chunk, candidate);

        AttachFloorPlan(go, chunk, candidate);
        _spawned.Add(go);
    }

    static GameObject FirstStampPrefab(CityPlaceableChunk chunk)
    {
        for (int i = 0; i < chunk.cells.Count; i++)
        {
            var s = chunk.cells[i]?.stamp;
            if (s?.signPrefab != null) return s.signPrefab;
        }
        return null;
    }

    void AttachIntersection(GameObject go, CityPlaceableChunk chunk)
    {
        var ctrl = go.GetComponent<TrafficLightController>() ?? go.AddComponent<TrafficLightController>();
        var timing = TrafficLightLadderTiming.Default();
        timing.ApplyTo(ctrl);
        var decorator = go.GetComponent<TrafficLightPoleDecorator>() ?? go.AddComponent<TrafficLightPoleDecorator>();
        decorator.controller = ctrl;
        decorator.createHeadsIfMissing = true;
        decorator.EnsureHeads();
        var stamp = chunk.cells[0].stamp;
        if (stamp?.pixelLightPattern != null)
        {
            foreach (var rig in go.GetComponentsInChildren<PixelLightRig>())
                rig.SetPattern(stamp.pixelLightPattern);
        }
        warden?.RefreshLights();
    }

    void AttachBuildingTenants(GameObject go, CityPlaceableChunk chunk, CityPlaceableCandidate candidate)
    {
        if (chunk.isSharedShell)
        {
            for (int t = 0; t < chunk.tenantTypeKeys.Count; t++)
            {
                string tk = chunk.tenantTypeKeys[t];
                var tenantGo = new GameObject("Tenant_" + tk);
                tenantGo.transform.SetParent(go.transform, false);
                var stub = tenantGo.AddComponent<CivilInstitutionStub>();
                stub.kind = ParseCivilKind(tk, chunk);
            }
            if (candidate?.sg3dPromptComposition != null)
                go.SendMessage("OnCityPixelSg3dSharedBuilding", candidate, SendMessageOptions.DontRequireReceiver);
            return;
        }

        var shellStub = go.GetComponent<CivilInstitutionStub>() ?? go.AddComponent<CivilInstitutionStub>();
        shellStub.kind = ParseCivilKind(chunk.typeKey, chunk);
        if (candidate?.buildingConfig != null && chunk.cells[0].stamp != null)
            chunk.cells[0].stamp.buildingConfig = candidate.buildingConfig;
        if (candidate?.sg3dPromptComposition != null)
            go.SendMessage("OnCityPixelSg3dBuilding", candidate, SendMessageOptions.DontRequireReceiver);
    }

    static CivilSystemKind ParseCivilKind(string typeKey, CityPlaceableChunk chunk)
    {
        for (int i = 0; i < chunk.cells.Count; i++)
        {
            var s = chunk.cells[i]?.stamp;
            if (s != null && string.Equals(CityPlaceableChunker.ResolveTypeKey(s), typeKey, System.StringComparison.OrdinalIgnoreCase))
                return s.buildingKind;
        }
        if (!string.IsNullOrEmpty(typeKey) && System.Enum.TryParse(typeKey, true, out CivilSystemKind parsed))
            return parsed;
        return CivilSystemKind.Generic;
    }

    void AttachFloorPlan(GameObject go, CityPlaceableChunk chunk, CityPlaceableCandidate candidate)
    {
        if (chunk.placeableKind != CityPlaceableKind.Building) return;

        FloorPlanIndexMap map = chunk.floorPlanOverride
                                ?? candidate?.defaultFloorPlanIndexMap;
        string stableId = go.name;
        if (map == null)
            map = FloorPlanIndexMap.BuildFromChunk(chunk, stableId);
        else if (string.IsNullOrEmpty(map.buildingStableId))
            map.buildingStableId = stableId;

        var host = go.GetComponent<FloorPlanIndexMapHost>() ?? go.AddComponent<FloorPlanIndexMapHost>();
        host.map = map;
        host.buildingStableId = map.buildingStableId;
        host.SendSg3dZonePrompts();
    }

    void SpawnPrefab(GameObject prefab, Vector3 world, Quaternion rot)
    {
        if (prefab == null) return;
        var go = Object.Instantiate(prefab, world, rot, materializeRoot);
        _spawned.Add(go);
    }

    void ClearSpawned()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                Object.Destroy(_spawned[i]);
        }
        _spawned.Clear();
    }

    void ApplySignHintsToAgents()
    {
        if (_signAvoidPoints.Count == 0) return;
        var agents = TravelAgentRegistry.All;
        for (int i = 0; i < agents.Count; i++)
        {
            var ta = agents[i];
            if (ta == null || ta.ignoreTrafficAvoidance) continue;
            for (int p = 0; p < _signAvoidPoints.Count; p++)
            {
                // Soft: inflate avoid via temporary points on solver each rebuild
            }
            if (ta.pathingSolverForPreview != null)
            {
                ta.pathingSolverForPreview.SetSoftAvoid(
                    _signAvoidPoints.ToArray(),
                    ta.avoidRadius > 0 ? ta.avoidRadius : 12f,
                    ta.avoidCostMultiplier > 0 ? ta.avoidCostMultiplier : 3f,
                    enabled: true);
            }
        }
    }

    /// <summary>Export trafficEvents / painted clusters into a NarrativeCalendarAsset.</summary>
    public static int ExportNarrativeEvents(CityPixelGrid grid, NarrativeCalendarAsset calendar)
    {
        if (grid == null || calendar == null) return 0;
        if (calendar.events == null)
            calendar.events = new List<NarrativeCalendarEvent>();
        grid.EnsureLayersAndFrames();
        int added = 0;

        if (grid.trafficEvents != null)
        {
            for (int i = 0; i < grid.trafficEvents.Count; i++)
            {
                var spec = grid.trafficEvents[i];
                if (spec == null) continue;
                Bounds4 vol = spec.volumeOverride ?? grid.CellClusterToBounds4(0, 0, grid.width - 1, grid.height - 1, spec.frameStart);
                // Prefer painted cells on matching layer.
                if (!string.IsNullOrEmpty(spec.layerId))
                {
                    var painted = FindPaintedBounds(grid, spec.layerId, spec.frameStart);
                    if (painted.HasValue) vol = painted.Value;
                }
                UpsertEvent(calendar, spec.eventId, spec.title, vol, spec.narrativeActionHints);
                added++;
            }
        }

        // Also export hazard layer components per frame as events.
        for (int li = 0; li < grid.layers.Count; li++)
        {
            var layer = grid.layers[li];
            if (layer == null || layer.kind == CityPixelLayerKind.Roads) continue;
            for (int f = 0; f < grid.frameCount && f < layer.frames.Count; f++)
            {
                var vol = FindPaintedBounds(grid, layer.layerId, f);
                if (!vol.HasValue) continue;
                string id = layer.layerId + "_f" + f;
                UpsertEvent(calendar, id, layer.kind + " @ frame " + f, vol.Value, "traffic_warden_lease");
                added++;
            }
        }

        return added;
    }

    static Bounds4? FindPaintedBounds(CityPixelGrid grid, string layerId, int frameIndex)
    {
        CityPixelLayer layer = null;
        for (int i = 0; i < grid.layers.Count; i++)
            if (grid.layers[i] != null && grid.layers[i].layerId == layerId)
            {
                layer = grid.layers[i];
                break;
            }
        if (layer == null || frameIndex >= layer.frames.Count) return null;
        var frame = layer.frames[frameIndex];
        int minX = grid.width, minY = grid.height, maxX = -1, maxY = -1;
        for (int y = 0; y < grid.height; y++)
        for (int x = 0; x < grid.width; x++)
        {
            if (frame.Get(x, y, grid.width) == 0) continue;
            if (x < minX) minX = x;
            if (y < minY) minY = y;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
        }
        if (maxX < 0) return null;
        return grid.CellClusterToBounds4(minX, minY, maxX, maxY, frameIndex);
    }

    static void UpsertEvent(NarrativeCalendarAsset calendar, string id, string title, Bounds4 vol, string notes)
    {
        NarrativeCalendarEvent existing = null;
        for (int i = 0; i < calendar.events.Count; i++)
        {
            if (calendar.events[i] != null && calendar.events[i].id == id)
            {
                existing = calendar.events[i];
                break;
            }
        }
        if (existing == null)
        {
            existing = new NarrativeCalendarEvent { id = id };
            calendar.events.Add(existing);
        }
        existing.title = title;
        existing.notes = notes ?? "";
        existing.spatiotemporalVolume = vol;
        existing.durationSeconds = Mathf.Max(1, Mathf.RoundToInt(vol.durationT));
    }

    void OnDestroy() => ClearSpawned();
}
