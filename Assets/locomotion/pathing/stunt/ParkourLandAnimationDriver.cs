using UnityEngine;

public sealed class ParkourLandAnimationDriver : MonoBehaviour
{
    public string activeAnimationGroupTag;
    public PhysicsIKTrainingCategory activeCategory;
    public LandAnimationPrep activePrep;
    public Vector3 landingGoalWorld;
    public bool hasLandingGoal;
    public RagdollIKAnimationManager ikAnimationManager;
    public bool showGizmo = true;

    public static bool IsLandingCategory(PhysicsIKTrainingCategory cat) =>
        cat.ToString().IndexOf("Land", System.StringComparison.OrdinalIgnoreCase) >= 0;

    public static bool IsLandingTag(string tag) =>
        !string.IsNullOrEmpty(tag) &&
        (tag.IndexOf("land", System.StringComparison.OrdinalIgnoreCase) >= 0
         || tag.IndexOf("fall_roll", System.StringComparison.OrdinalIgnoreCase) >= 0);

    public static PhysicsIKTrainingCategory CategoryForTag(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return PhysicsIKTrainingCategory.ParkourSpringLanding;
        string t = tag.ToLowerInvariant();
        if (t.Contains("one_leg") || t.Contains("oneleg")) return PhysicsIKTrainingCategory.ParkourOneLegLanding;
        if (t.Contains("one_hand") || t.Contains("onehand")) return PhysicsIKTrainingCategory.ParkourOneHandLanding;
        if (t.Contains("fall_roll") || t.Contains("fallroll")) return PhysicsIKTrainingCategory.ParkourFallRolls;
        return PhysicsIKTrainingCategory.ParkourSpringLanding;
    }

    public static float ScaleAttenuationByImpact(float baseAttenuation, float impact01) =>
        Mathf.Clamp01(baseAttenuation * (0.35f + 0.65f * Mathf.Clamp01(impact01)));

    public static ParkourLandAnimationDriver FindOrCreate(GameObject host)
    {
        if (host == null) return null;
        return host.GetComponent<ParkourLandAnimationDriver>() ?? host.AddComponent<ParkourLandAnimationDriver>();
    }

    public float SampleImpact01(float normalizedTime)
    {
        if (activePrep?.impactCurve == null) return 0f;
        return activePrep.impactCurve.Evaluate(normalizedTime);
    }

    public void PlayLanding(string animationGroupTag, Vector3 goalWorld, LandAnimationPrep prep, float durationSeconds = 1f)
    {
        activeAnimationGroupTag = animationGroupTag ?? "";
        landingGoalWorld = goalWorld;
        hasLandingGoal = true;
        activePrep = prep ?? new LandAnimationPrep();
        activePrep.EnsureReady();
    }
}
