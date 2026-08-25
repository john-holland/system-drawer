using UnityEngine;

public enum EmergencyWarningBarKind
{
    Police = 0,
    Fire = 1,
    Ems = 2,
    Utility = 3
}

/// <summary>Roof-mount wig-wag PixelLight bar for police / fire / EMS / utility.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vehicles/Emergency Warning Bar")]
public sealed class EmergencyWarningBar : MonoBehaviour
{
    public PixelLightRig leftBank;
    public PixelLightRig rightBank;
    public PixelLightRig rearBank;
    public EmergencyWarningBarKind kind = EmergencyWarningBarKind.Police;
    public float blinkHz = 2f;
    public bool barOn;
    public EmergencyVehiclePresence presence;

    public void SetKind(EmergencyWarningBarKind k)
    {
        kind = k;
        var left = ColorForKind(k, true);
        var right = ColorForKind(k, false);
        if (leftBank != null)
        {
            leftBank.colorPackage = left;
            leftBank.pattern = PixelLightPatternAsset.CreateWigWagPreset(true);
            leftBank.syncMode = PixelLightSyncMode.Free;
        }
        if (rightBank != null)
        {
            rightBank.colorPackage = right;
            rightBank.pattern = PixelLightPatternAsset.CreateWigWagPreset(false);
            rightBank.syncMode = PixelLightSyncMode.Free;
        }
    }

    public void SetOn(bool on)
    {
        barOn = on;
        if (leftBank != null)
        {
            leftBank.playing = on;
            leftBank.SetEnabledEmission(on);
        }
        if (rightBank != null)
        {
            rightBank.playing = on;
            rightBank.SetEnabledEmission(on);
        }
        if (rearBank != null)
        {
            rearBank.playing = on;
            rearBank.SetEnabledEmission(on);
        }
        if (presence == null)
            presence = GetComponent<EmergencyVehiclePresence>() ?? gameObject.AddComponent<EmergencyVehiclePresence>();
        presence.bar = this;
        presence.enabled = on;
        if (on)
        {
            var vehicle = GetComponentInParent<VehicleRagdoll>();
            TrafficWarden.Instance?.RegisterAvoidSource(transform);
        }
    }

    public static EmergencyWarningBar EnsureOnVehicle(VehicleRagdoll vehicle)
    {
        if (vehicle == null) return null;
        var bar = vehicle.GetComponentInChildren<EmergencyWarningBar>();
        if (bar != null) return bar;
        var go = PixelLightPrefabFactory.CreateWarningBarRuntime(vehicle.transform);
        go.name = "EmergencyWarningBar";
        var renderer = vehicle.GetComponentInChildren<Renderer>();
        float y = renderer != null ? renderer.bounds.max.y - vehicle.transform.position.y + 0.08f : 1.6f;
        go.transform.localPosition = new Vector3(0f, y, 0.1f);
        bar = go.GetComponent<EmergencyWarningBar>() ?? go.AddComponent<EmergencyWarningBar>();
        bar.leftBank = go.transform.Find("LeftBank")?.GetComponent<PixelLightRig>();
        bar.rightBank = go.transform.Find("RightBank")?.GetComponent<PixelLightRig>();
        bar.SetKind(bar.kind);
        return bar;
    }

    public static PixelLightColorPackage ColorForKind(EmergencyWarningBarKind k, bool left)
    {
        switch (k)
        {
            case EmergencyWarningBarKind.Police:
                return left ? PixelLightColorPackage.CreateEmergencyRed() : PixelLightColorPackage.CreateEmergencyBlue();
            case EmergencyWarningBarKind.Utility:
                return PixelLightColorPackage.CreateAmberCaution();
            default:
                return left ? PixelLightColorPackage.CreateEmergencyRed() : PixelLightColorPackage.CreateSignal(Color.white);
        }
    }
}
