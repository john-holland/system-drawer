using System.Collections.Generic;
using UnityEngine;

/// <summary>Police cruiser — lights, weapons chest telecom policy, aim/takedown stubs.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Police Car Vehicle Ragdoll")]
public sealed class PoliceCarVehicleRagdoll : VehicleRagdoll
{
    public List<PixelLightRig> lightMounts = new List<PixelLightRig>();
    public bool lightsOn;
    public bool hasWeaponsChest = true;
    public bool requiresTelecomForWeapons = true;
    public string pendingWeaponCode;
    public bool weaponsChestUnlocked;
    public Transform driverSeat;
    public VehicleInstrumentPhysicsProxy instrumentProxy;
    [Header("Weapon aim / takedown stubs")]
    public float aimYawDeg;
    public float aimPitchDeg;
    [Range(0f, 1f)] public float takedownPosturing01 = 0.5f;
    public GameObject takedownTargetVehicle;

    protected override void Awake()
    {
        base.Awake();
        if (interiors.Find(s => s != null && s.sectionName == "trunk") == null)
            interiors.Add(new VehicleInventorySection { sectionName = "trunk", capacity = 30f });
        if (lightMounts.Count == 0)
            GetComponentsInChildren(true, lightMounts);
        if (instrumentProxy == null)
            instrumentProxy = GetComponent<VehicleInstrumentPhysicsProxy>();
    }

    public void SetLights(bool on)
    {
        lightsOn = on;
        for (int i = 0; i < lightMounts.Count; i++)
        {
            if (lightMounts[i] == null) continue;
            lightMounts[i].syncMode = PixelLightSyncMode.Free;
            lightMounts[i].playing = on;
            if (on)
            {
                if (lightMounts[i].pattern == null)
                    lightMounts[i].SetPattern(PixelLightPatternAsset.CreateChasePreset());
                lightMounts[i].colorPackage = PixelLightColorPackage.CreateSignal(new Color(0.2f, 0.35f, 1f));
            }
            lightMounts[i].SetEnabledEmission(on);
        }
        SendMessage(on ? "OnPoliceLightsOn" : "OnPoliceLightsOff", this, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>
    /// If telecom required, mint a code (dispatch must confirm); else unlock immediately.
    /// </summary>
    public bool TryOpenWeaponsChest(out string telecomCode)
    {
        telecomCode = null;
        if (!hasWeaponsChest)
        {
            weaponsChestUnlocked = true;
            return true;
        }
        if (!requiresTelecomForWeapons)
        {
            weaponsChestUnlocked = true;
            return true;
        }
        pendingWeaponCode = Random.Range(1000, 9999).ToString();
        telecomCode = pendingWeaponCode;
        CentralDispatchHub.Instance?.RequestCrossDispatch(
            "police",
            "police",
            new DispatchRequest
            {
                kind = "confirm",
                notes = "weapon_chest_code:" + pendingWeaponCode,
                priority01 = 0.6f
            });
        return false;
    }

    public bool ConfirmWeaponCode(string code)
    {
        if (!requiresTelecomForWeapons || !hasWeaponsChest)
        {
            weaponsChestUnlocked = true;
            return true;
        }
        if (code == pendingWeaponCode)
        {
            weaponsChestUnlocked = true;
            pendingWeaponCode = null;
            return true;
        }
        return false;
    }

    public void SetAimFromInput(float mouseOrStickX, float mouseOrStickY)
    {
        aimYawDeg = mouseOrStickX * 90f;
        aimPitchDeg = mouseOrStickY * 45f;
    }

    public void BeginTakedown(GameObject targetVehicle, float posturing01)
    {
        takedownTargetVehicle = targetVehicle;
        takedownPosturing01 = Mathf.Clamp01(posturing01);
        // Stretch: crash physics via instrument proxy / impulse.
        SendMessage("OnPoliceTakedownStub", this, SendMessageOptions.DontRequireReceiver);
    }

    /// <summary>Release cruiser onto a traffic detail (lights + TravelAgent goal).</summary>
    public void DispatchToDetail(GameObject target, string notes)
    {
        available = false;
        SetLights(true);
        Vector3 goal = target != null ? target.transform.position : transform.position + transform.forward * 12f;
        var ta = GetComponent<TravelAgent>();
        if (ta != null)
        {
            ta.previewGoalWorld = goal;
            ta.ApplyAvoidHintsFromWarden();
            ta.RebuildCachedPlan();
        }

        var warden = TrafficWarden.Instance ?? FindFirstObjectByType<TrafficWarden>();
        warden?.RegisterAvoidSource(this);
        warden?.BeginPoliceDetail(goal);
        SendMessage("OnPoliceDispatchToDetail", notes ?? "", SendMessageOptions.DontRequireReceiver);
    }

    public void ClearDetailDispatch()
    {
        available = true;
        SetLights(false);
        var warden = TrafficWarden.Instance ?? FindFirstObjectByType<TrafficWarden>();
        warden?.UnregisterAvoidSource(transform);
        warden?.stateMachine.ClearPoliceDetail();
    }

    public void EnsureDefaultLights()
    {
        if (lightMounts.Count > 0) return;
        var go = PixelLightPrefabFactory.CreateDefaultRuntime(transform);
        go.name = "PoliceLightBar";
        go.transform.localPosition = new Vector3(0f, 1.6f, 0.1f);
        lightMounts.Add(go.GetComponent<PixelLightRig>());
    }
}
