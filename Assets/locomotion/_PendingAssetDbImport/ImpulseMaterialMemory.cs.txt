using UnityEngine;

/// <summary>
/// Per-piece bending/dent memory. Wood builds memory faster; metal accumulates over time.
/// Destructible impacts call <see cref="NotifyImpulse"/>.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Impulse Material Memory")]
public sealed class ImpulseMaterialMemory : MonoBehaviour
{
    public BuildingMaterialClass materialClass = BuildingMaterialClass.Generic;
    public BuildingRagdoll buildingRagdoll;
    [Tooltip("Memory time constant — lower = faster memory (wood).")]
    public float memoryTau = 8f;
    public float bendGain = 0.08f;
    public float dentGain = 0.04f;
    [Range(0f, 1f)] public float memory01;
    [Range(0f, 1f)] public float bend01;
    [Range(0f, 1f)] public float dent01;

    static float DefaultTau(BuildingMaterialClass c)
    {
        switch (c)
        {
            case BuildingMaterialClass.Wood: return 4f;
            case BuildingMaterialClass.Metal: return 40f;
            case BuildingMaterialClass.Masonry: return 20f;
            case BuildingMaterialClass.Glass: return 2f;
            default: return 12f;
        }
    }

    void Awake()
    {
        if (buildingRagdoll == null)
            buildingRagdoll = GetComponentInParent<BuildingRagdoll>();
        if (memoryTau <= 0f)
            memoryTau = DefaultTau(materialClass);
    }

    void OnEnable()
    {
        if (memoryTau <= 0f)
            memoryTau = DefaultTau(materialClass);
    }

    public void ApplyImpulse(float impulseN, Vector3 worldPoint, bool likelyDent)
    {
        float norm = Mathf.Clamp01(impulseN / 2000f);
        float rate = 1f / Mathf.Max(0.1f, memoryTau);
        bend01 = Mathf.Clamp01(bend01 + norm * bendGain * rate * 10f);
        if (likelyDent || materialClass == BuildingMaterialClass.Metal)
            dent01 = Mathf.Clamp01(dent01 + norm * dentGain * rate * 10f);
        memory01 = Mathf.Clamp01(Mathf.Max(bend01, dent01 * 0.85f));
        buildingRagdoll?.ReportPieceMemory(this, norm);
    }

    /// <summary>Called from DestructibleEnvironment (same process; Locomotion referenced).</summary>
    public static void NotifyImpulse(GameObject source, float impulseN, Vector3 worldPoint)
    {
        if (source == null || impulseN <= 0f) return;
        var mem = source.GetComponent<ImpulseMaterialMemory>()
                  ?? source.GetComponentInParent<ImpulseMaterialMemory>();
        if (mem == null)
        {
            var ragdoll = source.GetComponentInParent<BuildingRagdoll>();
            ragdoll?.ReportAnonymousImpulse(impulseN, worldPoint, source);
            return;
        }
        bool dent = impulseN > 400f;
        mem.ApplyImpulse(impulseN, worldPoint, dent);
    }
}
