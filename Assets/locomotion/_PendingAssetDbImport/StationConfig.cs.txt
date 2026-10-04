using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Kind-specific station configuration + commodity/assignment snapshots for upload.</summary>
[Serializable]
public sealed class StationConfig
{
    [TextArea(2, 6)] public string notes;
    public RestaurantVenueRuntime kitchenVenue;
    public ComputerPeripheryStation computerStation;
    public TrainStationRuntime trainStation;
    public GrainSiloStubRuntime grainSilo;
    public RailMaintenanceDepotStub railMaintenanceDepot;
    public VotingBoothStation votingBooth;
    public string vehicleId;
    public string vehicleRouteId;
    public string buildingStableId;
    public string consistId;
    public float staffingWeight = 1f;
    public List<StationCommodityEntry> commodities = new List<StationCommodityEntry>();
    public List<StationAssignmentEntry> assignments = new List<StationAssignmentEntry>();
}

[Serializable]
public sealed class StationCommodityEntry
{
    public string commodityKey = "labor";
    [CronExpr] public string cronExpr;
    public float surgeMult = 1f;
    public float quantity = 1f;
    public float price;
    public bool availability = true;
}

[Serializable]
public sealed class StationAssignmentEntry
{
    public string assignType = "persona"; // building | vehicle | persona
    public string refId;
    public string role;
    public int peckingOrder = 100;
}
