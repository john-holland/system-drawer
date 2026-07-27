using UnityEngine;

/// <summary>
/// Mouth as exterior edge loop (terrain-style) with fill zone and water potential for saliva.
/// Optional flood driver via duck-typed EmitFromFlow(float) (Locomotion.Liquid).
/// </summary>
[AddComponentMenu("Locomotion/Body Interior/Mouth Exterior Edge Loop")]
public sealed class MouthExteriorEdgeLoop : MonoBehaviour
{
    public Transform rimCenter;
    public float rimRadiusM = 0.04f;
    public Vector3 loopNormal = Vector3.forward;
    public float fillZoneRadiusM = 0.04f;
    [Range(0f, 1f)] public float waterPotential01 = 0.35f;
    public float salivaEmitRateLitersPerSecond = 0.0005f;
    [Tooltip("Optional RollingSphereFloodSimulator or similar with EmitFromFlow(float).")]
    public MonoBehaviour salivaFlood;

    public Vector3 CenterWorld => rimCenter != null ? rimCenter.position : transform.position;

    void Awake()
    {
        if (rimCenter == null)
            rimCenter = transform;
    }

    public void TickSaliva(float dt)
    {
        if (salivaFlood == null || waterPotential01 <= 1e-4f) return;
        float litersPerSec = salivaEmitRateLitersPerSecond * waterPotential01;
        if (litersPerSec <= 0f) return;
        var m = salivaFlood.GetType().GetMethod("EmitFromFlow", new[] { typeof(float) });
        m?.Invoke(salivaFlood, new object[] { litersPerSec });
    }
}
