using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Last published stunt/safety plan forks for broccoli-plume emergence viz.
/// </summary>
public static class StuntPlanEmergenceBuffer
{
    public struct Branch
    {
        public Vector3 a;
        public Vector3 b;
        public float weight;
        public float fade01;
        public bool stuntmanPreferred;
    }

    static readonly List<Branch> Branches = new List<Branch>();
    static float _publishedAt = -999f;

    public static IReadOnlyList<Branch> Current => Branches;

    public static void Publish(GenericMultiModalPathPlan plan)
    {
        Branches.Clear();
        _publishedAt = Time.realtimeSinceStartup;
        if (plan?.segments == null) return;

        PublishPolyline(plan.segments, 1f, stuntmanPreferred: true, fadeBase: 0.15f);
        if (plan.rejectedForks != null)
            PublishPolyline(plan.rejectedForks, 0.65f, stuntmanPreferred: false, fadeBase: 0.55f);
    }

    static void PublishPolyline(List<MultiModalSegment> segs, float weight, bool stuntmanPreferred, float fadeBase)
    {
        for (int s = 0; s < segs.Count; s++)
        {
            var seg = segs[s];
            if (seg?.waypoints == null || seg.waypoints.Count < 2) continue;
            float risk = seg.runningTotals.risk;
            float w = weight * (stuntmanPreferred ? 0.5f + risk * 0.5f : 0.35f + (1f - risk) * 0.4f);
            for (int i = 1; i < seg.waypoints.Count; i++)
            {
                float along = i / (float)seg.waypoints.Count;
                Branches.Add(new Branch
                {
                    a = seg.waypoints[i - 1],
                    b = seg.waypoints[i],
                    weight = w,
                    fade01 = Mathf.Clamp01(fadeBase + along * 0.7f),
                    stuntmanPreferred = stuntmanPreferred
                });
            }
        }
    }

    public static float AgeSeconds => Time.realtimeSinceStartup - _publishedAt;

    public static void Clear() => Branches.Clear();
}
