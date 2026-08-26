using UnityEngine;

/// <summary>Sends campus room SG4D / in-paint prompts through FloorPlanIndexMapHost plus per-room messages.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Campus Pixel Runtime")]
public sealed class CampusPixelRuntime : MonoBehaviour
{
    public UniversityCampusAsset campus;
    public FloorPlanIndexMapHost floorHost;
    public CityPixelGridRuntime gridRuntime;

    void Awake()
    {
        if (floorHost == null)
            floorHost = GetComponent<FloorPlanIndexMapHost>();
        if (gridRuntime == null)
            gridRuntime = GetComponent<CityPixelGridRuntime>();
    }

    public void SendRoomPrompts()
    {
        floorHost?.SendSg3dZonePrompts();
        if (campus == null || campus.rooms == null) return;
        for (int i = 0; i < campus.rooms.Count; i++)
        {
            var room = campus.rooms[i];
            if (room == null) continue;
            SendMessage("OnCampusRoomInpaint", room, SendMessageOptions.DontRequireReceiver);
            if (!string.IsNullOrEmpty(room.sg4dPrompt))
                SendMessage("OnCityPixelSg4dRoom", room, SendMessageOptions.DontRequireReceiver);
        }
    }
}
