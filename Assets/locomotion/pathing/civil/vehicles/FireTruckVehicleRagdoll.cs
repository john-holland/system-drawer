using System.Collections.Generic;
using UnityEngine;

/// <summary>Fire apparatus: rear steer, dual driver seats, water tank, hose spindle, PixelLight mounts.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Fire Truck Vehicle Ragdoll")]
public sealed class FireTruckVehicleRagdoll : VehicleRagdoll
{
    public bool rearSteering = true;
    public bool railMode;
    public Transform driverSeat;
    public Transform secondaryDriverSeat;
    public List<Transform> extraSeats = new List<Transform>();
    public float waterTankLiters = 1500f;
    public float hoseSpindleWoundMeters = 60f;
    public float hoseWindRateMps = 2f;
    public bool sirenOn;
    public List<PixelLightRig> lightMounts = new List<PixelLightRig>();
    public RopeSystem hoseRope;
    public GameObject dispatchTarget;
    public VehicleInstrumentPhysicsProxy instrumentProxy;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "hose_bed") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "hose_bed", capacity = 80f });
        if (lightMounts.Count == 0)
            GetComponentsInChildren(true, lightMounts);
        if (instrumentProxy == null)
            instrumentProxy = GetComponent<VehicleInstrumentPhysicsProxy>();
    }

    public void DispatchToFire(GameObject target, float waterDemandHint)
    {
        available = false;
        dispatchTarget = target;
        SetSiren(true);
        for (int i = 0; i < lightMounts.Count; i++)
        {
            if (lightMounts[i] == null) continue;
            lightMounts[i].syncMode = PixelLightSyncMode.Free;
            lightMounts[i].playing = true;
            if (lightMounts[i].pattern == null)
                lightMounts[i].SetPattern(PixelLightPatternAsset.CreateChasePreset());
        }
        if (target != null)
        {
            var ta = GetComponent<TravelAgent>();
            if (ta != null)
                ta.previewGoalWorld = target.transform.position;
        }
        // Daisy-chain hint: reserve tank against demand
        waterTankLiters = Mathf.Max(200f, waterTankLiters);
    }

    public void SetSiren(bool on)
    {
        sirenOn = on;
        SendMessage(on ? "OnFireTruckSirenOn" : "OnFireTruckSirenOff", this, SendMessageOptions.DontRequireReceiver);
    }

    public void WindHose(float dt, bool outWard)
    {
        float delta = hoseWindRateMps * dt * (outWard ? -1f : 1f);
        hoseSpindleWoundMeters = Mathf.Clamp(hoseSpindleWoundMeters + delta, 0f, 120f);
        if (hoseRope != null)
            hoseRope.SetWindRate(outWard ? hoseWindRateMps : -hoseWindRateMps);
    }

    public void EnsureDefaultLights()
    {
        if (lightMounts.Count > 0) return;
        var go = PixelLightPrefabFactory.CreateDefaultRuntime(transform);
        go.name = "EmergencyBar";
        go.transform.localPosition = new Vector3(0f, 2.2f, 0.2f);
        lightMounts.Add(go.GetComponent<PixelLightRig>());
    }
}
