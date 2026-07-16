using System;
using UnityEngine;

public enum ComputerKeyboardRenderMode
{
    SdfMax = 0,
    SimpleMesh = 1
}

/// <summary>Procedural computer keyboard geometry + travel authoring.</summary>
[Serializable]
public sealed class ComputerKeyboardSpec
{
    public float baseWidth = 0.45f;
    public float baseHeight = 0.025f;
    public float baseDepth = 0.16f;
    public float baseBevel = 0.004f;
    public float sideAngleDeg = 8f;
    public float topBevel = 0.003f;
    public float slantTowardUserDeg = 6f;
    public float overallBevel = 0.002f;
    public int auxKeyCount = 3;
    public bool fnPresent = true;
    public int maxJumpPressAttempts = 5;

    [Header("Chiclet defaults")]
    public float chicletWidth = 0.018f;
    public float chicletHeight = 0.01f;
    public float chicletSideAngleDeg = 5f;
    public float chicletSideBevel = 0.001f;
    public float chicletTopBevel = 0.0015f;
    public float chicletFingerInset = 0.001f;
    public float chicletTravel = 0.0035f;
    public float chicletClearance = 0.001f;
    public float minPressImpulse = 0.55f;

    [Header("Volume knob")]
    public float volumeKnobHeight = 0.018f;
    public float volumeKnobRadius = 0.01f;
    public float volumeKnobTopBevel = 0.002f;
    public float volumeKnobClearance = 0.002f;
    public float volumeKnobTravel = 0.004f;
    public int volumeKnobClicks = 12;

    public ComputerKeyboardRenderMode bodyRenderMode = ComputerKeyboardRenderMode.SimpleMesh;
    public ComputerKeyboardRenderMode chicletRenderMode = ComputerKeyboardRenderMode.SimpleMesh;

    /// <summary>Travel band: [min(base0, travel), clearance] preferred.</summary>
    public Vector2 ComputeTravelBand(float rowDepthFactor)
    {
        float base0 = chicletTravel;
        float f = rowDepthFactor; // f(depth/7) proxy
        float altMax = baseHeight - f + chicletClearance - chicletHeight;
        float lo = Mathf.Min(base0, Mathf.Max(0.0005f, Mathf.Min(base0, altMax)));
        float hi = Mathf.Max(lo, chicletClearance);
        return new Vector2(lo, hi);
    }
}
