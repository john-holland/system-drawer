using System.Collections.Generic;
using UnityEngine;

/// <summary>Garbage truck — hopper SPH compaction, passenger seats, fork lifter arm proxy + IK empty.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Garbage Truck Vehicle Ragdoll")]
public sealed class GarbageTruckVehicleRagdoll : VehicleRagdoll
{
    public Transform driverSeat;
    public List<Transform> passengerSeats = new List<Transform>();
    public GarbageBag hopper = new GarbageBag();
    public Transform hopperAnchor;
    public Transform lifterArmAnchor;
    public VehicleInstrumentPhysicsProxy lifterArmProxy;
    public BehaviorTree compressionBt;
    public BehaviorTree lifterArmBt;
    public GameObject dispatchTarget;
    public bool compactionActive;
    public TrainCarAmbulationLimb lifterLimb;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "hopper") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "hopper", capacity = 200f });
        if (lifterArmProxy == null)
            lifterArmProxy = GetComponent<VehicleInstrumentPhysicsProxy>();
        if (hopper != null)
            hopper.RebuildParticlesFromMass();
    }

    void Update()
    {
        if (compactionActive && hopper != null)
            hopper.TickSphCompaction(Time.deltaTime);
    }

    public void DispatchToPickup(GameObject target)
    {
        available = false;
        dispatchTarget = target;
        if (target != null)
        {
            var ta = GetComponent<TravelAgent>();
            if (ta != null)
                ta.previewGoalWorld = target.transform.position;
        }
        SendMessage("OnGarbageTruckDispatch", target, SendMessageOptions.DontRequireReceiver);
    }

    public void SetCompactionActive(bool on)
    {
        compactionActive = on;
        if (compressionBt != null)
            compressionBt.SendMessage(on ? "OnCompactionStart" : "OnCompactionStop", this,
                SendMessageOptions.DontRequireReceiver);
    }

    public bool LiftBin(TrashBinRuntime bin)
    {
        if (bin == null || !bin.forkLiftable) return false;
        if (lifterArmBt != null)
            lifterArmBt.SendMessage("OnForkLiftBin", bin, SendMessageOptions.DontRequireReceiver);
        else if (lifterLimb != null)
            TryUnfoldLifter();
        return true;
    }

    public bool TryUnfoldLifter()
    {
        if (lifterLimb == null) return false;
        SendMessage("TryUnfoldLimb", lifterLimb, SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public float ShakeBinIntoHopper(TrashBinRuntime bin, TrashWarden warden)
    {
        if (warden != null && !warden.ShouldShakeOut(bin))
            return 0f;
        if (bin == null) return 0f;
        return bin.EmptyInto(hopper);
    }
}
