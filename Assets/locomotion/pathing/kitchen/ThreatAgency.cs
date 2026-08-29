using System;

/// <summary>Agency ids for ThreatWarden alert / threat tracking.</summary>
public static class ThreatAgencyId
{
    public const string Kitchen = "kitchen";
    public const string BuildingMaintenance = "building_maintenance";
    public const string FireDepartment = "fire_department";
    public const string Police = "police";
    public const string Security = "security";
    public const string Owner = "owner";
}

public enum ThreatAlertLevel
{
    AllClear = 0,
    Advisory = 1,
    OnEdge = 2,
    Elevated = 3,
    UnderAttack = 4
}

public enum ThreatLevel
{
    None = 0,
    PotentialIntruders = 1,
    LocalizedHazard = 2,
    ActiveThreat = 3,
    Critical = 4
}

public enum ThreatKind
{
    Generic,
    SmokeDetectorBattery,
    SmokeDetectorAlarm,
    Fire,
    Intruder,
    GasLeak,
    EquipmentFault,
    Torture
}

public enum JusticeAction
{
    ShutOffHeat,
    Evacuate,
    SecureArea,
    CallAuthorities,
    DisarmHazard
}

[Serializable]
public struct ThreatAgencyState
{
    public string agencyId;
    public ThreatAlertLevel alertLevel;
    public ThreatLevel threatLevel;
    public float alertScore01;
    public float threatScore01;
    public string lemmaTag;
}
