using UnityEngine;

public enum CutProjectionMode
{
    SdfMax,
    MeshRendererRotating,
    Projected
}

/// <summary>Tool that emits combat cut events on an interval with projection options.</summary>
[AddComponentMenu("Locomotion/Combat/Cut Tool")]
public sealed class CutToolComponent : MonoBehaviour
{
    public bool active;
    public float cutInterval = 0.2f;
    public string cutterProfileId = "chainsaw";
    public string cutProfileId = "human_flesh";
    public CutProjectionMode projectionMode = CutProjectionMode.Projected;
    public CombatDamageType damageType = CombatDamageType.ContinuousCutter;
    [Range(0f, 1f)] public float depth01 = 0.5f;
    [Range(0f, 1f)] public float amount01 = 0.2f;
    public float range = 1.2f;
    public LayerMask hitMask = ~0;

    [Header("Stretch / repeat (spatial-generator style)")]
    public bool stretchToBounds;
    public bool repeatTexture = true;
    public Vector2 textureScale = Vector2.one;

    public GameObject owner;
    float _accum;
    public int emitCount;

    void Update()
    {
        if (!active) return;
        _accum += Time.deltaTime;
        if (_accum < Mathf.Max(0.02f, cutInterval)) return;
        _accum = 0f;
        EmitOnce();
    }

    public void EmitOnce()
    {
        Vector3 origin = transform.position;
        Vector3 dir = transform.forward;
        if (!Physics.Raycast(origin, dir, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
            return;
        GameObject target = hit.collider.attachedRigidbody != null
            ? hit.collider.attachedRigidbody.gameObject
            : hit.collider.gameObject;
        EmitAt(target, hit.point, dir);
    }

    /// <summary>Emit a cut event at a known target (tests / scripted cuts without raycast).</summary>
    public void EmitAt(GameObject target, Vector3 worldHit, Vector3 direction)
    {
        if (target == null) return;
        var evt = new CombatDamageEvent
        {
            attacker = owner != null ? owner : gameObject,
            target = target,
            type = damageType,
            worldHit = worldHit,
            direction = direction.sqrMagnitude > 1e-6f ? direction.normalized : transform.forward,
            depth01 = depth01,
            amount01 = amount01,
            cutterProfileId = cutterProfileId,
            cutProfileId = cutProfileId,
            createWound = true,
            autoSuture = false,
            limbId = "Chest" // todo: use scan result for limbid, unless limbid is the attacking limb
        };
        var rd = evt.target.GetComponentInParent<RagdollSystem>();
        if (rd != null) evt.target = rd.gameObject;

        CombatDamageFamilyRouter.ApplyForCard(null, evt);
        emitCount++;
    }
}
