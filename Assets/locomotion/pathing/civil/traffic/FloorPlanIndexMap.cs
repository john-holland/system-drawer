using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FloorPlanZoneCell
{
    public int localX;
    public int localZ;
    public string zoneId = "zone";
    public string tenantTypeKey;
    public string displayName;
}

[Serializable]
public sealed class FloorPlanDirectoryEntry
{
    public string name;
    public string zoneId;
    public int floorIndex;
}

[Serializable]
public sealed class FloorPlanFloor
{
    [Tooltip("House convention: first=1, basement=0, sub-basement=-1 (HouseFloorIndex).")]
    public int floorIndex;
    public string label = "Floor";
    public List<FloorPlanZoneCell> cells = new List<FloorPlanZoneCell>();
    [Tooltip("NarrativeTree / lemma dialogue-set id for attendants on this floor.")]
    public string attendantDialogTreeId;
    public List<FloorPlanDirectoryEntry> directoryEntries = new List<FloorPlanDirectoryEntry>();
}

[Serializable]
public sealed class FloorPlanSharedSpace
{
    public string spaceId = "corridor";
    public int floorIndex;
    public string tenantTypeKey = "shared";
    public string label = "Shared";
}

/// <summary>Floor/zone division for shared (and single-tenant) buildings — maps, SG3D zones, attendant dialog.</summary>
[CreateAssetMenu(fileName = "FloorPlanIndexMap", menuName = "Locomotion/Civil/Floor Plan Index Map")]
public sealed class FloorPlanIndexMap : ScriptableObject
{
    public string mapId = "floor_plan";
    public string buildingStableId;
    public int floorCount = 1;
    public List<FloorPlanFloor> floors = new List<FloorPlanFloor>();
    public List<FloorPlanSharedSpace> sharedSpaces = new List<FloorPlanSharedSpace>();

    public FloorPlanFloor GetFloor(int floorIndex)
    {
        if (floors == null) return null;
        for (int i = 0; i < floors.Count; i++)
            if (floors[i] != null && floors[i].floorIndex == floorIndex)
                return floors[i];
        return null;
    }

    public bool TryGetAttendantDialog(int floorIndex, string zoneId, out string dialogTreeId)
    {
        dialogTreeId = null;
        var floor = GetFloor(floorIndex);
        if (floor == null) return false;
        if (!string.IsNullOrEmpty(floor.attendantDialogTreeId))
        {
            dialogTreeId = floor.attendantDialogTreeId;
            return true;
        }
        return false;
    }

    public List<FloorPlanDirectoryEntry> GetDirectory()
    {
        var list = new List<FloorPlanDirectoryEntry>();
        if (floors == null) return list;
        for (int i = 0; i < floors.Count; i++)
        {
            var f = floors[i];
            if (f?.directoryEntries == null) continue;
            list.AddRange(f.directoryEntries);
        }
        return list;
    }

    /// <summary>Build a simple map from placeable stamps (floorIndex, zoneId, typeKey).</summary>
    public static FloorPlanIndexMap BuildFromStamps(
        IReadOnlyList<CityPixelBrushStamp> stamps,
        int minX,
        int minZ,
        string buildingStableId)
    {
        var map = CreateInstance<FloorPlanIndexMap>();
        map.mapId = "auto_" + (buildingStableId ?? "building");
        map.buildingStableId = buildingStableId ?? map.mapId;
        map.floors = new List<FloorPlanFloor>();
        if (stamps == null) return map;

        var floorLookup = new Dictionary<int, FloorPlanFloor>();
        for (int i = 0; i < stamps.Count; i++)
        {
            var s = stamps[i];
            if (s == null) continue;
            int fi = Mathf.Max(0, s.floorIndex);
            if (!floorLookup.TryGetValue(fi, out var floor))
            {
                floor = new FloorPlanFloor
                {
                    floorIndex = fi,
                    label = fi == 0 ? "Lobby" : "L" + fi,
                    attendantDialogTreeId = "attendant_floor_" + fi
                };
                floorLookup[fi] = floor;
                map.floors.Add(floor);
            }

            string typeKey = CityPlaceableChunker.ResolveTypeKey(s);
            string zoneId = string.IsNullOrEmpty(s.zoneId) ? typeKey : s.zoneId;
            floor.cells.Add(new FloorPlanZoneCell
            {
                localX = s.cellX - minX,
                localZ = s.cellY - minZ,
                zoneId = zoneId,
                tenantTypeKey = typeKey,
                displayName = typeKey
            });

            bool hasDir = false;
            for (int d = 0; d < floor.directoryEntries.Count; d++)
            {
                if (floor.directoryEntries[d].zoneId == zoneId)
                {
                    hasDir = true;
                    break;
                }
            }
            if (!hasDir)
            {
                floor.directoryEntries.Add(new FloorPlanDirectoryEntry
                {
                    name = typeKey,
                    zoneId = zoneId,
                    floorIndex = fi
                });
            }
        }

        map.floorCount = floorLookup.Count > 0 ? floorLookup.Count : 1;
        return map;
    }

    /// <summary>Build a simple map from a placeable chunk's cells.</summary>
    public static FloorPlanIndexMap BuildFromChunk(CityPlaceableChunk chunk, string buildingStableId)
    {
        if (chunk == null) return BuildFromStamps(null, 0, 0, buildingStableId);
        var stamps = new List<CityPixelBrushStamp>(chunk.cells.Count);
        for (int i = 0; i < chunk.cells.Count; i++)
        {
            var cell = chunk.cells[i];
            if (cell?.stamp != null)
            {
                stamps.Add(cell.stamp);
                continue;
            }
            stamps.Add(new CityPixelBrushStamp
            {
                cellX = cell.cellX,
                cellY = cell.cellY,
                kind = CityPixelBrushKind.Building,
                typeKey = cell.typeKey,
                floorIndex = cell.floorIndex,
                zoneId = cell.zoneId,
                heightCells = 1
            });
        }
        return BuildFromStamps(stamps, chunk.minX, chunk.minY, buildingStableId);
    }
}
