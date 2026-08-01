using UnityEngine;

/// <summary>
/// Permanent sink nozzle with springy tip + rinse throughput (water-delivery sentiment).
/// </summary>
[AddComponentMenu("Locomotion/Kitchen/Sink Spring Nozzle Fixture")]
public sealed class SinkSpringNozzleFixture : MonoBehaviour
{
    public Transform tip;
    public SpringJoint spring;
    public KitchenDispenseNozzle dispense;
    [Range(0f, 1f)] public float open01 = 1f;
    public float springForce = 80f;
    public float springDamper = 8f;
    public float totalRinseLiters;
    public string flowSentiment = "flowing";

    void Awake()
    {
        if (dispense == null)
            dispense = GetComponent<KitchenDispenseNozzle>() ?? gameObject.AddComponent<KitchenDispenseNozzle>();
        if (tip == null) tip = transform;
        EnsureSpring();
    }

    public void EnsureSpring()
    {
        if (tip == null || tip == transform) return;
        var rb = tip.GetComponent<Rigidbody>();
        if (rb == null) rb = tip.gameObject.AddComponent<Rigidbody>();
        rb.mass = 0.15f;
        if (spring == null)
            spring = tip.GetComponent<SpringJoint>() ?? tip.gameObject.AddComponent<SpringJoint>();
        spring.autoConfigureConnectedAnchor = false;
        spring.connectedBody = GetComponent<Rigidbody>();
        spring.spring = springForce;
        spring.damper = springDamper;
        spring.maxDistance = 0.08f;
    }

    /// <summary>Rinse liters with stalled/almost/endless sentiment like liquid delivery.</summary>
    public float Rinse(float liters, DishScrubMode mode = DishScrubMode.TimingAndFlood)
    {
        float open = Mathf.Clamp01(open01);
        dispense.open01 = open;
        float delivered = Mathf.Max(0f, liters) * open;
        if (open < 0.05f)
        {
            flowSentiment = "stalled";
            delivered = 0f;
        }
        else if (open < 0.45f)
            flowSentiment = "almost";
        else if (open > 0.98f && liters > 1f)
            flowSentiment = "endless";
        else
            flowSentiment = "flowing";

        dispense.Dispense(delivered);
        totalRinseLiters += delivered;
        return delivered;
    }
}
