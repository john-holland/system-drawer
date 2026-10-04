using System.Collections.Generic;
using UnityEngine;

public enum PulleySurfaceKind
{
    Slats = 0,
    Cloth = 1,
    Reeds = 2
}

/// <summary>Shade surface: slats or cloth/reeds gathered toward a headrail by pull01.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Pulley Surface Ragdoll")]
public sealed class PulleySurfaceRagdoll : MonoBehaviour
{
    public const string PullStringId = "shade.pull_string";

    public PulleySurfaceKind kind = PulleySurfaceKind.Slats;
    [Range(0f, 1f)] public float pull01;
    public AnimationCurve raiseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public int slatCount = 8;
    public float dropMeters = 1.2f;
    public Transform headrail;
    public readonly List<Transform> slats = new List<Transform>();
    public LineRenderer cord;
    public ClothUvStretchDriver cloth;
    public MonoBehaviour ropeSystem;

    ShadePullStringSurface _surface;

    public IVehicleControlSurface PullSurface => _surface ??= new ShadePullStringSurface(this);

    public bool MatchesPullString(string localSurfaceId) =>
        string.Equals(localSurfaceId, PullStringId, System.StringComparison.OrdinalIgnoreCase);

    public void EnsureSlats()
    {
        if (headrail == null)
        {
            var go = new GameObject("headrail");
            go.transform.SetParent(transform, false);
            headrail = go.transform;
        }
        while (slats.Count < Mathf.Max(2, slatCount))
        {
            var go = new GameObject("slat_" + slats.Count);
            go.transform.SetParent(transform, false);
            slats.Add(go.transform);
        }
        ApplyPull();
    }

    public void SetPull01(float value)
    {
        pull01 = Mathf.Clamp01(value);
        ApplyPull();
    }

    public void ApplyPullImpulse(float normalized, float dt)
    {
        float step = Mathf.Clamp(normalized, -1f, 1f) * Mathf.Max(dt, 1f / 60f) * 1.5f;
        SetPull01(pull01 + step);
    }

    public float EvaluateRaise(float t) =>
        raiseCurve != null ? raiseCurve.Evaluate(Mathf.Clamp01(t)) : Mathf.Clamp01(t);

    public float SampleSlatT(int index, float pull)
    {
        int n = Mathf.Max(1, slats.Count > 0 ? slats.Count : slatCount);
        float along = n <= 1 ? 0f : index / (float)(n - 1);
        float raised = EvaluateRaise(Mathf.Clamp01(pull));
        return Mathf.Lerp(along, 0f, raised);
    }

    public void ApplyPull()
    {
        if (slats.Count == 0 && slatCount > 0)
            EnsureSlats();
        Vector3 origin = headrail != null ? headrail.position : transform.position;
        Vector3 down = -transform.up * dropMeters;
        for (int i = 0; i < slats.Count; i++)
        {
            if (slats[i] == null) continue;
            float t = SampleSlatT(i, pull01);
            slats[i].position = origin + down * t;
        }
        if (cord != null)
        {
            cord.positionCount = 2;
            cord.SetPosition(0, origin);
            Vector3 end = slats.Count > 0 && slats[slats.Count - 1] != null
                ? slats[slats.Count - 1].position
                : origin + down * (1f - EvaluateRaise(pull01));
            cord.SetPosition(1, end);
        }
        if (cloth != null)
            cloth.NotifyContact(gameObject, EvaluateRaise(pull01));
    }

    public float LerpPull(float from, float to, float t) =>
        Mathf.Lerp(from, to, EvaluateRaise(Mathf.Clamp01(t)));
}

public sealed class ShadePullStringSurface : IVehicleControlSurface
{
    readonly PulleySurfaceRagdoll _pulley;

    public ShadePullStringSurface(PulleySurfaceRagdoll pulley)
    {
        _pulley = pulley;
        Id = PulleySurfaceRagdoll.PullStringId;
        ImpulseChannelKey = PulleySurfaceRagdoll.PullStringId;
    }

    public string Id { get; }
    public string ImpulseChannelKey { get; }
    public VehicleActor Owner => null;

    public void ApplyImpulse(float normalized, float dt)
    {
        _pulley?.ApplyPullImpulse(normalized, dt);
    }
}

/// <summary>Animation BT node: curve-lerps pulley pull01 while Running.</summary>
[AddComponentMenu("Locomotion/Civil/Pulley Pull Node")]
public sealed class PulleyPullNode : BehaviorTreeNode
{
    public PulleySurfaceRagdoll pulley;
    public AnimationCurve pullCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    public float duration = 0.6f;
    public bool raise = true;

    float _elapsed;
    float _from;
    bool _started;

    void Awake() => nodeType = NodeType.Action;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (pulley == null)
            pulley = GetComponent<PulleySurfaceRagdoll>() ?? GetComponentInParent<PulleySurfaceRagdoll>();
        if (pulley == null)
            return BehaviorTreeStatus.Failure;

        if (!_started)
        {
            _started = true;
            _elapsed = 0f;
            _from = pulley.pull01;
        }

        _elapsed += Time.deltaTime;
        float u = duration <= 1e-4f ? 1f : Mathf.Clamp01(_elapsed / duration);
        float shaped = pullCurve != null ? pullCurve.Evaluate(u) : u;
        float target = raise ? 1f : 0f;
        pulley.SetPull01(Mathf.Lerp(_from, target, shaped));
        return u >= 1f - 1e-4f ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
    }
}
