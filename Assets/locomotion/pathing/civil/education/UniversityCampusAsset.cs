using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UniversityCampus", menuName = "Locomotion/Civil/University Campus")]
public sealed class UniversityCampusAsset : ScriptableObject
{
    public string campusId = "campus";
    public UniversityAgeBracket defaultBracket = UniversityAgeBracket.Undergrad;
    public UniversityCurriculumAsset curriculum;
    public CityPixelGrid campusGrid;
    public StreetBlocksPlanAsset elevationPlan;
    public PixelLightMultiSlotCatalog pixelLightCatalog;
    public List<CampusElevationBand> elevationBands = new List<CampusElevationBand>();
    public List<CampusRoomSpec> rooms = new List<CampusRoomSpec>();
    public string dormCompanyId = "campus_dorm";
    public Vector3 campusOrigin;

    public CampusRoomSpec FindRoom(string roomId)
    {
        if (rooms == null || string.IsNullOrEmpty(roomId)) return null;
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i] != null && rooms[i].roomId == roomId)
                return rooms[i];
        return null;
    }

    public Vector3 RoomWorld(string roomId)
    {
        var room = FindRoom(roomId);
        if (room == null) return campusOrigin;
        return room.worldPosition.sqrMagnitude > 1e-6f ? room.worldPosition : campusOrigin;
    }

    public void EnsureDefaultElevationBands()
    {
        if (elevationBands != null && elevationBands.Count > 0) return;
        elevationBands = new List<CampusElevationBand>
        {
            new CampusElevationBand { id = "basement", depthMinM = -12f, depthMaxM = 0f },
            new CampusElevationBand { id = "quad", depthMinM = 0f, depthMaxM = 15f },
            new CampusElevationBand { id = "podium", depthMinM = 15f, depthMaxM = 30f },
            new CampusElevationBand { id = "tower", depthMinM = 30f, depthMaxM = 150f }
        };
    }

    public static UniversityCampusAsset CreateBoardingDefaults()
    {
        var c = CreateInstance<UniversityCampusAsset>();
        c.name = "BoardingCampus";
        c.EnsureDefaultElevationBands();
        c.rooms = new List<CampusRoomSpec>
        {
            new CampusRoomSpec { roomId = "lecture", displayName = "Lecture Hall", station = LearningStationKind.Class, floorIndex = 1 },
            new CampusRoomSpec { roomId = "library", displayName = "Library", station = LearningStationKind.Library, floorIndex = 1 },
            new CampusRoomSpec { roomId = "dorm", displayName = "Dorm", station = LearningStationKind.Desk, floorIndex = 2, zoneId = "dorm" },
            new CampusRoomSpec { roomId = "dining", displayName = "Dining", station = LearningStationKind.Conversation, floorIndex = 1 }
        };
        return c;
    }
}
