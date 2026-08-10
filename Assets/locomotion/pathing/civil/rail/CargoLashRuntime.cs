using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime FixedJoints / rope straps holding cargo or folded ambulatory sections stable.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Cargo Lash Runtime")]
public sealed class CargoLashRuntime : MonoBehaviour
{
    public CargoLashProfile profile;
    public CargoStabilityBakeAsset bake;
    public CargoStabilityMode mode = CargoStabilityMode.Nominal;
    public Transform deckRoot;
    public Rigidbody cargoBody;
    public readonly CargoStabilityEvaluator evaluator = new CargoStabilityEvaluator();

    readonly List<FixedJoint> _joints = new List<FixedJoint>();
    readonly List<RopeSystem> _ropes = new List<RopeSystem>();
    FixedJoint _impossiblePin;

    public float NormalizedLoadMax
    {
        get
        {
            float max = 0f;
            for (int i = 0; i < _ropes.Count; i++)
            {
                if (_ropes[i] == null) continue;
                max = Mathf.Max(max, _ropes[i].NormalizedLoad);
            }
            return max;
        }
    }

    public bool IsStable => evaluator.LastIsStable;
    public float LashStable01 => evaluator.LastLashStable01;

    public void ApplyProfile(CargoLashProfile p, CargoStabilityMode stabilityMode)
    {
        profile = p;
        mode = stabilityMode;
        ClearJoints();
        if (mode == CargoStabilityMode.ImpossibleKeepStable)
        {
            ApplyImpossiblePin();
            TickEvaluate(Vector3.zero);
            return;
        }
        if (profile == null || cargoBody == null) return;
        for (int i = 0; i < profile.joints.Count; i++)
        {
            var spec = profile.joints[i];
            if (spec == null || spec.anchorA == null) continue;
            var host = spec.anchorA.GetComponent<Rigidbody>() ?? spec.anchorA.gameObject.AddComponent<Rigidbody>();
            var fj = host.gameObject.AddComponent<FixedJoint>();
            fj.connectedBody = cargoBody;
            fj.breakForce = mode == CargoStabilityMode.SoftLash
                ? spec.breakForce * 0.5f
                : spec.breakForce;
            fj.breakTorque = mode == CargoStabilityMode.SoftLash
                ? spec.breakTorque * 0.5f
                : spec.breakTorque;
            _joints.Add(fj);
        }
        TickEvaluate(Vector3.zero);
    }

    public void TickEvaluate(Vector3 accelWorld)
    {
        Vector3 com = cargoBody != null ? cargoBody.worldCenterOfMass : transform.position;
        evaluator.Evaluate(mode, profile, bake, deckRoot != null ? deckRoot : transform, com, accelWorld, NormalizedLoadMax);
        if (mode == CargoStabilityMode.ImpossibleKeepStable && !IsStable)
        {
            // Force stable reporting + pin.
            ApplyImpossiblePin();
            evaluator.Evaluate(CargoStabilityMode.ImpossibleKeepStable, profile, bake, deckRoot, com, accelWorld, 0f);
        }
    }

    void ApplyImpossiblePin()
    {
        if (cargoBody == null) return;
        if (_impossiblePin == null)
        {
            _impossiblePin = gameObject.GetComponent<FixedJoint>() ?? gameObject.AddComponent<FixedJoint>();
            _impossiblePin.connectedBody = cargoBody;
        }
        _impossiblePin.breakForce = Mathf.Infinity;
        _impossiblePin.breakTorque = Mathf.Infinity;
        cargoBody.isKinematic = true;
    }

    public void ClearJoints()
    {
        for (int i = 0; i < _joints.Count; i++)
            if (_joints[i] != null)
                DestroyImmediateSafe(_joints[i]);
        _joints.Clear();
        if (_impossiblePin != null)
        {
            DestroyImmediateSafe(_impossiblePin);
            _impossiblePin = null;
        }
        if (cargoBody != null && mode != CargoStabilityMode.ImpossibleKeepStable)
            cargoBody.isKinematic = false;
    }

    public void RegisterRope(RopeSystem rope)
    {
        if (rope != null && !_ropes.Contains(rope))
            _ropes.Add(rope);
    }

    static void DestroyImmediateSafe(UnityEngine.Object o)
    {
        if (o == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            UnityEngine.Object.DestroyImmediate(o);
        else
            UnityEngine.Object.Destroy(o);
#else
        UnityEngine.Object.Destroy(o);
#endif
    }

    void OnDestroy() => ClearJoints();
}
