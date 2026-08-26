using System.Collections.Generic;
using UnityEngine;

/// <summary>Reynolds flocking as a crowd nuance layer on TravelAgentRegistry. Shared route stays WaypointGuidanceService.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Travel/Boids Crowd Layer")]
public sealed class BoidsCrowdLayer : MonoBehaviour
{
    public float neighborRadius = 4f;
    public float separationWeight = 1.1f;
    public float alignmentWeight = 0.6f;
    public float cohesionWeight = 0.45f;
    public float maxLateralM = 1.25f;
    public bool applyToCachedPlan = true;

    void LateUpdate()
    {
        if (!applyToCachedPlan) return;
        var all = TravelAgentRegistry.All;
        for (int i = 0; i < all.Count; i++)
        {
            var self = all[i];
            if (self == null || self.crowdHint == CityPixelCrowdHint.None) continue;
            Vector3 offset = ComputeLateralOffset(self, all);
            ApplyOffset(self, offset);
        }
    }

    public Vector3 ComputeLateralOffset(TravelAgent self, IReadOnlyList<TravelAgent> peers)
    {
        return ComputeSteering(
            self != null ? self.transform.position : Vector3.zero,
            self != null ? self.transform.forward : Vector3.forward,
            self != null ? self.flockGroupId : null,
            peers,
            neighborRadius,
            separationWeight,
            alignmentWeight,
            cohesionWeight,
            maxLateralM);
    }

    public static Vector3 ComputeSteering(
        Vector3 selfPos,
        Vector3 selfFwd,
        string flockGroupId,
        IReadOnlyList<TravelAgent> peers,
        float neighborRadius,
        float sepW,
        float aliW,
        float cohW,
        float maxLateralM)
    {
        Vector3 sep = Vector3.zero;
        Vector3 ali = Vector3.zero;
        Vector3 coh = Vector3.zero;
        int n = 0;
        float r2 = neighborRadius * neighborRadius;
        if (peers != null)
        {
            for (int i = 0; i < peers.Count; i++)
            {
                var p = peers[i];
                if (p == null) continue;
                Vector3 pp = p.transform.position;
                if ((pp - selfPos).sqrMagnitude < 1e-8f) continue;
                if (!string.IsNullOrEmpty(flockGroupId) && p.flockGroupId != flockGroupId)
                    continue;
                Vector3 d = selfPos - pp;
                d.y = 0f;
                float mag2 = d.sqrMagnitude;
                if (mag2 > r2 || mag2 < 1e-8f) continue;
                sep += d / mag2;
                ali += p.transform.forward;
                coh += pp;
                n++;
            }
        }
        if (n == 0) return Vector3.zero;
        sep /= n;
        ali = (ali / n) - selfFwd;
        coh = (coh / n) - selfPos;
        sep.y = 0f;
        ali.y = 0f;
        coh.y = 0f;
        Vector3 v = sep * sepW + ali * aliW + coh * cohW;
        v.y = 0f;
        if (v.sqrMagnitude > maxLateralM * maxLateralM)
            v = v.normalized * maxLateralM;
        return v;
    }

    /// <summary>Separation-only helper for tests (two positions).</summary>
    public static Vector3 SeparationOffset(Vector3 self, Vector3 other, float maxLateralM = 1.25f)
    {
        Vector3 d = self - other;
        d.y = 0f;
        if (d.sqrMagnitude < 1e-8f)
            d = Vector3.right * 0.01f;
        d = d.normalized * Mathf.Min(maxLateralM, 1f / Mathf.Max(0.05f, d.magnitude));
        if (d.sqrMagnitude > maxLateralM * maxLateralM)
            d = d.normalized * maxLateralM;
        return d;
    }

    static void ApplyOffset(TravelAgent agent, Vector3 offset)
    {
        if (agent == null || offset.sqrMagnitude < 1e-6f) return;
        var plan = agent.CachedPlan;
        if (plan == null || plan.segments == null) return;
        for (int s = 0; s < plan.segments.Count; s++)
        {
            var seg = plan.segments[s];
            if (seg?.waypoints == null) continue;
            for (int w = 0; w < seg.waypoints.Count; w++)
                seg.waypoints[w] += offset;
        }
    }
}
