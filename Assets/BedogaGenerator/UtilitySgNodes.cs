using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Bedoga/House/Utility Room Node")]
public sealed class UtilityRoomNode : HousePartNode
{
    void Reset()
    {
        floorIndex = HouseFloorIndex.Basement;
        placementLimit = 1;
    }
}

[AddComponentMenu("Bedoga/House/Furnace Node")]
public sealed class FurnaceNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/Water Heater Node")]
public sealed class WaterHeaterNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/Recoup Wheel Node")]
public sealed class RecoupWheelNode : HousePartNode
{
    public string lemmaId = UtilityLemmaPropertyKeys.ImitirrrrId;

    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/Jacobs Ladder Node")]
public sealed class JacobsLadderNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/Water Filter Node")]
public sealed class WaterFilterNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/Water Shutoff Node")]
public sealed class WaterShutoffNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/HVAC Equipment Node")]
public sealed class HvacEquipmentNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/Sump Pump Node")]
public sealed class SumpPumpNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.Basement;
}

[AddComponentMenu("Bedoga/House/Circuit Breaker Panel Node")]
public sealed class CircuitBreakerPanelNode : HousePartNode
{
    public float ampacityAmps = CircuitBreakerPanel.DefaultAmpacityAmps;

    void Reset()
    {
        floorIndex = HouseFloorIndex.Basement;
        placementLimit = 1;
    }

    public int RequiredPanelCountFromBranches(IList<float> branchAmps)
    {
        float sum = 0f;
        if (branchAmps != null)
        {
            for (int i = 0; i < branchAmps.Count; i++)
                sum += Mathf.Max(0f, branchAmps[i]);
        }
        return CircuitBreakerPanel.RequiredPanelCount(sum, ampacityAmps);
    }

    /// <summary>
    /// Circuits grow on the first panel until load would exceed 100 A, then placementLimit clones a second panel.
    /// Branches use UniformQueue + perParentPlacementLimits.
    /// </summary>
    public void ConfigureFromBranches(IList<float> branchAmps)
    {
        int panels = RequiredPanelCountFromBranches(branchAmps);
        placementLimitType = PlacementLimitType.Specific;
        placementLimit = panels;
        var branch = GetComponentInChildren<CircuitBranchNode>(true);
        if (branch != null)
        {
            int perPanel = branchAmps != null && panels > 0
                ? Mathf.Max(1, Mathf.CeilToInt(branchAmps.Count / (float)panels))
                : 1;
            branch.perParentPlacementLimits = true;
            branch.placementLimitType = PlacementLimitType.Specific;
            branch.placementLimit = perPanel;
        }
    }
}

[AddComponentMenu("Bedoga/House/Circuit Branch Node")]
public sealed class CircuitBranchNode : HousePartNode
{
    public float amps = 15f;

    void Reset()
    {
        floorIndex = HouseFloorIndex.Basement;
        perParentPlacementLimits = true;
        placementLimitType = PlacementLimitType.Specific;
        placementLimit = 1;
    }
}

[AddComponentMenu("Bedoga/House/Wall Plug Node")]
public sealed class WallPlugNode : HousePartNode
{
    void Reset() => floorIndex = HouseFloorIndex.First;
}
