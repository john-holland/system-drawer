using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Structural integrity facade for a civil building — health, impulse memory aggregate, damage queue reporting.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Building Ragdoll")]
public class BuildingRagdoll : MonoBehaviour
{
    public string buildingStableId;
    public BuildingBioRhythmService bio;
    public DamagedObjectQueue damageQueue;
    [Tooltip("Optional soft ref — BuildingBeast stub for later fiction.")]
    public BuildingBeast beastStub;
    [Range(0f, 1f)] public float enqueueDamageThreshold01 = 0.12f;
    public string repairWaypointGroup = "repair";

    readonly List<ImpulseMaterialMemory> _pieces = new List<ImpulseMaterialMemory>();

    public BuildingHealthState Health => bio != null ? bio.health : null;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(buildingStableId))
            buildingStableId = gameObject.name;
        if (bio == null)
            bio = GetComponent<BuildingBioRhythmService>() ?? gameObject.AddComponent<BuildingBioRhythmService>();
        if (damageQueue == null)
            damageQueue = GetComponent<DamagedObjectQueue>() ?? FindFirstObjectByType<DamagedObjectQueue>();
        if (beastStub == null)
            beastStub = GetComponent<BuildingBeast>();
        GetComponentsInChildren(true, _pieces);
    }

    public virtual void Tick(float dt)
    {
        bio?.Tick(dt);
    }

    public void ReportPieceMemory(ImpulseMaterialMemory piece, float impulseNorm01)
    {
        if (bio?.health == null) return;
        float dmg = impulseNorm01 * 0.05f + (piece != null ? piece.memory01 * 0.02f : 0f);
        bio.health.ApplyImpulseDamage(dmg);
        bio.health.memoryAggregate01 = Mathf.Clamp01(
            bio.health.memoryAggregate01 * 0.98f + (piece != null ? piece.memory01 : impulseNorm01) * 0.02f);
        MaybeEnqueue(piece != null ? piece.gameObject : null, dmg, piece != null ? piece.materialClass : BuildingMaterialClass.Generic);
    }

    public void ReportAnonymousImpulse(float impulseN, Vector3 worldPoint, GameObject source)
    {
        float norm = Mathf.Clamp01(impulseN / 2000f);
        if (bio?.health == null) return;
        bio.health.ApplyImpulseDamage(norm * 0.04f);
        bio.health.exteriorPressure01 = Mathf.Clamp01(bio.health.exteriorPressure01 + norm * 0.1f);
        MaybeEnqueue(source, norm * 0.04f, BuildingMaterialClass.Generic);
    }

    void MaybeEnqueue(GameObject source, float damage01, BuildingMaterialClass mat)
    {
        if (damageQueue == null || damage01 < enqueueDamageThreshold01) return;
        damageQueue.Enqueue(new DamagedObjectRecord
        {
            objectId = source != null ? source.name : $"{buildingStableId}:anon",
            buildingId = buildingStableId,
            damage01 = Mathf.Clamp01(damage01),
            materialClass = mat,
            worldPos = source != null ? source.transform.position : transform.position,
            reportedAtUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            waypointGroup = repairWaypointGroup,
            source = source
        });
    }

    public void ApplyRepair(float amount01)
    {
        if (bio?.health == null) return;
        bio.health.integrity01 = Mathf.Clamp01(bio.health.integrity01 + amount01);
    }
}
