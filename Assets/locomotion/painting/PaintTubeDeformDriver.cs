using System.Collections.Generic;
using UnityEngine;
using SdfMax;

/// <summary>
/// Finger-sphere dispense into hanging droplets; SDF Max deformation memory on squeeze.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Tube Deform Driver")]
public sealed class PaintTubeDeformDriver : MonoBehaviour
{
    public PaintTubeConfig config;
    public Transform tubeRoot;
    public Transform nozzle;
    public PaintPileLiquidDriver pileDriver;
    public RagdollSystem ragdoll;
    public string fingerBoneName = "RightHand";
    [Min(0.005f)] public float fingerSphereRadius = 0.012f;
    public SdfMaxCompositionAsset deformMemory;

    float _hangTimer;
    float _pendingMass;
    Color _pendingColor;

    void Awake()
    {
        if (tubeRoot == null) tubeRoot = transform;
        if (nozzle == null) nozzle = transform;
        if (config != null && deformMemory == null)
            deformMemory = PaintTubeSdfComposer.Compose(config);
    }

    void FixedUpdate()
    {
        if (config == null) return;
        float squeeze = 0f;
        var proxy = GetComponentInParent<PaintInstrumentProxy>();
        if (proxy != null)
            squeeze = Mathf.Max(squeeze, proxy.GetChannel(PaintInstrumentMap.TubeSqueeze));

        Transform finger = ResolveFinger();
        if (finger != null && tubeRoot != null)
        {
            float dist = Vector3.Distance(finger.position, tubeRoot.position);
            float penetrate = Mathf.Max(0f, fingerSphereRadius + config.baseRadiusM * 0.5f - dist);
            if (penetrate > 1e-4f || squeeze > 0.1f)
            {
                float depth = Mathf.Max(penetrate, squeeze * 0.02f);
                float vol = fingerSphereRadius * depth * config.volumePerMeter;
                _pendingMass += vol;
                _pendingColor = config.paintColor;
                AppendDeformStamp(finger.position, depth);
                _hangTimer = config.hangTime;
            }
        }

        if (_hangTimer > 0f)
        {
            _hangTimer -= Time.fixedDeltaTime;
            // Hanging blob visualization via pile driver nozzle hold
            if (pileDriver != null && nozzle != null)
                pileDriver.SetHanging(_pendingMass, config.paintColor, nozzle.position, config);
            if (_hangTimer <= 0f && _pendingMass > 1e-5f)
            {
                pileDriver?.ReleaseHang(config);
                _pendingMass = 0f;
            }
        }
    }

    Transform ResolveFinger()
    {
        if (ragdoll == null)
            ragdoll = GetComponentInParent<RagdollSystem>();
        return ragdoll != null ? ragdoll.GetBoneTransform(fingerBoneName) : null;
    }

    void AppendDeformStamp(Vector3 worldFinger, float depth)
    {
        if (deformMemory == null || tubeRoot == null) return;
        Vector3 local = tubeRoot.InverseTransformPoint(worldFinger);
        deformMemory.nodes ??= new List<SdfMaxNode>();
        int leaf = deformMemory.nodes.Count;
        deformMemory.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.PrimitiveLeaf,
            primitiveType = SdfPrimitiveType.Sphere,
            radius = Mathf.Max(config.nozzleRadiusM, depth),
            sphereRadius = Mathf.Max(config.nozzleRadiusM, depth),
            localPosition = local
        });
        int prevRoot = deformMemory.ResolveRootIndex();
        deformMemory.nodes.Add(new SdfMaxNode
        {
            op = SdfMaxOp.Subtract,
            childIndexA = prevRoot >= 0 ? prevRoot : 0,
            childIndexB = leaf
        });
        deformMemory.rootNodeIndex = deformMemory.nodes.Count - 1;
    }
}
