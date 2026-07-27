using System;
using UnityEngine;

/// <summary>Dental tooth kinds (FDI-style naming, default adult set).</summary>
public enum ToothKind
{
    CentralIncisor,
    LateralIncisor,
    Canine,
    FirstPremolar,
    SecondPremolar,
    FirstMolar,
    SecondMolar,
    Wisdom
}

public enum ToothZone
{
    Front,
    MolarBack
}

public enum ToothArch
{
    Upper,
    Lower
}

public enum ToothSide
{
    Left,
    Right,
    Center
}

/// <summary>One tooth placement on upper/lower 3D spline curves.</summary>
[Serializable]
public sealed class ToothSlot
{
    public ToothKind kind = ToothKind.CentralIncisor;
    public ToothZone zone = ToothZone.Front;
    public ToothArch arch = ToothArch.Upper;
    public ToothSide side = ToothSide.Left;
    [Range(0f, 1f)] public float stop01 = 0.5f;
    public Vector3 biteOffset;
    public Mesh staticMesh;
    public UnityEngine.Object sdfComposition;
    public bool present = true;

    public static ToothZone ZoneFor(ToothKind kind)
    {
        switch (kind)
        {
            case ToothKind.CentralIncisor:
            case ToothKind.LateralIncisor:
            case ToothKind.Canine:
                return ToothZone.Front;
            default:
                return ToothZone.MolarBack;
        }
    }

    public static ToothSlot CreateDefault(ToothKind kind, ToothArch arch, ToothSide side, float stop01)
    {
        return new ToothSlot
        {
            kind = kind,
            zone = ZoneFor(kind),
            arch = arch,
            side = side,
            stop01 = Mathf.Clamp01(stop01),
            present = true
        };
    }
}

/// <summary>Canonical adult dentition (32 slots) with default front/molar zones.</summary>
public static class ToothCatalog
{
    public static ToothSlot[] BuildDefaultAdultSet()
    {
        var list = new System.Collections.Generic.List<ToothSlot>(32);
        // Upper right → left along stop01
        AddArch(list, ToothArch.Upper, ToothSide.Right, 0.05f);
        AddArch(list, ToothArch.Upper, ToothSide.Left, 0.55f);
        AddArch(list, ToothArch.Lower, ToothSide.Right, 0.05f);
        AddArch(list, ToothArch.Lower, ToothSide.Left, 0.55f);
        return list.ToArray();
    }

    static void AddArch(System.Collections.Generic.List<ToothSlot> list, ToothArch arch, ToothSide side, float start)
    {
        var kinds = new[]
        {
            ToothKind.CentralIncisor, ToothKind.LateralIncisor, ToothKind.Canine,
            ToothKind.FirstPremolar, ToothKind.SecondPremolar,
            ToothKind.FirstMolar, ToothKind.SecondMolar, ToothKind.Wisdom
        };
        for (int i = 0; i < kinds.Length; i++)
        {
            float stop = start + i * 0.05f;
            list.Add(ToothSlot.CreateDefault(kinds[i], arch, side, stop));
        }
    }
}
