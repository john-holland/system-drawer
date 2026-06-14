using Planetary.Field;
using UnityEngine;
using Weather;
using Weather.NearField;

/// <summary>
/// Buoyancy + spin-traction aquaplane solver composing with <see cref="VehicleAmbulationSolver"/>.
/// </summary>
[DisallowMultipleComponent]
public sealed class VehicleAquaplaneSolver : MonoBehaviour
{
    public VehicleAmbulationSolver ambulationSolver;
    public VehicleActor vehicle;
    public Rigidbody chassisRigidbody;
    public LiquidContactSphere[] tireContacts;
    public NearFieldWindInteractionGraph nearFieldWind;

    [Header("Buoyancy")]
    public float waterDensity = 1000f;
    public float submergedVolumeEstimate = 0.5f;

    [Header("Aquaplane traction")]
    [Range(0.01f, 0.15f)] public float aquaplaneMu = 0.05f;
    public float spinTractionGain = 0.002f;
    public float spinTractionCap = 0.12f;
    public float lateralDrag = 0.35f;

    void Awake()
    {
        if (ambulationSolver == null)
            ambulationSolver = GetComponentInParent<VehicleAmbulationSolver>();
        if (vehicle == null)
            vehicle = GetComponentInParent<VehicleActor>();
        if (chassisRigidbody == null && vehicle != null)
            chassisRigidbody = vehicle.GetComponentInChildren<Rigidbody>();
        if (tireContacts == null || tireContacts.Length == 0)
            tireContacts = GetComponentsInChildren<LiquidContactSphere>();
        if (nearFieldWind == null)
            nearFieldWind = FindAnyObjectByType<NearFieldWindInteractionGraph>();
    }

    void FixedUpdate()
    {
        if (chassisRigidbody == null)
            return;

        bool anyLiquid = false;
        float muEffMax = 0f;
        for (int i = 0; i < tireContacts.Length; i++)
        {
            LiquidContactSphere c = tireContacts[i];
            if (c == null)
                continue;
            if (!c.IsInLiquid)
                continue;
            anyLiquid = true;
            float spin = EstimateWheelSpin(c.transform);
            float muEff = aquaplaneMu + spinTractionGain * Mathf.Clamp(spin, 0f, spinTractionCap);
            muEffMax = Mathf.Max(muEffMax, muEff);

            if (nearFieldWind != null)
            {
                Vector3 rel = nearFieldWind.GetBlendedVelocity(c.transform.position, Vector3.zero)
                              - chassisRigidbody.linearVelocity;
                Vector3 lateral = Vector3.ProjectOnPlane(rel, c.transform.up);
                chassisRigidbody.AddForceAtPosition(-lateral * lateralDrag, c.transform.position, ForceMode.Force);
            }
        }

        if (anyLiquid)
            ApplyBuoyancy();

        _lastMuEff = muEffMax;
    }

    float _lastMuEff;

    public float LastMuEff => _lastMuEff;

    public bool TrySolveSteeringLeafAquaplane(
        float steerDemandSigned01,
        IAmbulationRangePropagator rangePropagator,
        Vector3 sampleWorldPosition,
        float tractionBudget01,
        out float steerCommandSigned01,
        out float throttleForward01)
    {
        if (ambulationSolver == null)
        {
            steerCommandSigned01 = 0f;
            throttleForward01 = 0f;
            return false;
        }

        float gripScale = _lastMuEff > 1e-4f ? _lastMuEff / 0.7f : 1f;
        return ambulationSolver.TrySolveSteeringLeaf(
            steerDemandSigned01,
            rangePropagator,
            sampleWorldPosition,
            tractionBudget01 * gripScale,
            out steerCommandSigned01,
            out throttleForward01);
    }

    void ApplyBuoyancy()
    {
        float submerged = EstimateSubmergedFraction();
        if (submerged <= 1e-4f)
            return;
        Vector3 force = Vector3.up * waterDensity * Mathf.Abs(Physics.gravity.y) * submergedVolumeEstimate * submerged;
        chassisRigidbody.AddForce(force, ForceMode.Force);
    }

    float EstimateSubmergedFraction()
    {
        if (tireContacts == null || tireContacts.Length == 0)
            return 0f;
        int wet = 0;
        for (int i = 0; i < tireContacts.Length; i++)
            if (tireContacts[i] != null && tireContacts[i].IsInLiquid)
                wet++;
        return wet / (float)tireContacts.Length;
    }

    static float EstimateWheelSpin(Transform tire)
    {
        var rb = tire.GetComponentInParent<Rigidbody>();
        if (rb == null)
            return 0f;
        Vector3 forward = tire.forward;
        return Mathf.Abs(Vector3.Dot(rb.angularVelocity, tire.right));
    }
}
