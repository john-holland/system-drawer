using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds active parkour land animation prep (goal + impact curve) for BT IK / training selection.
/// Draws an example land gizmo when a landing animation type is selected and <see cref="showGizmo"/> is true.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Parkour/Parkour Land Animation Driver")]
public sealed class ParkourLandAnimationDriver : MonoBehaviour
{
    [Header("Active land")]
    public string activeAnimationGroupTag;
    public PhysicsIKTrainingCategory activeCategory = PhysicsIKTrainingCategory.ParkourSpringLanding;
    public LandAnimationPrep activePrep;
    public Vector3 landingGoalWorld;
    public bool hasLandingGoal;

    [Header("Gizmo")]
    [Tooltip("Draw example landing goal + impact keyframe markers when a land anim type is active/selected.")]
    public bool showGizmo = true;

    [Tooltip("Optional IK manager used to detect selected land animation types for gizmo preview.")]
    public RagdollIKAnimationManager ikAnimationManager;

    float _until;
    float _startedAt;
    float _duration = 1f;

    public void PlayLanding(string animationGroupTag, Vector3 goalWorld, LandAnimationPrep prep, float durationSeconds = 1f)
    {
        activeAnimationGroupTag = animationGroupTag ?? "";
        activeCategory = CategoryForTag(activeAnimationGroupTag);
        landingGoalWorld = goalWorld;
        hasLandingGoal = true;
        activePrep = prep ?? new LandAnimationPrep();
        activePrep.EnsureReady();
        _duration = Mathf.Max(0.05f, durationSeconds);
        _startedAt = Time.time;
        _until = _startedAt + _duration;
    }

    public float SampleImpact01(float normalizedTime)
    {
        if (activePrep?.impactCurve == null)
            return 0f;
        return activePrep.impactCurve.Evaluate(normalizedTime);
    }

    public float SampleImpact01Now()
    {
        if (_until <= 0f || _duration <= 1e-4f)
            return 0f;
        float t = Mathf.Clamp01((Time.time - _startedAt) / _duration);
        return SampleImpact01(t);
    }

    void Update()
    {
        if (_until > 0f && Time.time >= _until)
        {
            activeAnimationGroupTag = null;
            hasLandingGoal = false;
            _until = 0f;
        }
    }

    void LateUpdate()
    {
        SyncPreviewFromSelectedLandAnim();
    }

    void SyncPreviewFromSelectedLandAnim()
    {
        if (_until > 0f)
            return;

        RagdollIKAnimationManager mgr = ikAnimationManager != null
            ? ikAnimationManager
            : GetComponent<RagdollIKAnimationManager>() ?? GetComponentInParent<RagdollIKAnimationManager>() ?? GetComponentInChildren<RagdollIKAnimationManager>();
        ikAnimationManager = mgr;
        if (mgr == null)
            return;

        List<RagdollAnimationSet> selected = mgr.GetSelectedAnimationSets();
        if (selected == null || selected.Count == 0)
            return;

        for (int i = 0; i < selected.Count; i++)
        {
            RagdollAnimationSet set = selected[i];
            ABTClipConfig cfg = set?.animationTree != null ? set.animationTree.GetActiveConfiguration() : null;
            if (cfg == null || !IsLandingCategory(cfg.testCategory))
                continue;

            activeCategory = cfg.testCategory;
            activeAnimationGroupTag = TagForCategory(cfg.testCategory);
            activePrep = cfg.landPrep ?? new LandAnimationPrep();
            activePrep.EnsureReady();
            if (!hasLandingGoal || (_until <= 0f && Application.isPlaying == false))
            {
                Vector3 origin = transform.position;
                Vector3 forward = transform.forward;
                if (forward.sqrMagnitude < 1e-4f)
                    forward = Vector3.forward;
                landingGoalWorld = origin + forward.normalized * 1.25f;
                hasLandingGoal = true;
            }
            return;
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmo)
            return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
            SyncPreviewFromSelectedLandAnim();
#endif
        if (!ShouldDrawLandGizmo())
            return;
        DrawLandGizmo();
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo)
            return;
        if (!ShouldDrawLandGizmo())
            return;
        DrawLandGizmo();
    }

    bool ShouldDrawLandGizmo()
    {
        if (hasLandingGoal && IsLandingCategory(activeCategory))
            return true;
        if (!string.IsNullOrEmpty(activeAnimationGroupTag) && IsLandingTag(activeAnimationGroupTag))
            return true;
        return false;
    }

    void DrawLandGizmo()
    {
        LandAnimationPrep prep = activePrep ?? new LandAnimationPrep();
        prep.EnsureReady();

        Vector3 goal = landingGoalWorld;
        Vector3 from = transform.position;
        Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.9f);
        Gizmos.DrawWireSphere(goal, 0.18f);
        Gizmos.DrawLine(from, goal);

        Vector3 mid = Vector3.Lerp(from, goal, 0.5f) + Vector3.up * 0.6f;
        DrawArc(from, mid, goal, 12);

        List<LandImpactKeyframe> impacts = prep.impactCurve.GetImpactKeyframes();
        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.95f);
        for (int i = 0; i < impacts.Count; i++)
        {
            LandImpactKeyframe k = impacts[i];
            if (k == null) continue;
            Vector3 p = SampleArcPoint(from, mid, goal, Mathf.Clamp01(k.time01));
            float h = 0.15f + 0.35f * Mathf.Clamp01(k.strength01);
            Gizmos.DrawLine(p, p + Vector3.up * h);
            Gizmos.DrawWireSphere(p, 0.06f);
        }
    }

    static void DrawArc(Vector3 a, Vector3 b, Vector3 c, int segments)
    {
        Vector3 prev = a;
        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 p = SampleArcPoint(a, b, c, t);
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    static Vector3 SampleArcPoint(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }

    public static bool IsLandingTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return false;
        string t = tag.ToLowerInvariant();
        return t == ParkourAnimationGroup.SpringLanding
               || t == ParkourAnimationGroup.OneLegLanding
               || t == ParkourAnimationGroup.OneHandLanding
               || t == ParkourAnimationGroup.FallRolls
               || t.Contains("landing")
               || t.Contains("fall_roll");
    }

    public static bool IsLandingCategory(PhysicsIKTrainingCategory category)
    {
        return category == PhysicsIKTrainingCategory.ParkourSpringLanding
               || category == PhysicsIKTrainingCategory.ParkourOneLegLanding
               || category == PhysicsIKTrainingCategory.ParkourOneHandLanding
               || category == PhysicsIKTrainingCategory.ParkourFallRolls;
    }

    public static PhysicsIKTrainingCategory CategoryForTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            return PhysicsIKTrainingCategory.ParkourSpringLanding;
        string t = tag.ToLowerInvariant();
        if (t.Contains("one_leg") || t.Contains("oneleg"))
            return PhysicsIKTrainingCategory.ParkourOneLegLanding;
        if (t.Contains("one_hand") || t.Contains("onehand"))
            return PhysicsIKTrainingCategory.ParkourOneHandLanding;
        if (t.Contains("fall_roll") || t.Contains("fallroll"))
            return PhysicsIKTrainingCategory.ParkourFallRolls;
        if (t.Contains("spring_landing") || t.Contains("springlanding"))
            return PhysicsIKTrainingCategory.ParkourSpringLanding;
        if (IsLandingTag(t))
            return PhysicsIKTrainingCategory.ParkourSpringLanding;
        return PhysicsIKTrainingCategory.ParkourSpringLanding;
    }

    public static string TagForCategory(PhysicsIKTrainingCategory category)
    {
        switch (category)
        {
            case PhysicsIKTrainingCategory.ParkourOneLegLanding:
                return ParkourAnimationGroup.OneLegLanding;
            case PhysicsIKTrainingCategory.ParkourOneHandLanding:
                return ParkourAnimationGroup.OneHandLanding;
            case PhysicsIKTrainingCategory.ParkourFallRolls:
                return ParkourAnimationGroup.FallRolls;
            default:
                return ParkourAnimationGroup.SpringLanding;
        }
    }

    /// <summary>Scale ABT attenuation by current impact sample (hook for frame playback).</summary>
    public static float ScaleAttenuationByImpact(float baseAttenuation, float impact01)
    {
        return Mathf.Clamp01(baseAttenuation * (0.35f + 0.65f * Mathf.Clamp01(impact01)));
    }

    public static ParkourLandAnimationDriver FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        return host.GetComponent<ParkourLandAnimationDriver>()
               ?? host.GetComponentInParent<ParkourLandAnimationDriver>()
               ?? host.AddComponent<ParkourLandAnimationDriver>();
    }

    public static LandAnimationPrep ResolvePrepFromSets(RagdollAnimationSetManager mgr, string tag, PhysicsIKTrainingCategory category)
    {
        if (mgr?.animationSets != null)
        {
            for (int i = 0; i < mgr.animationSets.Count; i++)
            {
                RagdollAnimationSet set = mgr.animationSets[i];
                ABTClipConfig cfg = set?.animationTree != null ? set.animationTree.GetActiveConfiguration() : null;
                if (cfg == null) continue;
                if (IsLandingCategory(cfg.testCategory) &&
                    (cfg.testCategory == category || string.IsNullOrEmpty(tag) ||
                     (set.displayName != null && set.displayName.IndexOf(tag, System.StringComparison.OrdinalIgnoreCase) >= 0)))
                {
                    LandAnimationPrep prep = cfg.landPrep ?? new LandAnimationPrep();
                    prep.EnsureReady();
                    return prep;
                }
            }

            for (int i = 0; i < mgr.animationSets.Count; i++)
            {
                RagdollAnimationSet set = mgr.animationSets[i];
                ABTClipConfig cfg = set?.animationTree != null ? set.animationTree.GetActiveConfiguration() : null;
                if (cfg != null && IsLandingCategory(cfg.testCategory))
                {
                    LandAnimationPrep prep = cfg.landPrep ?? new LandAnimationPrep();
                    prep.EnsureReady();
                    return prep;
                }
            }
        }

        var fallback = new LandAnimationPrep();
        fallback.EnsureReady();
        return fallback;
    }
}
