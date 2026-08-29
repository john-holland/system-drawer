using System;
using System.Collections.Generic;
using UnityEngine;

public enum LegalRoomKind
{
    Office = 0,
    JudgesChambers = 1,
    MeetingRoom = 2,
    Cafeteria = 3,
    Bathroom = 4,
    Courtroom = 5,
    GovSuite = 6,
    CompanySuite = 7
}

[Serializable]
public sealed class LegalRoomSpec
{
    public string roomId = "office";
    public string displayName = "Office";
    public LegalRoomKind kind = LegalRoomKind.Office;
    public int floorIndex = 1;
    public string sg4dPrompt;
    public string inpaintPrompt;
    public PixelLightMultiSlotCatalog pixelLightSlots;
    public string pixelLightSlotId;
    public Vector3 worldPosition;
}

/// <summary>Courthouse / legal building with rooms, company pecking, and stations.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Legal Building")]
public sealed class LegalBuilding : MonoBehaviour
{
    public string buildingId = "courthouse";
    public BuildingRequirementSpec requirements;
    public List<LegalRoomSpec> rooms = new List<LegalRoomSpec>();
    public CompanyRegistration company;
    public CourtWarden courtWarden;
    public CourtSystemBioRhythm bioRhythm;
    public PixelLightMultiSlotCatalog pixelLightCatalog;
    public CityPixelGrid courtroomGrid;

    public LegalRoomSpec FindRoom(string roomId)
    {
        if (rooms == null || string.IsNullOrEmpty(roomId))
            return null;
        for (int i = 0; i < rooms.Count; i++)
            if (rooms[i] != null && rooms[i].roomId == roomId)
                return rooms[i];
        return null;
    }

    public void EnsureDefaultRooms()
    {
        if (rooms != null && rooms.Count > 0)
            return;
        rooms = new List<LegalRoomSpec>
        {
            Room("chambers", "Judges' Chambers", LegalRoomKind.JudgesChambers, 2),
            Room("courtroom", "Courtroom", LegalRoomKind.Courtroom, 1),
            Room("offices", "Offices", LegalRoomKind.Office, 2),
            Room("meeting", "Meeting Room", LegalRoomKind.MeetingRoom, 2),
            Room("cafeteria", "Cafeteria", LegalRoomKind.Cafeteria, 1),
            Room("bathroom", "Bathroom", LegalRoomKind.Bathroom, 1),
            Room("gov_suite", "Gov Suite", LegalRoomKind.GovSuite, 3),
            Room("company_suite", "Company Suite", LegalRoomKind.CompanySuite, 3)
        };
    }

    static LegalRoomSpec Room(string id, string name, LegalRoomKind kind, int floor)
    {
        return new LegalRoomSpec
        {
            roomId = id,
            displayName = name,
            kind = kind,
            floorIndex = floor
        };
    }
}
