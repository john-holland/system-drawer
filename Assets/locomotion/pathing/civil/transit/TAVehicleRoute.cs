using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Named transit route with service + maintenance cron and ordered stops.</summary>
[Serializable]
public sealed class TAVehicleRoute
{
    public string routeId;
    public string vehicleId;
    public string label;
    public string serviceCron = "* 6-22 * * 1-5";
    public string maintenanceCron = "0 2 * * 0";
    public List<string> stopIds = new List<string>();
    public List<Vector3> stopWorld = new List<Vector3>();
    public bool enabled = true;

    public bool IsServiceDue(DateTime utcNow) =>
        enabled && CronDue.IsActiveSchedule(serviceCron, utcNow);

    public bool IsMaintenanceDue(DateTime utcNow) =>
        enabled && CronDue.IsActiveSchedule(maintenanceCron, utcNow);
}

/// <summary>Canonical DispatchRequest.kind strings for transportation authority.</summary>
public static class TADispatchKinds
{
    public const string Reroute = "ta_reroute";
    public const string Halt = "ta_halt";
    public const string Recall = "ta_recall";
    public const string Hours = "ta_hours";
    public const string ReleasePassengers = "ta_release_passengers";
    public const string Speak = "ta_speak";
    public const string Music = "ta_music";
    public const string SoundDesign = "ta_sound_design";
    public const string Park = "ta_park";
    public const string BayPark = "ta_bay_park";
    public const string BayRepair = "ta_bay_repair";
    public const string Fuel = "ta_fuel";
    public const string Schedule = "ta_schedule";
    public const string Route = "ta_route";
    public const string MaintenanceRequest = "ta_maintenance_request";
    public const string RoadWorkRequest = "ta_road_work_request";

    public const string TransportationAuthority = "transportation_authority";
    public const string MissionControl = "mission_control";
    public const string AirTrafficControl = "air_traffic_control";
}

public enum TAPassengerReleaseMode
{
    OnSpotAsap = 0,
    NextStop = 1
}
