using System;
using System.Collections.Generic;
using UnityEngine;

public enum GearboxDriveKind
{
    GearGear = 0,
    Belt = 1,
    ChainLinkBelt = 2
}

[Serializable]
public sealed class GearboxConnection
{
    public string label = "pair";
    public GearboxDriveKind kind = GearboxDriveKind.GearGear;
    public int drivingTeeth = 12;
    public int drivenTeeth = 24;
    public float drivingPulleyM = 0.08f;
    public float drivenPulleyM = 0.16f;
    public GarageChainSpec chainBelt;
    public int drivingCellX;
    public int drivingCellY;
    public int drivenCellX;
    public int drivenCellY;

    public float Ratio
    {
        get
        {
            if (kind == GearboxDriveKind.Belt)
                return Mathf.Max(0.01f, drivenPulleyM) / Mathf.Max(0.01f, drivingPulleyM);
            return Mathf.Max(1, drivenTeeth) / (float)Mathf.Max(1, drivingTeeth);
        }
    }

    public float BeltPathLengthM()
    {
        float a = Mathf.Max(0.01f, drivingPulleyM) * 0.5f;
        float b = Mathf.Max(0.01f, drivenPulleyM) * 0.5f;
        float c = Mathf.Max(0.02f, Mathf.Abs(b - a) + 0.12f);
        return Mathf.PI * (a + b) + 2f * c;
    }

    public float ChainBeltPathLengthM()
    {
        if (chainBelt != null)
            return Mathf.Max(0.2f, chainBelt.totalLengthM);
        return BeltPathLengthM() * 1.05f;
    }
}

[CreateAssetMenu(fileName = "Gearbox", menuName = "Locomotion/Civil/Gearbox")]
public sealed class GearboxSpec : ScriptableObject
{
    public PixelLightMultiSlotCatalog catalog;
    public List<GearboxConnection> connections = new List<GearboxConnection>();

    public GearboxConnection AddGearPair(int drivingTeeth, int drivenTeeth)
    {
        var c = new GearboxConnection
        {
            kind = GearboxDriveKind.GearGear,
            drivingTeeth = Mathf.Max(3, drivingTeeth),
            drivenTeeth = Mathf.Max(3, drivenTeeth)
        };
        connections.Add(c);
        return c;
    }
}
