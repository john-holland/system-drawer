using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Garage door track: road/rail spline installed in a wall plane, perpendicular to the ground.
/// Head curve is the wheel groove.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Housing/Garage Door Vertical Track")]
public sealed class GarageDoorVerticalTrack : MonoBehaviour
{
    public PlanarSplinePathLocomotion spline;
    public RailSideCurveParams groove = new RailSideCurveParams();
    public float heightM = 2.2f;
    public float headerRadiusM = 0.35f;
    public float wallOffsetM = 0.08f;
    public List<PixelLightGridMountGameObject> wheelMounts = new List<PixelLightGridMountGameObject>();
    public RadialJoinKind wheelJoin = RadialJoinKind.Hardware;
    public string wheelJointId = "garage_roller";

    public PlanarSplinePathLocomotion EnsureSpline()
    {
        if (spline == null)
            spline = GetComponent<PlanarSplinePathLocomotion>() ?? gameObject.AddComponent<PlanarSplinePathLocomotion>();
        EnsureWallPlaneSpline();
        return spline;
    }

    /// <summary>Control points in the local XY wall plane (Y up, X along header) — not a ground ribbon.</summary>
    public void EnsureWallPlaneSpline()
    {
        spline ??= GetComponent<PlanarSplinePathLocomotion>() ?? gameObject.AddComponent<PlanarSplinePathLocomotion>();
        float h = Mathf.Max(0.5f, heightM);
        float r = Mathf.Max(0.05f, headerRadiusM);
        spline.controlPoints = new List<Vector3>
        {
            new Vector3(0f, 0f, wallOffsetM),
            new Vector3(0f, h * 0.55f, wallOffsetM),
            new Vector3(r * 0.35f, h - r * 0.15f, wallOffsetM),
            new Vector3(r, h, wallOffsetM)
        };
        spline.defaultWidth = Mathf.Max(0.04f, groove.head != null ? groove.head.Evaluate(0f) : 0.04f);
    }

    public PixelLightGridMountGameObject AddWheelMount(Vector3 local)
    {
        var go = new GameObject("TrackWheelMount");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = local;
        var m = go.AddComponent<PixelLightGridMountGameObject>();
        wheelMounts.Add(m);
        return m;
    }

    public float GrooveWidth() =>
        groove != null && groove.head != null ? Mathf.Max(0.02f, groove.head.Evaluate(0f)) : 0.04f;
}
