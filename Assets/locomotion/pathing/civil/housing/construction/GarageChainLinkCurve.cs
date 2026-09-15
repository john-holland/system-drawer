using System;
using UnityEngine;

/// <summary>Shared roller-link dimensions. Chain / Broken use the same curve; Master unwelded + clip.</summary>
[Serializable]
public sealed class GarageChainLinkCurve
{
    public float pitchM = 0.08f;
    public float plateOuterR = 0.018f;
    [Range(0.15f, 1f)]
    public float waistScale = 0.55f;
    public float plateThickness = 0.002f;
    public float plateGap = 0.012f;
    public float pinRadius = 0.004f;
    public float pinLength = 0.02f;
    public float rollerRadius = 0.008f;
    public float rollerInnerRadius = 0.0045f;
    public float rollerLength = 0.01f;
    public float weldK = 0.0015f;
    public float clipThickness = 0.0015f;
    public float clipSpan = 0.02f;

    public float Pitch => Mathf.Max(0.02f, pitchM);
    public float PlateOuterR => Mathf.Max(0.004f, plateOuterR);
    public float WaistScale => Mathf.Clamp(waistScale, 0.15f, 1f);
    public float WaistHalfWidth => PlateOuterR * WaistScale;
    public float PlateThickness => Mathf.Max(0.0004f, plateThickness);
    public float PlateGap => Mathf.Max(PlateThickness, plateGap);
    public float PinRadius => Mathf.Max(0.0008f, pinRadius);
    public float PinLength => Mathf.Max(PlateGap + PlateThickness, pinLength);
    public float RollerRadius => Mathf.Max(PinRadius + 0.0004f, rollerRadius);
    public float RollerInnerRadius => Mathf.Clamp(rollerInnerRadius, PinRadius, RollerRadius - 0.0002f);
    public float RollerLength => Mathf.Max(0.002f, Mathf.Min(rollerLength, PlateGap));
    public float WeldK => Mathf.Max(0.0002f, weldK);
    public float ClipThickness => Mathf.Max(0.0004f, clipThickness);
    public float ClipSpan => Mathf.Max(PlateOuterR, clipSpan);

    public Vector3 LeftPin => new Vector3(-Pitch * 0.5f, 0f, 0f);
    public Vector3 RightPin => new Vector3(Pitch * 0.5f, 0f, 0f);

    public void SyncFromPitch(float linkPitchM)
    {
        pitchM = Mathf.Max(0.02f, linkPitchM);
    }

    public float WaistY(float x)
    {
        float half = Pitch * 0.5f;
        float t = Mathf.Clamp01(Mathf.Abs(x) / Mathf.Max(1e-6f, half));
        return Mathf.Lerp(WaistHalfWidth, PlateOuterR, t * t);
    }
}
