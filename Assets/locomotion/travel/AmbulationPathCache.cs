using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared ambulation polylines keyed by route/start-goal + obstacle fingerprint.
/// Reuse when start/goal stay within cacheToleranceM. Likelihood is per-actor (non-human defaults higher).
/// </summary>
public static class AmbulationPathCache
{
    public const float HumanLikelihood01 = 0.35f;
    public const float NonHumanLikelihood01 = 0.85f;

    struct Entry
    {
        public Vector3 start;
        public Vector3 goal;
        public int fingerprint;
        public GenericMultiModalPathPlan plan;
        public float toleranceM;
    }

    static readonly Dictionary<string, Entry> s_byKey = new Dictionary<string, Entry>();

    public static void Clear() => s_byKey.Clear();

    public static float ResolveLikelihood01(TravelAgent agent)
    {
        if (agent == null) return HumanLikelihood01;
        if (agent.ambulationCacheLikelihood01 >= 0f)
            return Mathf.Clamp01(agent.ambulationCacheLikelihood01);
        return DefaultLikelihood01(agent.ambulatingActor);
    }

    public static float DefaultLikelihood01(BaseAmbulatingActor actor)
    {
        if (actor is VehicleActor || actor is AnimalAmbulatingActor)
            return NonHumanLikelihood01;
        return HumanLikelihood01;
    }

    public static bool WithinTolerance(Vector3 a, Vector3 b, float toleranceM)
    {
        float t = Mathf.Max(0.01f, toleranceM);
        return (a - b).sqrMagnitude <= t * t;
    }

    public static string MakeKey(string cacheKey, Vector3 start, Vector3 goal, float quantizeM, int fingerprint)
    {
        if (!string.IsNullOrEmpty(cacheKey))
            return cacheKey + ":" + fingerprint;
        Vector3 qs = Quantize(start, quantizeM);
        Vector3 qg = Quantize(goal, quantizeM);
        return $"{qs.x:0.##}_{qs.y:0.##}_{qs.z:0.##}>{qg.x:0.##}_{qg.y:0.##}_{qg.z:0.##}:{fingerprint}";
    }

    public static int ObstacleFingerprint(TravelAgent agent)
    {
        int h = 17;
        if (agent == null) return h;
        h = h * 31 + (agent.ignoreTrafficAvoidance ? 1 : 0);
        var avoids = agent.avoidActors;
        if (avoids == null) return h;
        for (int i = 0; i < avoids.Count; i++)
        {
            if (avoids[i] == null) continue;
            Vector3 q = Quantize(avoids[i].position, 2f);
            h = h * 31 + q.x.GetHashCode();
            h = h * 31 + q.z.GetHashCode();
        }
        return h;
    }

    public static bool TryReuse(TravelAgent agent, out GenericMultiModalPathPlan plan)
    {
        plan = null;
        if (agent == null) return false;
        float likelihood = ResolveLikelihood01(agent);
        if (likelihood <= 0f) return false;
        if (Random.value > likelihood) return false;

        float tol = agent.cacheToleranceM > 0f ? agent.cacheToleranceM : 1.5f;
        int fp = ObstacleFingerprint(agent);
        string key = MakeKey(agent.ambulationCacheKey, agent.previewStartWorld, agent.previewGoalWorld, tol, fp);
        if (!s_byKey.TryGetValue(key, out var e))
        {
            foreach (var kv in s_byKey)
            {
                var cand = kv.Value;
                if (cand.fingerprint != fp) continue;
                if (!WithinTolerance(cand.start, agent.previewStartWorld, tol)) continue;
                if (!WithinTolerance(cand.goal, agent.previewGoalWorld, tol)) continue;
                e = cand;
                key = kv.Key;
                break;
            }
            if (e.plan == null) return false;
        }
        else if (!WithinTolerance(e.start, agent.previewStartWorld, tol)
                 || !WithinTolerance(e.goal, agent.previewGoalWorld, tol)
                 || e.fingerprint != fp)
            return false;

        plan = e.plan != null ? e.plan.Clone() : null;
        return plan != null && !plan.IsEmpty;
    }

    public static void Remember(TravelAgent agent, GenericMultiModalPathPlan plan)
    {
        if (agent == null || plan == null || plan.IsEmpty) return;
        float tol = agent.cacheToleranceM > 0f ? agent.cacheToleranceM : 1.5f;
        int fp = ObstacleFingerprint(agent);
        string key = MakeKey(agent.ambulationCacheKey, agent.previewStartWorld, agent.previewGoalWorld, tol, fp);
        s_byKey[key] = new Entry
        {
            start = agent.previewStartWorld,
            goal = agent.previewGoalWorld,
            fingerprint = fp,
            plan = plan.Clone(),
            toleranceM = tol
        };
    }

    /// <summary>Direct put/get for tests (no TravelAgent / likelihood roll).</summary>
    public static void Put(string key, Vector3 start, Vector3 goal, int fingerprint, GenericMultiModalPathPlan plan, float toleranceM)
    {
        s_byKey[key] = new Entry
        {
            start = start,
            goal = goal,
            fingerprint = fingerprint,
            plan = plan,
            toleranceM = toleranceM
        };
    }

    public static bool TryGet(string key, Vector3 start, Vector3 goal, int fingerprint, float toleranceM, out GenericMultiModalPathPlan plan)
    {
        plan = null;
        if (!s_byKey.TryGetValue(key, out var e)) return false;
        if (e.fingerprint != fingerprint) return false;
        if (!WithinTolerance(e.start, start, toleranceM)) return false;
        if (!WithinTolerance(e.goal, goal, toleranceM)) return false;
        plan = e.plan;
        return plan != null;
    }

    static Vector3 Quantize(Vector3 p, float step)
    {
        float s = Mathf.Max(0.25f, step);
        return new Vector3(Mathf.Round(p.x / s) * s, Mathf.Round(p.y / s) * s, Mathf.Round(p.z / s) * s);
    }
}
