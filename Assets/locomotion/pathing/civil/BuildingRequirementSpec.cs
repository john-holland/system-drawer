using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BuildingRequirementSlot
{
    public string slotId;
    public string label;
    public bool required = true;
    public Transform reference;
    public GameObject referenceObject;
    public string notes;
}

/// <summary>Checklist of required building refs (bathrooms, trash, egresses, …).</summary>
[CreateAssetMenu(fileName = "BuildingRequirementSpec", menuName = "Locomotion/Civil/Building Requirement Spec")]
public sealed class BuildingRequirementSpec : ScriptableObject
{
    public string buildingTypeId = "house";
    public CivilSystemKind civilKind = CivilSystemKind.Generic;
    public List<BuildingRequirementSlot> slots = new List<BuildingRequirementSlot>();

    public static BuildingRequirementSpec CreateDefault(string buildingTypeId, CivilSystemKind kind)
    {
        var spec = CreateInstance<BuildingRequirementSpec>();
        spec.buildingTypeId = buildingTypeId;
        spec.civilKind = kind;
        spec.slots = DefaultSlotsFor(buildingTypeId);
        return spec;
    }

    public static List<BuildingRequirementSlot> DefaultSlotsFor(string buildingTypeId)
    {
        var list = new List<BuildingRequirementSlot>
        {
            Slot("bathroom", "Bathroom", true),
            Slot("trash", "Trash pickup", true),
            Slot("egress_main", "Main egress", true),
            Slot("egress_fire", "Fire egress", false),
            Slot("parking", "Parking / arrival", false),
            Slot("repair_staging", "Repair staging", false),
            Slot("telecom", "Telecom node", false)
        };
        var id = (buildingTypeId ?? "").ToLowerInvariant();
        if (id.Contains("school"))
        {
            list.Add(Slot("reception", "Morning reception", true));
            list.Add(Slot("classroom", "Classroom", true));
        }
        if (id.Contains("church"))
        {
            list.Add(Slot("nave", "Nave / sanctuary", true));
            list.Add(Slot("valet", "Car checkpoint / valet", false));
        }
        if (id.Contains("police"))
        {
            list.Add(Slot("holding", "Holding / booking", true));
            list.Add(Slot("garage", "Car repair / garage", false));
        }
        if (id.Contains("gym"))
        {
            list.Add(Slot("front_desk", "Front desk check-in", true));
            list.Add(Slot("floor", "Gym floor", true));
        }
        if (id.Contains("factory"))
        {
            list.Add(Slot("gate", "Front gate", true));
            list.Add(Slot("line", "Manufacturing line", true));
        }
        if (id.Contains("gas"))
        {
            list.Add(Slot("pumps", "Fuel pumps", true));
            list.Add(Slot("store", "Convenience store", false));
        }
        if (id.Contains("house") || id.Contains("home") || id.Contains("residence") || id.Contains("cabin"))
        {
            list.Add(Slot("garbage", "Garbage", true));
            list.Add(Slot("garage", "Garage", false));
            list.Add(Slot("driveway", "Driveway", true));
            list.Add(Slot("eaves", "Eaves", false));
            list.Add(Slot("gutters", "Gutters", false));
            list.Add(Slot("windows", "Windows", false));
            list.Add(Slot("shed", "Shed", false));
            list.Add(Slot("guest_house", "Guest house", false));
            list.Add(Slot("playhouse", "Playhouse", false));
            list.Add(Slot("yard_toys", "Yard toys", false));
            list.Add(Slot("gas_hookup", "Gas hookup", false));
            list.Add(Slot("oil_hookup", "Oil hookup", false));
            list.Add(Slot("meters", "Utility meters", false));
            list.Add(Slot("generators", "Generators", false));
            list.Add(Slot("rv_parking", "RV parking", false));
            list.Add(Slot("extra_parking", "Additional parking", false));
            list.Add(Slot("satellite", "Satellite", false));
            list.Add(Slot("cable", "Cable demarc", false));
            list.Add(Slot("fiber", "Fiber ONT", false));
            list.Add(Slot("electrical", "Electrical connection", true));
        }
        if (id.Contains("nightclub") || id.Contains("night_club") || id.Contains("disco"))
        {
            list.Add(Slot("dance_floor", "Dance floor", true));
            list.Add(Slot("dj_booth", "DJ / music booth", true));
            list.Add(Slot("bar_station", "Bar station", true));
            list.Add(Slot("bouncer_post", "Bouncer post", true));
            list.Add(Slot("valet", "Valet / parking", false));
        }
        if (id == "bar" || id.EndsWith("_bar") || id.Contains("tavern") || id.Contains("pub"))
        {
            list.Add(Slot("bar_surface", "Bar surface", true));
            list.Add(Slot("dance_floor", "Dance floor", false));
            list.Add(Slot("bathroom", "Bathroom", true));
        }
        if (id.Contains("hotel") || id == "inn" || id.StartsWith("inn_") || id.Contains("motel"))
        {
            list.Add(Slot("front_desk", "Front desk", true));
            list.Add(Slot("guest_rooms", "Guest rooms", true));
            list.Add(Slot("linen_closet", "Linen / storage", true));
            list.Add(Slot("maintenance", "Maintenance closet", false));
            list.Add(Slot("breakfast", "Continental breakfast / kitchen", false));
            list.Add(Slot("spa_child", "Child spa", false));
        }
        if (id.Contains("checkpoint") || id.Contains("military_gate"))
        {
            list.Add(Slot("ops_center", "Ops center", true));
            list.Add(Slot("entrance_gate", "Entrance", true));
            list.Add(Slot("exit_gate", "Exit", true));
            list.Add(Slot("barracks_beds", "Beds / bedding", false));
        }
        if (id.Contains("spy") || id.Contains("intelligence") || id.Contains("embassy") || id.Contains("consulate"))
        {
            list.Add(Slot("front_desk", "Front desk / reception", true));
            list.Add(Slot("meeting_room", "Meeting room", true));
            list.Add(Slot("offices", "Offices / webtop", true));
            list.Add(Slot("cafeteria", "Cafeteria kitchen", false));
            list.Add(Slot("checkpoint", "Entrance checkpoint", true));
        }
        if (id.Contains("legislative") || id.Contains("capitol") || id.Contains("parliament")
            || id.Contains("monarch") || id.Contains("palace"))
        {
            list.Add(Slot("chamber", "Chamber / court", true));
            list.Add(Slot("offices", "Offices", true));
            list.Add(Slot("kitchen", "Kitchen", false));
            list.Add(Slot("living_quarters", "Living quarters", false));
        }
        if (id == "spa" || id.StartsWith("spa_") || id.Contains("bathhouse"))
        {
            list.Add(Slot("front_desk", "Front desk", true));
            list.Add(Slot("treatment_room", "Treatment rooms", true));
            list.Add(Slot("beds", "Beds / loungers", false));
        }
        if (id.Contains("private_industry") || id.Contains("office_building") || id.Contains("home_business"))
        {
            list.Add(Slot("front_desk", "Front desk", true));
            list.Add(Slot("meeting_room", "Meeting room", false));
            list.Add(Slot("office", "Office / webtop", true));
        }
        if (id.Contains("barber"))
        {
            list.Add(Slot("chair", "Barber chair", true));
            list.Add(Slot("sink", "Shampoo sink", true));
            list.Add(Slot("commodities", "Commodities shelf", false));
        }
        if (id.Contains("fire_station") || id.Contains("firehouse") || id.Contains("fire_dept"))
        {
            list.Add(Slot("engine_bay", "Engine bay", true));
            list.Add(Slot("pole", "Fireman pole", false));
            list.Add(Slot("sleeping", "Sleeping area", true));
            list.Add(Slot("meeting", "Meeting room / telecom", true));
            list.Add(Slot("office", "Office / webtop", true));
            list.Add(Slot("kitchen", "Kitchen", false));
            list.Add(Slot("parking", "Parking / apron", true));
            list.Add(Slot("rail_bay", "Rail firetruck bay", false));
        }
        if (id.Contains("car_repair") || id.Contains("auto_repair") || id == "garage"
            || id.Contains("vehicle_repair"))
        {
            list.Add(Slot("maintenance_bay", "Maintenance bay", true));
            list.Add(Slot("retail", "Retail / commodities", true));
            list.Add(Slot("kitchen", "Kitchen", false));
            list.Add(Slot("bathroom", "Bathroom", true));
            list.Add(Slot("parking", "Parking", true));
            list.Add(Slot("trash", "Trash", true));
        }
        if (id.Contains("police"))
        {
            list.Add(Slot("main_hall", "Main office / hall", true));
            list.Add(Slot("desk_station", "Desk stations", true));
            list.Add(Slot("meeting_room", "Meeting rooms / telecom", true));
            list.Add(Slot("private_office", "Private offices", false));
            list.Add(Slot("interrogation", "Interrogation rooms", true));
            list.Add(Slot("holding", "Holding cell", true));
            list.Add(Slot("repair_bay", "Vehicle repair bay", false));
            list.Add(Slot("parking", "Parking", true));
            list.Add(Slot("kitchen", "Kitchen", false));
        }
        return list;
    }

    static BuildingRequirementSlot Slot(string id, string label, bool required) =>
        new BuildingRequirementSlot { slotId = id, label = label, required = required };

    public bool Validate(out string error)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null || !s.required) continue;
            if (s.reference == null && s.referenceObject == null)
            {
                error = $"Missing required slot: {s.label} ({s.slotId})";
                return false;
            }
        }
        error = null;
        return true;
    }
}
