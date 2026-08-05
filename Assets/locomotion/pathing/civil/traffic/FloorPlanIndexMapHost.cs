using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime host for a building's FloorPlanIndexMap (directory, attendant dialog, SG3D zones).</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Floor Plan Index Map Host")]
public sealed class FloorPlanIndexMapHost : MonoBehaviour
{
    public FloorPlanIndexMap map;
    public string buildingStableId;

    void Awake()
    {
        if (map != null && string.IsNullOrEmpty(buildingStableId))
            buildingStableId = map.buildingStableId;
    }

    public bool TryGetAttendantDialog(int floorIndex, string zoneId, out string dialogTreeId)
    {
        dialogTreeId = null;
        return map != null && map.TryGetAttendantDialog(floorIndex, zoneId, out dialogTreeId);
    }

    public IReadOnlyList<FloorPlanDirectoryEntry> GetDirectory()
    {
        if (map == null) return System.Array.Empty<FloorPlanDirectoryEntry>();
        return map.GetDirectory();
    }

    public void SendSg3dZonePrompts()
    {
        if (map == null) return;
        SendMessage("OnCityPixelFloorPlanReady", map, SendMessageOptions.DontRequireReceiver);
        if (map.floors == null) return;
        for (int i = 0; i < map.floors.Count; i++)
        {
            var floor = map.floors[i];
            if (floor == null) continue;
            SendMessage("OnCityPixelSg3dFloorZone", floor, SendMessageOptions.DontRequireReceiver);
        }
    }
}
