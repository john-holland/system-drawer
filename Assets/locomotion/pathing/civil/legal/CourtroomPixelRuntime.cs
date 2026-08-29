using UnityEngine;

/// <summary>Binds courtroom pixel grid layers and gallery seat BT on a legal building.</summary>
[AddComponentMenu("Locomotion/Civil/Courtroom Pixel Runtime")]
public sealed class CourtroomPixelRuntime : MonoBehaviour
{
    public LegalBuilding building;
    public CityPixelGrid grid;
    public CourtroomSeatBt seatBt;
    public PixelLightMultiSlotCatalog pixelLightCatalog;

    void Awake()
    {
        if (building == null) building = GetComponent<LegalBuilding>();
        if (seatBt == null) seatBt = GetComponent<CourtroomSeatBt>() ?? gameObject.AddComponent<CourtroomSeatBt>();
        Bind();
    }

    public void Bind()
    {
        if (building != null && grid == null)
            grid = building.courtroomGrid;
        if (grid != null)
            grid.EnsureCourtroomLayers();
        if (building != null)
        {
            building.courtroomGrid = grid;
            if (pixelLightCatalog == null)
                pixelLightCatalog = building.pixelLightCatalog;
        }
        seatBt?.RebuildAnchors();
    }

    public void SendRoomPrompts()
    {
        if (building == null || building.rooms == null) return;
        for (int i = 0; i < building.rooms.Count; i++)
        {
            var room = building.rooms[i];
            if (room == null) continue;
            if (!string.IsNullOrEmpty(room.sg4dPrompt) || !string.IsNullOrEmpty(room.inpaintPrompt))
                SendMessage("OnCourtroomRoomPrompt", room, SendMessageOptions.DontRequireReceiver);
        }
    }
}
