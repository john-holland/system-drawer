using System.Collections.Generic;
using UnityEngine;

public enum TrainDriveKind
{
    Wheels = 0,
    Maglev = 1
}

/// <summary>Authoritative train craft — owns nested cars, limbs, bays, lash, coupling, cabin systems.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Vehicle Ragdoll")]
public sealed class TrainVehicleRagdoll : VehicleRagdoll
{
    [Header("Identity")]
    public string craftName = "Train";
    public string callsign = "CUU-T1";
    public string consistId = "consist_1";
    public string formationGroupId = "train_snake";

    [Header("Consist (this unit is head when cars populated)")]
    public List<TrainVehicleRagdoll> cars = new List<TrainVehicleRagdoll>();
    public bool linkedSegmentMultibody = true;
    public float nominalCouplerSpacingM = 1.2f;
    public int carIndexInConsist;
    public TrainVehicleRagdoll headTrain;

    [Header("Train type")]
    public TrainDriveKind driveKind = TrainDriveKind.Wheels;
    public Transform wheelBarAnchor;
    public float gaugeM = 1.435f;
    public float enginePowerKw = 4000f;
    public float brakePowerKw = 5000f;
    public float startupSec = 8f;
    public float shutdownSec = 6f;
    public bool engineRunning;

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

    [Header("Cabin / pathing")]
    public VehicleSeating seating;
    public List<Transform> seatAnchors = new List<Transform>();
    public string doorOpenCloseTopologyId = "train_door";
    public BehaviorTree doorOpenCloseBt;
    public PlanarSplinePathLocomotion aislePath;
    public PlanarSplinePathLocomotion doorBridgePath;
    public PlanarSplinePathLocomotion caboosePorchPath;
    public PlanarSplinePathLocomotion engineCabinPath;
    public List<VehicleGrabHold> grabHolds = new List<VehicleGrabHold>();
    public List<VehicleStrapHold> strapHolds = new List<VehicleStrapHold>();

    [Header("Telecom")]
    public Component engineerTelecomBridge;
    public bool engineerWebtopMapEnabled = true;
    public Component attendantIntercom;
    public Component passengerWalkie;
    public string cabinMusicTrackId;
    public bool cabinMusicPlaying;

    [Header("Travel")]
    public string railSegmentId;
    public float speedLimitMs = 40f;
    public float currentSpeedMs;

    [Header("Fuel")]
    [Range(0f, 1f)] public float fuel01 = 1f;
    public Transform fuelPort;
    public string fuelPortTopologyId = "fuel01";

    [Header("Seat ticket")]
    public TrainSeatTicketConfig seatTicket;

    TrainCarResultantApi _resultants;
    public TrainCarResultantApi Resultants => _resultants ??= new TrainCarResultantApi(this);

    public float LastLashStable01 => lashRuntime != null ? lashRuntime.LashStable01 : 1f;
    public bool LastFoldFailed { get; set; }
    public TrainVehicleRagdoll Head => cars != null && cars.Count > 0 ? cars[0] : this;
    public TrainVehicleRagdoll Tail => cars != null && cars.Count > 0 ? cars[cars.Count - 1] : this;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "cargo") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "cargo", capacity = 120f });
        if (interiors.Find(s => s != null && s.sectionName == "baggage") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "baggage", capacity = 80f });
        if (coupling == null)
            coupling = GetComponent<TrainCouplingRuntime>() ?? gameObject.AddComponent<TrainCouplingRuntime>();
        coupling.car = this;
        if (lashRuntime == null)
            lashRuntime = GetComponent<CargoLashRuntime>() ?? gameObject.AddComponent<CargoLashRuntime>();
        lashRuntime.bake = defaultBake;
        lashRuntime.mode = defaultStabilityMode;
        if (seating == null)
            seating = GetComponent<VehicleSeating>() ?? GetComponentInChildren<VehicleSeating>();
        if (engineerTelecomBridge == null)
            engineerTelecomBridge = GetComponent("TelecomUnityBridge");
        if (string.IsNullOrEmpty(consistId))
            consistId = gameObject.name;
        EnsureDefaultLimb();
        EnsureDefaultBay();
        EnsureSharedHolds();
        if (cars.Count == 0)
            RebuildCarsFromChildren();
        IndexCars();
        seatTicket?.ApplyTo(this);
    }

    public void EnsureSharedHolds()
    {
        if (grabHolds == null) grabHolds = new List<VehicleGrabHold>();
        if (strapHolds == null) strapHolds = new List<VehicleStrapHold>();
        grabHolds.Clear();
        strapHolds.Clear();
        grabHolds.AddRange(GetComponentsInChildren<VehicleGrabHold>(true));
        strapHolds.AddRange(GetComponentsInChildren<VehicleStrapHold>(true));
        for (int i = 0; i < grabHolds.Count; i++)
            grabHolds[i]?.EnsureCollider();
        for (int i = 0; i < strapHolds.Count; i++)
            strapHolds[i]?.EnsureRope();
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

    public void RebuildCarsFromChildren()
    {
        cars.Clear();
        cars.Add(this);
        var found = GetComponentsInChildren<TrainVehicleRagdoll>(true);
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i] != null && found[i] != this && !cars.Contains(found[i]))
                cars.Add(found[i]);
        }
        IndexCars();
    }

    public void RebuildCarsFromCouplers() => RebuildFromCouplers(this);

    /// <summary>Rebuild this host's car list by walking couplers from <paramref name="seed"/>.</summary>
    public void RebuildFromCouplers(TrainVehicleRagdoll seed)
    {
        cars.Clear();
        var head = WalkToHead(seed != null ? seed : this);
        var cur = head;
        var guard = 0;
        while (cur != null && guard++ < 256)
        {
            if (!cars.Contains(cur)) cars.Add(cur);
            cur = cur.coupling != null && cur.coupling.rearConnected != null
                ? cur.coupling.rearConnected.car
                : null;
        }
        IndexCars();
    }

    static TrainVehicleRagdoll WalkToHead(TrainVehicleRagdoll seed)
    {
        var cur = seed;
        var guard = 0;
        while (cur?.coupling?.frontConnected?.car != null && guard++ < 256)
            cur = cur.coupling.frontConnected.car;
        return cur;
    }

    public void IndexCars()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            if (cars[i] == null) continue;
            cars[i].carIndexInConsist = i;
            cars[i].consistId = consistId;
            cars[i].headTrain = this;
        }
    }

    public void AddCar(TrainVehicleRagdoll car)
    {
        if (car == null || cars.Contains(car)) return;
        cars.Add(car);
        IndexCars();
    }

    public bool RemoveCar(TrainVehicleRagdoll car)
    {
        if (car == null || car == this) return false;
        bool ok = cars.Remove(car);
        if (ok)
        {
            car.coupling?.DecoupleFront();
            car.coupling?.DecoupleRear();
            car.headTrain = null;
            IndexCars();
        }
        return ok;
    }

    public bool ReplaceCar(int index, TrainVehicleRagdoll replacement)
    {
        if (replacement == null || index < 0 || index >= cars.Count) return false;
        var old = cars[index];
        cars[index] = replacement;
        if (old != null && old != replacement)
        {
            old.coupling?.DecoupleFront();
            old.coupling?.DecoupleRear();
            old.headTrain = null;
        }
        IndexCars();
        return true;
    }

    public void InsertCar(int index, TrainVehicleRagdoll car)
    {
        if (car == null) return;
        index = Mathf.Clamp(index, 0, cars.Count);
        if (!cars.Contains(car))
            cars.Insert(index, car);
        IndexCars();
    }

    public void CopySnakeWorldPositions(IReadOnlyList<Vector3> samples)
    {
        if (samples == null || cars == null) return;
        int n = Mathf.Min(cars.Count, samples.Count);
        for (int i = 0; i < n; i++)
        {
            if (cars[i] == null) continue;
            var p = samples[i];
            cars[i].transform.position = new Vector3(p.x, cars[i].transform.position.y, p.z);
            if (i + 1 < n)
            {
                Vector3 dir = samples[i + 1] - samples[i];
                dir.y = 0f;
                if (dir.sqrMagnitude > 1e-4f)
                    cars[i].transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }
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
        Notify(TrainCarNarrativeActionIds.UnfoldLimb);
        ApplyLimbLash(limb);
        return true;
    }

    public bool TryRefoldLimb(string limbId)
    {
        var limb = FindLimb(limbId) ?? (limbs.Count > 0 ? limbs[0] : null);
        if (limb == null) return false;
        limb.state = TrainCarLimbState.Folded;
        LastFoldFailed = false;
        Notify(TrainCarNarrativeActionIds.RefoldLimb);
        ApplyLimbLash(limb);
        return true;
    }

    public void MarkFoldFailed(string limbOrBayId)
    {
        LastFoldFailed = true;
        var limb = FindLimb(limbOrBayId);
        if (limb != null) limb.state = TrainCarLimbState.Failed;
        Notify(TrainCarNarrativeActionIds.FoldFailed);
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
        Notify(TrainCarNarrativeActionIds.ParkVehicle);
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
        Notify(TrainCarNarrativeActionIds.UnloadBay);
        return true;
    }

    public void SetBayRampUnfolded(string bayId, bool unfolded)
    {
        var bay = FindBay(bayId);
        if (bay != null) bay.rampUnfolded = unfolded;
    }

    public void SetEngineRunning(bool running)
    {
        engineRunning = running;
        Notify(running ? TrainDispatchNarrativeIds.EngineStart : TrainDispatchNarrativeIds.EngineStop);
    }

    public void SetCabinMusic(string trackId, bool play)
    {
        cabinMusicTrackId = trackId;
        cabinMusicPlaying = play;
    }

    public void SetCabinLocked(bool locked)
    {
        Notify(locked ? TrainDispatchNarrativeIds.CabinLock : TrainDispatchNarrativeIds.CabinUnlock);
    }

    public void RebuildPlanarPaths()
    {
        aislePath?.Rebuild();
        doorBridgePath?.Rebuild();
        caboosePorchPath?.Rebuild();
        engineCabinPath?.Rebuild();
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

    void Notify(string id) =>
        SendMessage("OnNarrativeSchedulerAction", id ?? "", SendMessageOptions.DontRequireReceiver);

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

public static class TrainDispatchNarrativeIds
{
    public const string EngineStart = "train_engine_start";
    public const string EngineStop = "train_engine_stop";
    public const string CabinLock = "train_cabin_lock";
    public const string CabinUnlock = "train_cabin_unlock";
    public const string SpeedAdjust = "train_speed_adjust";
    public const string Plow = "train_plow";
    public const string FollowTrain = "train_follow";
    public const string Turnstile = "train_turnstile";
}
