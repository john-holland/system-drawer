using System.Collections.Generic;
using UnityEngine;

/// <summary>Rail car — ambulation limbs, containment bays, lash stability, coupling ends.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Car Vehicle Ragdoll")]
public sealed class TrainCarVehicleRagdoll : VehicleRagdoll
{
    [Header("Identity")]
    public string craftName = "TrainCar";
    public string callsign = "CUU-T1";
    public string consistId;

    [Header("Coupling")]
    public TrainCouplingRuntime coupling;
    public Transform frontCoupler;
    public Transform rearCoupler;

    [Header("Composition")]
    public List<TrainCarAmbulationLimb> limbs = new List<TrainCarAmbulationLimb>();
    public List<TrainCarContainmentBay> containmentBays = new List<TrainCarContainmentBay>();
    public CargoLashRuntime lashRuntime;
    public CargoStabilityBakeAsset defaultBake;
    public CargoStabilityMode defaultStabilityMode = CargoStabilityMode.Nominal;

    [Header("Travel")]
    public string railSegmentId;
    public int carIndexInConsist;
    public TrainConsistRuntime consist;

    TrainCarResultantApi _resultants;
    public TrainCarResultantApi Resultants => _resultants ??= new TrainCarResultantApi(this);

    public float LastLashStable01 => lashRuntime != null ? lashRuntime.LashStable01 : 1f;
    public bool LastFoldFailed { get; set; }

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "cargo") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "cargo", capacity = 120f });
        if (coupling == null)
            coupling = GetComponent<TrainCouplingRuntime>() ?? gameObject.AddComponent<TrainCouplingRuntime>();
        coupling.car = this;
        if (lashRuntime == null)
            lashRuntime = GetComponent<CargoLashRuntime>() ?? gameObject.AddComponent<CargoLashRuntime>();
        lashRuntime.bake = defaultBake;
        lashRuntime.mode = defaultStabilityMode;
        EnsureDefaultLimb();
        EnsureDefaultBay();
    }

    void EnsureDefaultLimb()
    {
        if (limbs.Count > 0) return;
        limbs.Add(new TrainCarAmbulationLimb
        {
            limbId = "main_crane",
            role = TrainCarLimbRole.Crane,
            openCloseTopologyId = "train_limb_crane"
        });
    }

    void EnsureDefaultBay()
    {
        if (containmentBays.Count > 0) return;
        containmentBays.Add(new TrainCarContainmentBay
        {
            bayId = "deck",
            kind = TrainCarBayKind.Vehicle,
            capacity = 2,
            parkAnchor = transform,
            deckRoot = transform
        });
    }

    public TrainCarAmbulationLimb FindLimb(string limbId)
    {
        for (int i = 0; i < limbs.Count; i++)
            if (limbs[i] != null && limbs[i].limbId == limbId)
                return limbs[i];
        return null;
    }

    public TrainCarContainmentBay FindBay(string bayId)
    {
        for (int i = 0; i < containmentBays.Count; i++)
            if (containmentBays[i] != null && containmentBays[i].bayId == bayId)
                return containmentBays[i];
        return containmentBays.Count > 0 ? containmentBays[0] : null;
    }

    public bool TryUnfoldLimb(string limbId)
    {
        var limb = FindLimb(limbId) ?? (limbs.Count > 0 ? limbs[0] : null);
        if (limb == null) return false;
        limb.state = TrainCarLimbState.Unfolded;
        LastFoldFailed = false;
        SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.UnfoldLimb,
            SendMessageOptions.DontRequireReceiver);
        ApplyLimbLash(limb);
        return true;
    }

    public bool TryRefoldLimb(string limbId)
    {
        var limb = FindLimb(limbId) ?? (limbs.Count > 0 ? limbs[0] : null);
        if (limb == null) return false;
        limb.state = TrainCarLimbState.Folded;
        LastFoldFailed = false;
        SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.RefoldLimb,
            SendMessageOptions.DontRequireReceiver);
        ApplyLimbLash(limb);
        return true;
    }

    public void MarkFoldFailed(string limbOrBayId)
    {
        LastFoldFailed = true;
        var limb = FindLimb(limbOrBayId);
        if (limb != null) limb.state = TrainCarLimbState.Failed;
        SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.FoldFailed,
            SendMessageOptions.DontRequireReceiver);
    }

    public bool TryParkVehicle(VehicleRagdoll vehicle, string bayId = null)
    {
        var bay = FindBay(bayId);
        if (bay == null || vehicle == null || !bay.HasRoom) return false;
        if (!bay.containedVehicles.Contains(vehicle))
            bay.containedVehicles.Add(vehicle);
        if (bay.parkAnchor != null)
        {
            vehicle.transform.SetParent(bay.parkAnchor, true);
            vehicle.transform.localPosition = Vector3.zero;
            vehicle.transform.localRotation = Quaternion.identity;
        }
        var rb = vehicle.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
        ApplyBayLash(bay, vehicle);
        SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.ParkVehicle,
            SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public bool TryUnloadVehicle(VehicleRagdoll vehicle, string bayId = null)
    {
        var bay = FindBay(bayId);
        if (bay == null || vehicle == null) return false;
        bay.containedVehicles.Remove(vehicle);
        bay.rampUnfolded = true;
        vehicle.transform.SetParent(null, true);
        var rb = vehicle.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;
        SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.UnloadBay,
            SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public void SetBayRampUnfolded(string bayId, bool unfolded)
    {
        var bay = FindBay(bayId);
        if (bay != null) bay.rampUnfolded = unfolded;
    }

    void ApplyLimbLash(TrainCarAmbulationLimb limb)
    {
        if (lashRuntime == null || limb == null) return;
        lashRuntime.deckRoot = limb.limbRoot != null ? limb.limbRoot : transform;
        lashRuntime.mode = limb.stabilityMode;
        lashRuntime.ApplyProfile(limb.lashProfile, limb.stabilityMode);
        lashRuntime.TickEvaluate(Vector3.zero);
    }

    void ApplyBayLash(TrainCarContainmentBay bay, VehicleRagdoll vehicle)
    {
        if (lashRuntime == null || bay == null) return;
        lashRuntime.deckRoot = bay.deckRoot != null ? bay.deckRoot : transform;
        lashRuntime.cargoBody = vehicle != null ? vehicle.GetComponent<Rigidbody>() : null;
        lashRuntime.mode = bay.stabilityMode;
        lashRuntime.bake = defaultBake;
        lashRuntime.ApplyProfile(bay.lashProfile, bay.stabilityMode);
        lashRuntime.TickEvaluate(Vector3.zero);
    }

    public Dictionary<string, object> LemmaSnapshot()
    {
        var limb = limbs.Count > 0 ? limbs[0] : null;
        var bay = containmentBays.Count > 0 ? containmentBays[0] : null;
        return new Dictionary<string, object>
        {
            [TrainCarLemmaPropertyKeys.ConsistId] = consistId ?? "",
            [TrainCarLemmaPropertyKeys.LimbState] = limb != null ? limb.state.ToString() : "",
            [TrainCarLemmaPropertyKeys.LimbRole] = limb != null ? limb.role.ToString() : "",
            [TrainCarLemmaPropertyKeys.BayId] = bay != null ? bay.bayId : "",
            [TrainCarLemmaPropertyKeys.ContainedVehicle] =
                bay != null && bay.containedVehicles.Count > 0 && bay.containedVehicles[0] != null
                    ? bay.containedVehicles[0].vehicleId
                    : "",
            [TrainCarLemmaPropertyKeys.LashStable01] = LastLashStable01,
            [TrainCarLemmaPropertyKeys.ImpossibleKeepStable] =
                defaultStabilityMode == CargoStabilityMode.ImpossibleKeepStable,
            [TrainCarLemmaPropertyKeys.StabilityMode] = defaultStabilityMode.ToString(),
            [TrainCarLemmaPropertyKeys.FoldFailed] = LastFoldFailed
        };
    }
}
