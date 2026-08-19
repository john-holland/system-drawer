using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Registry of civil venues ordered by developer priority (lower = more important).</summary>
[Serializable]
public sealed class CivilSystemLattice
{
    public List<CivilVenueNode> venues = new List<CivilVenueNode>();

    /// <summary>Kind priority order from settings (first = highest). Used to break ties / filter.</summary>
    public List<CivilSystemKind> kindPriorityOrder = new List<CivilSystemKind>
    {
        CivilSystemKind.Kitchen,
        CivilSystemKind.SoupKitchen,
        CivilSystemKind.School,
        CivilSystemKind.PoliceStation,
        CivilSystemKind.Prison,
        CivilSystemKind.Church,
        CivilSystemKind.Library,
        CivilSystemKind.Mall,
        CivilSystemKind.Factory,
        CivilSystemKind.Gym,
        CivilSystemKind.TownHall,
        CivilSystemKind.GasStation,
        CivilSystemKind.Park,
        CivilSystemKind.SanitationFacility,
        CivilSystemKind.TransitHub,
        CivilSystemKind.BusDepot,
        CivilSystemKind.TrainStation,
        CivilSystemKind.Airport,
        CivilSystemKind.RailMaintenanceDepot,
        CivilSystemKind.GrainSilo,
        CivilSystemKind.LiquorStore,
        CivilSystemKind.House,
        CivilSystemKind.CarRepair,
        CivilSystemKind.Bathroom,
        CivilSystemKind.FireStation,
        CivilSystemKind.MilitaryCheckpoint,
        CivilSystemKind.Embassy,
        CivilSystemKind.SpyAgency,
        CivilSystemKind.GovLegislative,
        CivilSystemKind.Monarchic,
        CivilSystemKind.Hotel,
        CivilSystemKind.Inn,
        CivilSystemKind.NightClub,
        CivilSystemKind.Bar,
        CivilSystemKind.Spa,
        CivilSystemKind.BarberShop,
        CivilSystemKind.PrivateIndustry,
        CivilSystemKind.Generic
    };

    public void Register(CivilVenueNode node)
    {
        if (node == null || string.IsNullOrEmpty(node.stableId)) return;
        for (int i = 0; i < venues.Count; i++)
        {
            if (venues[i] != null && venues[i].stableId == node.stableId)
            {
                venues[i] = node;
                return;
            }
        }
        venues.Add(node);
    }

    public CivilVenueNode Get(string stableId)
    {
        if (string.IsNullOrEmpty(stableId)) return null;
        for (int i = 0; i < venues.Count; i++)
            if (venues[i] != null && venues[i].stableId == stableId)
                return venues[i];
        return null;
    }

    public List<CivilVenueNode> OrderedByPriority()
    {
        var list = new List<CivilVenueNode>();
        for (int i = 0; i < venues.Count; i++)
            if (venues[i] != null)
                list.Add(venues[i]);
        list.Sort(ComparePriority);
        return list;
    }

    int ComparePriority(CivilVenueNode a, CivilVenueNode b)
    {
        int ka = KindRank(a.kind);
        int kb = KindRank(b.kind);
        if (ka != kb) return ka.CompareTo(kb);
        int pa = a.developerPriority;
        int pb = b.developerPriority;
        if (pa != pb) return pa.CompareTo(pb);
        return string.CompareOrdinal(a.stableId, b.stableId);
    }

    int KindRank(CivilSystemKind kind)
    {
        if (kindPriorityOrder == null) return 100;
        int idx = kindPriorityOrder.IndexOf(kind);
        return idx >= 0 ? idx : 100;
    }

    public static CivilSystemKind KindFromBuildingType(string buildingTypeId)
    {
        if (string.IsNullOrEmpty(buildingTypeId)) return CivilSystemKind.Generic;
        var id = buildingTypeId.ToLowerInvariant();
        if (id.Contains("soup")) return CivilSystemKind.SoupKitchen;
        if (id.Contains("nightclub") || id.Contains("night_club") || id.Contains("disco")) return CivilSystemKind.NightClub;
        if (id.Contains("barbershop") || id.Contains("barber_shop") || id.Contains("barber")) return CivilSystemKind.BarberShop;
        if (id.Contains("hotel")) return CivilSystemKind.Hotel;
        if (id == "inn" || id.StartsWith("inn_") || id.EndsWith("_inn") || id.Contains("motel")) return CivilSystemKind.Inn;
        if (id.Contains("fire_station") || id.Contains("firehouse") || id.Contains("fire_dept")) return CivilSystemKind.FireStation;
        if (id.Contains("checkpoint") || id.Contains("military_gate")) return CivilSystemKind.MilitaryCheckpoint;
        if (id.Contains("spy") || id.Contains("intelligence")) return CivilSystemKind.SpyAgency;
        if (id.Contains("embassy") || id.Contains("consulate")) return CivilSystemKind.Embassy;
        if (id.Contains("legislative") || id.Contains("capitol") || id.Contains("parliament")) return CivilSystemKind.GovLegislative;
        if (id.Contains("monarch") || id.Contains("palace") || id.Contains("royal")) return CivilSystemKind.Monarchic;
        if (id == "spa" || id.StartsWith("spa_") || id.EndsWith("_spa") || id.Contains("bathhouse")) return CivilSystemKind.Spa;
        // Unemployment before `_office` — "unemployment_office" would otherwise map as PrivateIndustry.
        if (id.Contains("unemployment") || id.Contains("job_center") || id == "dol"
            || id.Contains("employment_office"))
            return CivilSystemKind.UnemploymentOffice;
        if (id.Contains("private_industry") || id.Contains("office_building") || id.EndsWith("_office") || id.Contains("home_business")) return CivilSystemKind.PrivateIndustry;
        // Bar after barber/hotel to avoid false positives on substrings.
        if (id == "bar" || id.EndsWith("_bar") || id.Contains("tavern") || id.Contains("pub")) return CivilSystemKind.Bar;
        if (id.Contains("restaurant") || id.Contains("kitchen")) return CivilSystemKind.Kitchen;
        if (id.Contains("school")) return CivilSystemKind.School;
        if (id.Contains("police")) return CivilSystemKind.PoliceStation;
        if (id.Contains("prison") || id.Contains("jail") || id.Contains("corrections"))
            return CivilSystemKind.Prison;
        // Church before mall — "church_small" contains substring "mall".
        if (id.Contains("church")) return CivilSystemKind.Church;
        if (id.Contains("library")) return CivilSystemKind.Library;
        if (id.Contains("liquor")) return CivilSystemKind.LiquorStore;
        if (id.Contains("mall")) return CivilSystemKind.Mall;
        if (id.Contains("sanitation") || id.Contains("waste_water") || id.Contains("transfer_station")
            || id.Contains("sewage") || id.Contains("recycling_plant"))
            return CivilSystemKind.SanitationFacility;
        if (id.Contains("factory")) return CivilSystemKind.Factory;
        if (id.Contains("gym")) return CivilSystemKind.Gym;
        if (id.Contains("town_hall") || id.Contains("townhall") || id.Contains("city_hall")) return CivilSystemKind.TownHall;
        if (id.Contains("gas")) return CivilSystemKind.GasStation;
        if (id == "park" || id.StartsWith("park_") || id.EndsWith("_park")
            || id.Contains("city_park") || id.Contains("plaza_park") || id.Contains("greenway"))
            return CivilSystemKind.Park;
        if (id.Contains("house") || id.Contains("home") || id.Contains("residence")) return CivilSystemKind.House;
        if (id.Contains("car_repair") || id.Contains("auto_repair") || id.Contains("garage")) return CivilSystemKind.CarRepair;
        if (id.Contains("bathroom") || id.Contains("restroom")) return CivilSystemKind.Bathroom;
        if (id.Contains("train_station") || id.Contains("rail_station")) return CivilSystemKind.TrainStation;
        if (id.Contains("grain_silo") || id.Contains("silo")) return CivilSystemKind.GrainSilo;
        if (id.Contains("rail_maintenance") || id.Contains("rail_depot") || id.Contains("train_depot"))
            return CivilSystemKind.RailMaintenanceDepot;
        if (id.Contains("transit_hub") || id == "transithub") return CivilSystemKind.TransitHub;
        if (id.Contains("bus_depot") || id.Contains("bus_station") || id == "busdepot")
            return CivilSystemKind.BusDepot;
        if (id.Contains("airport")) return CivilSystemKind.Airport;
        return CivilSystemKind.Generic;
    }
}
