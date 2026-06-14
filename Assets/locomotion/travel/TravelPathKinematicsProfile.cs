using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Velocity / tangent sample along a multi-modal plan for skier-style visualization.
/// </summary>
public struct TravelPathSample
{
    public Vector3 position;
    public Vector3 tangent;
    public Vector3 velocity;
    public TravelLegMode mode;
    public float speed;
    public float arcLength;
    public float arcLength01;
    public bool reverse;
}

/// <summary>
/// Builds arc-length samples from a plan with optional reverse tail within budget.
/// </summary>
public sealed class TravelPathKinematicsProfile
{
    public IReadOnlyList<TravelPathSample> Samples => _samples;
    public float TotalPathLengthMeters { get; private set; }
    public float ReverseBudgetMeters { get; private set; }
    public float ReverseLegLimit01 { get; private set; }

    readonly List<TravelPathSample> _samples = new List<TravelPathSample>();

    public static TravelPathKinematicsProfile Build(
        GenericMultiModalPathPlan plan,
        float reverseLegLimit01,
        float sampleSpacingMeters = 2f)
    {
        var profile = new TravelPathKinematicsProfile();
        profile.ReverseLegLimit01 = reverseLegLimit01;
        profile.TotalPathLengthMeters = TravelPathReverseLimits.ComputeTotalPathLengthMeters(plan);
        profile.ReverseBudgetMeters = TravelPathReverseLimits.ReverseBudgetMeters(
            reverseLegLimit01, profile.TotalPathLengthMeters);

        if (plan == null || plan.IsEmpty || profile.TotalPathLengthMeters <= 1e-4f)
            return profile;

        BuildForwardSamples(plan, profile, sampleSpacingMeters);
        if (TravelPathReverseLimits.AllowsReverse(reverseLegLimit01) && profile.ReverseBudgetMeters > 1e-4f)
            AppendReverseTail(plan, profile, sampleSpacingMeters);

        return profile;
    }

    static void BuildForwardSamples(GenericMultiModalPathPlan plan, TravelPathKinematicsProfile profile, float spacing)
    {
        float arc = 0f;
        float nextSampleAt = 0f;

        for (int s = 0; s < plan.segments.Count; s++)
        {
            MultiModalSegment seg = plan.segments[s];
            if (seg?.waypoints == null || seg.waypoints.Count < 2)
                continue;

            float segTime = Mathf.Max(seg.estimatedTimeSec, 0.01f);
            for (int i = 1; i < seg.waypoints.Count; i++)
            {
                Vector3 a = seg.waypoints[i - 1];
                Vector3 b = seg.waypoints[i];
                float edgeLen = Vector3.Distance(a, b);
                if (edgeLen < 1e-5f)
                    continue;

                Vector3 tangent = (b - a) / edgeLen;
                float edgeTime = segTime * (edgeLen / SegmentLength(seg));
                float speed = edgeLen / Mathf.Max(edgeTime, 0.01f);

                while (arc + edgeLen >= nextSampleAt - 1e-4f)
                {
                    float t = edgeLen > 1e-5f ? (nextSampleAt - arc) / edgeLen : 0f;
                    t = Mathf.Clamp01(t);
                    profile._samples.Add(new TravelPathSample
                    {
                        position = Vector3.Lerp(a, b, t),
                        tangent = tangent,
                        velocity = tangent * speed,
                        mode = seg.mode,
                        speed = speed,
                        arcLength = nextSampleAt,
                        arcLength01 = nextSampleAt / profile.TotalPathLengthMeters,
                        reverse = false
                    });
                    nextSampleAt += spacing;
                    if (nextSampleAt > profile.TotalPathLengthMeters + spacing)
                        break;
                }

                arc += edgeLen;
            }
        }
    }

    static void AppendReverseTail(GenericMultiModalPathPlan plan, TravelPathKinematicsProfile profile, float spacing)
    {
        float reverseStart = profile.TotalPathLengthMeters - profile.ReverseBudgetMeters;
        float arc = profile.TotalPathLengthMeters;
        float nextSampleAt = profile.TotalPathLengthMeters - spacing * 0.5f;

        for (int s = plan.segments.Count - 1; s >= 0 && nextSampleAt >= reverseStart; s--)
        {
            MultiModalSegment seg = plan.segments[s];
            if (seg?.waypoints == null || seg.waypoints.Count < 2)
                continue;

            float segTime = Mathf.Max(seg.estimatedTimeSec, 0.01f);
            for (int i = seg.waypoints.Count - 1; i >= 1; i--)
            {
                Vector3 b = seg.waypoints[i];
                Vector3 a = seg.waypoints[i - 1];
                float edgeLen = Vector3.Distance(a, b);
                if (edgeLen < 1e-5f)
                    continue;

                arc -= edgeLen;
                Vector3 tangent = (a - b) / edgeLen;
                float edgeTime = segTime * (edgeLen / SegmentLength(seg));
                float speed = edgeLen / Mathf.Max(edgeTime, 0.01f);

                while (nextSampleAt >= arc - 1e-4f && nextSampleAt >= reverseStart)
                {
                    float t = edgeLen > 1e-5f ? (nextSampleAt - arc) / edgeLen : 0f;
                    t = Mathf.Clamp01(t);
                    profile._samples.Add(new TravelPathSample
                    {
                        position = Vector3.Lerp(b, a, t),
                        tangent = tangent,
                        velocity = tangent * speed,
                        mode = seg.mode,
                        speed = speed,
                        arcLength = nextSampleAt,
                        arcLength01 = nextSampleAt / profile.TotalPathLengthMeters,
                        reverse = true
                    });
                    nextSampleAt -= spacing;
                }
            }
        }
    }

    static float SegmentLength(MultiModalSegment seg)
    {
        if (seg?.waypoints == null || seg.waypoints.Count < 2)
            return 1f;
        float len = 0f;
        for (int i = 1; i < seg.waypoints.Count; i++)
            len += Vector3.Distance(seg.waypoints[i - 1], seg.waypoints[i]);
        return Mathf.Max(len, 0.01f);
    }
}
