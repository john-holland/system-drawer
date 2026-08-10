using System;
using System.Collections.Generic;
using UnityEngine;

public enum TrainCarLimbRole
{
    Generic = 0,
    Crane = 1,
    DigArm = 2,
    Loader = 3
}

public enum TrainCarLimbState
{
    Folded = 0,
    Unfolding = 1,
    Unfolded = 2,
    Refolding = 3,
    Failed = 4
}

public enum TrainCarBayKind
{
    Vehicle = 0,
    BulkCommodity = 1,
    Mixed = 2
}

public enum TrainCarCloseMode
{
    RefoldLimb = 0,
    ParkContainedVehicle = 1,
    Both = 2
}

public enum CargoStabilityMode
{
    Nominal = 0,
    SoftLash = 1,
    ImpossibleKeepStable = 2
}

[Serializable]
public sealed class TrainCarAmbulationLimb
{
    public string limbId;
    public TrainCarLimbRole role = TrainCarLimbRole.Crane;
    public TrainCarLimbState state = TrainCarLimbState.Folded;
    public string openCloseTopologyId = "train_limb";
    public BehaviorTree openCloseBt;
    public Transform limbRoot;
    public VehiclePartBase partHost;
    public CargoLashProfile lashProfile;
    public CargoStabilityMode stabilityMode = CargoStabilityMode.Nominal;

    public bool IsUnfolded => state == TrainCarLimbState.Unfolded;
    public bool IsFolded => state == TrainCarLimbState.Folded;
}

[Serializable]
public sealed class TrainCarContainmentBay
{
    public string bayId;
    public TrainCarBayKind kind = TrainCarBayKind.Vehicle;
    public int capacity = 1;
    public string unloadOpenCloseTopologyId = "train_bay_ramp";
    public BehaviorTree unloadBt;
    public Transform parkAnchor;
    public Transform deckRoot;
    public List<VehicleRagdoll> containedVehicles = new List<VehicleRagdoll>();
    public List<VehicleActor> containedActors = new List<VehicleActor>();
    public CargoLashProfile lashProfile;
    public CargoStabilityMode stabilityMode = CargoStabilityMode.Nominal;
    public bool rampUnfolded;
    public string bulkCommodityKey;
    public float bulkQuantity;

    public int Occupancy =>
        (containedVehicles != null ? containedVehicles.Count : 0)
        + (containedActors != null ? containedActors.Count : 0);

    public bool HasRoom => Occupancy < Mathf.Max(1, capacity);
}

[Serializable]
public sealed class CargoLashJointSpec
{
    public string jointId;
    public Transform anchorA;
    public Transform anchorB;
    public float breakForce = 5000f;
    public float breakTorque = 5000f;
}

[Serializable]
public sealed class CargoLashRopeSpec
{
    public string ropeId;
    public Transform deckAnchor;
    public Transform cargoAnchor;
    public RopeConfig ropeConfig = new RopeConfig();
}
