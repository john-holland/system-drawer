using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StairwellFloorLanding
{
    public string landingId;
    [Tooltip("House convention: first=1, basement=0, sub-basement=-1 (HouseFloorIndex).")]
    public int floorIndex;
    public Vector3 worldPosition;
    public List<string> railingIds = new List<string>();
}

/// <summary>Ordered stairwell topology for depth-first railing search.</summary>
[CreateAssetMenu(fileName = "StairwellTopology", menuName = "Locomotion/Stairwell Topology", order = 121)]
public sealed class StairwellTopologyAsset : ScriptableObject
{
    public List<StairwellFloorLanding> floors = new List<StairwellFloorLanding>();
    public string elevatorRaceGoalId = "elevator";

    /// <summary>Depth-first order: top floor railings first, then lower floors.</summary>
    public List<string> EnumerateRailingsDepthFirst()
    {
        var ordered = new List<StairwellFloorLanding>(floors);
        ordered.Sort((a, b) => b.floorIndex.CompareTo(a.floorIndex));
        var ids = new List<string>();
        for (int i = 0; i < ordered.Count; i++)
        {
            var f = ordered[i];
            if (f?.railingIds == null) continue;
            for (int r = 0; r < f.railingIds.Count; r++)
                if (!string.IsNullOrEmpty(f.railingIds[r]))
                    ids.Add(f.railingIds[r]);
        }
        return ids;
    }

    public float RemainingDepthNormalized(int currentFloorIndex, int minFloor)
    {
        int span = Mathf.Max(1, MaxFloor() - minFloor);
        return Mathf.Clamp01((currentFloorIndex - minFloor) / (float)span);
    }

    public int MaxFloor()
    {
        int max = 0;
        for (int i = 0; i < floors.Count; i++)
            if (floors[i] != null)
                max = Mathf.Max(max, floors[i].floorIndex);
        return max;
    }

    public int MinFloor()
    {
        int min = int.MaxValue;
        for (int i = 0; i < floors.Count; i++)
            if (floors[i] != null)
                min = Mathf.Min(min, floors[i].floorIndex);
        return min == int.MaxValue ? 0 : min;
    }
}
