using System.Collections.Generic;
using UnityEngine;

/// <summary>Elevator cab — VehicleInterior, optional bath/kitchen/houses/vehicles, door topology, PixelLight panel.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Elevators/Elevator Vehicle Ragdoll")]
public sealed class ElevatorVehicleRagdoll : VehicleRagdoll
{
    [Header("Cab")]
    public string craftName = "Elevator";
    public int currentFloor;
    public int minFloor;
    public int maxFloor = 10;
    public string doorOpenCloseTopologyId = "elevator_door";
    public BehaviorTree doorOpenCloseBt;
    public bool doorsOpen;

    [Header("Interior options")]
    public bool hasBathroom;
    public bool hasKitchen;
    public Transform bathroomAnchor;
    public RestaurantVenueRuntime kitchen;
    public List<TrainCarContainmentBay> nestedBays = new List<TrainCarContainmentBay>();
    public List<TrainCarAmbulationLimb> limbs = new List<TrainCarAmbulationLimb>();

    [Header("PixelLight call panel")]
    public PixelLightGridMountGameObject buttonPanelMount;
    public ElevatorButtonPanel buttonPanel;
    public PlanarSplinePathLocomotion cabFloorPath;

    [Header("SG")]
    public string sg3dShaftNodeId;
    public string sg4dCausalityLeafId;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "cabin") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "cabin", capacity = 20f });
        if (buttonPanel == null)
            buttonPanel = GetComponent<ElevatorButtonPanel>() ?? gameObject.AddComponent<ElevatorButtonPanel>();
        buttonPanel.elevator = this;
        if (buttonPanelMount != null)
            buttonPanel.mount = buttonPanelMount;
    }

    public void SetDoorsOpen(bool open)
    {
        doorsOpen = open;
        SendMessage("OnNarrativeSchedulerAction",
            open ? ElevatorNarrativeIds.DoorOpen : ElevatorNarrativeIds.DoorClose,
            SendMessageOptions.DontRequireReceiver);
    }

    public bool CallFloor(int floor)
    {
        if (floor < minFloor || floor > maxFloor) return false;
        currentFloor = floor;
        SendMessage("OnNarrativeSchedulerAction", ElevatorNarrativeIds.CallFloor,
            SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public bool TryParkNested(VehicleRagdoll vehicle, string bayId = null)
    {
        var bay = nestedBays.Find(b => b != null && (bayId == null || b.bayId == bayId));
        if (bay == null || vehicle == null || !bay.HasRoom) return false;
        bay.containedVehicles.Add(vehicle);
        if (bay.parkAnchor != null)
            vehicle.transform.SetParent(bay.parkAnchor, true);
        return true;
    }
}

public static class ElevatorNarrativeIds
{
    public const string DoorOpen = "elevator_door_open";
    public const string DoorClose = "elevator_door_close";
    public const string CallFloor = "elevator_call_floor";
}
