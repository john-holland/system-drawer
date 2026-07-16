using UnityEngine;

/// <summary>
/// Per-actor runtime for hierarchical sit / stand-on tow chain + CoG stabilize.
/// Attach beside RagdollSystem / BehaviorTree.
/// </summary>
[AddComponentMenu("Locomotion/Seated Occupancy Runtime")]
public sealed class SeatedOccupancyRuntime : MonoBehaviour
{
    public SurfaceOccupancyMode mode = SurfaceOccupancyMode.Sit;
    public IkTowChain chain = new IkTowChain();
    public SeatedCogStabilizer cog = new SeatedCogStabilizer();
    public SitSurfaceContact surface;
    public bool occupied;
    public float feetReachGroundMaxDistance = 0.12f;
    public float lastFeetGroundDistance = float.PositiveInfinity;
    public bool feetReachGround = true;

    RagdollSystem _ragdoll;

    void Awake()
    {
        _ragdoll = GetComponent<RagdollSystem>();
        if (_ragdoll == null)
            _ragdoll = GetComponentInChildren<RagdollSystem>();
    }

    void FixedUpdate()
    {
        if (!occupied || chain == null || !chain.active)
            return;
        float dt = Time.fixedDeltaTime;
        chain.Tick(dt);
        if (cog != null && surface != null)
        {
            cog.surface = surface;
            cog.mode = mode;
            cog.Evaluate(gameObject);
            if (mode == SurfaceOccupancyMode.Sit)
                UpdateFeetReach();
        }
    }

    public void BeginSit(SitSurfaceContact contact)
    {
        surface = contact;
        mode = SurfaceOccupancyMode.Sit;
        occupied = true;
        EnsureRagdoll();
        Transform pelvis = ResolvePelvis();
        Transform torso = ResolveTorso();
        Rigidbody pelvisRb = pelvis != null ? pelvis.GetComponent<Rigidbody>() : null;
        chain = IkTowChain.BuildSit(contact, pelvis, torso, pelvisRb);
        cog.surface = contact;
        cog.mode = SurfaceOccupancyMode.Sit;
        UpdateFeetReach();
    }

    public void BeginStandOn(SitSurfaceContact contact)
    {
        surface = contact;
        mode = SurfaceOccupancyMode.StandOn;
        occupied = true;
        EnsureRagdoll();
        Transform left = ResolveFoot(true);
        Transform right = ResolveFoot(false);
        Transform pelvis = ResolvePelvis();
        Rigidbody lRb = left != null ? left.GetComponent<Rigidbody>() : null;
        Rigidbody rRb = right != null ? right.GetComponent<Rigidbody>() : null;
        Rigidbody pRb = pelvis != null ? pelvis.GetComponent<Rigidbody>() : null;
        chain = IkTowChain.BuildStandOn(contact, left, right, pelvis, lRb, rRb, pRb);
        cog.surface = contact;
        cog.mode = SurfaceOccupancyMode.StandOn;
        feetReachGround = true;
        lastFeetGroundDistance = 0f;
    }

    public void EndOccupancy()
    {
        occupied = false;
        if (chain != null)
            chain.Clear();
        surface = null;
    }

    public void UpdateFeetReach()
    {
        Transform left = ResolveFoot(true);
        Transform right = ResolveFoot(false);
        float dL = ProbeDown(left);
        float dR = ProbeDown(right);
        lastFeetGroundDistance = Mathf.Min(dL, dR);
        feetReachGround = lastFeetGroundDistance <= feetReachGroundMaxDistance;
    }

    float ProbeDown(Transform foot)
    {
        if (foot == null)
            return float.PositiveInfinity;
        Vector3 origin = foot.position + Vector3.up * 0.05f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 2.5f, ~0, QueryTriggerInteraction.Ignore))
            return hit.distance - 0.05f;
        return float.PositiveInfinity;
    }

    void EnsureRagdoll()
    {
        if (_ragdoll == null)
        {
            _ragdoll = GetComponent<RagdollSystem>();
            if (_ragdoll == null)
                _ragdoll = GetComponentInChildren<RagdollSystem>();
        }
    }

    Transform ResolvePelvis()
    {
        EnsureRagdoll();
        if (_ragdoll != null && _ragdoll.pelvisComponent != null && _ragdoll.pelvisComponent.PrimaryBoneTransform != null)
            return _ragdoll.pelvisComponent.PrimaryBoneTransform;
        return _ragdoll != null ? _ragdoll.GetBoneTransform("Pelvis") : null;
    }

    Transform ResolveTorso()
    {
        EnsureRagdoll();
        if (_ragdoll == null) return null;
        return _ragdoll.GetBoneTransform("Spine")
               ?? _ragdoll.GetBoneTransform("Chest")
               ?? _ragdoll.GetBoneTransform("Torso");
    }

    Transform ResolveFoot(bool left)
    {
        EnsureRagdoll();
        if (_ragdoll == null) return null;
        string side = left ? "Left" : "Right";
        return _ragdoll.GetBoneTransform(side + "Foot")
               ?? _ragdoll.GetBoneTransform(side + "Toe")
               ?? _ragdoll.GetBoneTransform(side.ToLowerInvariant() + "_foot");
    }
}
