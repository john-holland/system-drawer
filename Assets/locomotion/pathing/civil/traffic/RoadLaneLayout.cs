using System;
using UnityEngine;

public enum TravelLanePolicy
{
    StayInLanes = 0,
    IgnoreLaneGrid = 1,
    AlignGridIgnoreLanes = 2
}

[Serializable]
public sealed class RoadLaneGridSettings
{
    [Min(0.1f)] public float followTimeSec = 3f;
    [Min(0)] public float gridCarLengths = 1f;
    [Range(0f, 1f)] public float occupancy01 = 0.85f;
    [Min(0.5f)] public float carLengthM = 4.5f;

    public float CellLengthM(float currentSpeedMps, float aggressiveness01 = 0.5f)
    {
        float temporal = followTimeSec * Mathf.Max(0f, currentSpeedMps);
        if (gridCarLengths <= 1e-4f)
            return Mathf.Max(0.05f, temporal);
        float spatial = gridCarLengths * carLengthM;
        return Mathf.Max(spatial, temporal);
    }

    /// <summary>When gridCarLengths is 0, high aggressiveness shrinks bumper gap toward 0.</summary>
    public float MinSeparationM(float currentSpeedMps, float aggressiveness01 = 0.5f)
    {
        float cell = CellLengthM(currentSpeedMps, aggressiveness01);
        if (gridCarLengths <= 1e-4f)
            return cell * Mathf.Lerp(1f, 0.05f, Mathf.Clamp01(aggressiveness01));
        return cell;
    }
}

[Serializable]
public sealed class RoadLaneLayout
{
    [Min(1)] public int laneCount = 2;
    [Min(0.5f)] public float laneWidthM = 3.5f;
    public int[] directionSign = { 1, -1 };

    public float LaneCenterOffset(int laneIndex)
    {
        int n = Mathf.Max(1, laneCount);
        int i = Mathf.Clamp(laneIndex, 0, n - 1);
        return (i - (n - 1) * 0.5f) * laneWidthM;
    }

    public int LaneFromLateral(float lateralOffset)
    {
        int n = Mathf.Max(1, laneCount);
        float half = (n - 1) * 0.5f;
        int i = Mathf.RoundToInt(lateralOffset / Mathf.Max(0.1f, laneWidthM) + half);
        return Mathf.Clamp(i, 0, n - 1);
    }

    public int DirectionSign(int laneIndex)
    {
        if (directionSign == null || directionSign.Length == 0)
            return 1;
        int i = Mathf.Clamp(laneIndex, 0, directionSign.Length - 1);
        return directionSign[i] == 0 ? 0 : (directionSign[i] > 0 ? 1 : -1);
    }

    public bool LaneEnabled(int laneIndex) => DirectionSign(laneIndex) != 0;
}

/// <summary>Live occupancy slots on a road ribbon.</summary>
public sealed class RoadLaneOccupancy
{
    readonly System.Collections.Generic.Dictionary<string, TravelAgent> _slots =
        new System.Collections.Generic.Dictionary<string, TravelAgent>();

    public int OccupiedCount => _slots.Count;

    public static string SlotKey(string roadSegmentId, int laneIndex, int cellIndex) =>
        (roadSegmentId ?? "") + ":" + laneIndex + ":" + cellIndex;

    public int Cap(RoadLaneLayout layout, RoadLaneGridSettings grid, float roadLengthM)
    {
        if (layout == null || grid == null) return 0;
        float cell = Mathf.Max(0.5f, grid.CellLengthM(10f));
        int cellsAlong = Mathf.Max(1, Mathf.FloorToInt(roadLengthM / cell));
        return Mathf.Max(1, Mathf.RoundToInt(grid.occupancy01 * layout.laneCount * cellsAlong));
    }

    public bool TryOccupy(string key, TravelAgent agent)
    {
        if (string.IsNullOrEmpty(key) || agent == null) return false;
        if (_slots.TryGetValue(key, out var occ) && occ != null && occ != agent)
            return false;
        _slots[key] = agent;
        return true;
    }

    public void Release(string key)
    {
        if (!string.IsNullOrEmpty(key))
            _slots.Remove(key);
    }

    public TravelAgent Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return null;
        _slots.TryGetValue(key, out var a);
        return a;
    }
}
