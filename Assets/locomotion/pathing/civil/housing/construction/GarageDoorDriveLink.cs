using UnityEngine;

/// <summary>
/// Automatic physics link: tangent force from axle torque / pitch radius,
/// scaled by SPH pull at wrap. Broken chain yields zero force (door stalls).
/// Does not reference Open.Runtime — slides the door Transform and winds RopeSystem.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Housing/Garage Door Drive Link")]
public sealed class GarageDoorDriveLink : MonoBehaviour
{
    public GarageChainAssembly chain;
    public GarageChainSpec spec;
    public RopeSystem rope;
    public Transform doorLeaf;
    public HousingBuildingRagdoll house;
    public float axleTorqueNm = 80f;
    public float axleAngularRateRad = 1.2f;
    public Vector3 slideAxisLocal = Vector3.up;
    public float slideMeters = 2.1f;
    public float open01;
    Vector3 _closedLocal;

    public bool ChainBroken =>
        spec != null && spec.selectedKind == GarageChainLinkKind.Broken;

    void Awake()
    {
        if (doorLeaf != null)
            _closedLocal = doorLeaf.localPosition;
        else if (house != null && house.slots != null && house.slots.garageDoor != null)
            doorLeaf = house.slots.garageDoor;
        if (doorLeaf != null)
            _closedLocal = doorLeaf.localPosition;
    }

    public float ComputeTangentForceN(float wrapSample01 = 0.5f)
    {
        if (ChainBroken)
            return 0f;
        var s = spec != null ? spec : chain != null ? chain.spec : null;
        float r = s != null ? Mathf.Max(0.02f, s.pitchRadiusM) : 0.12f;
        float sph = 1f;
        if (chain != null && chain.pullField != null && chain.pullField.BinCount > 0)
            sph = Mathf.Max(0.05f, chain.pullField.SampleWrap(wrapSample01));
        float sign = axleAngularRateRad >= 0f ? 1f : -1f;
        return sign * Mathf.Abs(axleTorqueNm) / r * sph;
    }

    public float ComputeWindRateMps()
    {
        if (ChainBroken)
            return 0f;
        var s = spec != null ? spec : chain != null ? chain.spec : null;
        float r = s != null ? Mathf.Max(0.02f, s.pitchRadiusM) : 0.12f;
        return axleAngularRateRad * r;
    }

    public void Apply(float dt)
    {
        float force = ComputeTangentForceN();
        var rs = rope != null ? rope : chain != null ? chain.rope : null;
        if (rs != null)
            rs.SetWindRate(ChainBroken ? 0f : ComputeWindRateMps());

        if (doorLeaf == null)
            return;
        if (ChainBroken)
            return;
        float lift = force * Mathf.Max(0.0001f, dt) / 400f;
        open01 = Mathf.Clamp01(open01 + lift);
        Vector3 axis = slideAxisLocal.sqrMagnitude > 1e-8f ? slideAxisLocal.normalized : Vector3.up;
        doorLeaf.localPosition = _closedLocal + axis * (slideMeters * open01);
    }

    void Update()
    {
        if (Application.isPlaying)
            Apply(Time.deltaTime);
    }
}
