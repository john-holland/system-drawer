using System;
using UnityEngine;

public enum TrayPlaceMode
{
    HoldIk,
    TrayPlaceSetting,
    ExistingTablePlacement
}

public enum TrayServeBailReason
{
    None,
    TrayDropped,
    AlreadyEaten,
    PlaceWaypointCovered
}

[Serializable]
public sealed class TrayBinSettings
{
    public GameObject trayPrefab;
    public int maxPlateSlots = 4;
    public float maxMass = 8f;
    public int maxCount = 4;
    public bool allowSinglePersonLoads = true;
    public bool allowSansTrayFallback = true;
    public TrayPlaceMode placeMode = TrayPlaceMode.TrayPlaceSetting;
    public Transform placeWaypoint;
}
