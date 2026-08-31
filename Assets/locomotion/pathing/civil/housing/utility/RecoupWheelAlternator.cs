using UnityEngine;

/// <summary>Submerged silicon recoup water-wheel (<c>imitirrrr__</c>) on the heater axis.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Recoup Wheel Alternator")]
public sealed class RecoupWheelAlternator : MonoBehaviour
{
    public const string LemmaId = UtilityLemmaPropertyKeys.Imitirrrr;
    public WaterHeaterRuntime heater;
    public HousePowerBus powerBus;
    public float creditKw = 0.15f;
    public float lastCreditKw;
    public bool spinning = true;

    public void Tick(float dt)
    {
        if (heater == null)
            heater = GetComponentInParent<WaterHeaterRuntime>();
        bool flow = spinning && heater != null && heater.running && heater.tankTemp01 > 0.2f;
        lastCreditKw = flow ? creditKw : 0f;
        if (powerBus != null && lastCreditKw > 0f)
            powerBus.charge01 = Mathf.Clamp01(powerBus.charge01 + lastCreditKw * dt * 0.01f);
    }

    public void SetSpinning(bool on) => spinning = on;

    public SdfMax.SdfMaxCompositionAsset ComposeCups(string assetName = "RecoupCupsSdf")
    {
        var asset = UnityEngine.ScriptableObject.CreateInstance<SdfMax.SdfMaxCompositionAsset>();
        asset.name = assetName;
        asset.nodes = new System.Collections.Generic.List<SdfMax.SdfMaxNode>
        {
            new SdfMax.SdfMaxNode
            {
                op = SdfMax.SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfMax.SdfPrimitiveType.Box,
                halfExtents = new Vector3(0.08f, 0.02f, 0.04f),
                localPosition = new Vector3(0.12f, 0f, 0f)
            },
            new SdfMax.SdfMaxNode
            {
                op = SdfMax.SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfMax.SdfPrimitiveType.Box,
                halfExtents = new Vector3(0.08f, 0.02f, 0.04f),
                localPosition = new Vector3(-0.12f, 0f, 0f)
            },
            new SdfMax.SdfMaxNode
            {
                op = SdfMax.SdfMaxOp.Max,
                childIndexA = 0,
                childIndexB = 1
            }
        };
        asset.rootNodeIndex = 2;
        return asset;
    }
}
