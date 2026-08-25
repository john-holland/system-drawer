using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VehicleProjectedWaypoint
{
    public Vector3 world;
    public float s;
    public Vector3 tangent;
    public float speed;
    public float steerHintSigned01;
    public int trackId;
    public int classId;
    public double tMs;
    public int segmentIndex;
    public int laneIndex = -1;
}

[Serializable]
public sealed class VehicleProjectedSegment
{
    public int index;
    public double startMs;
    public double endMs;
    public float speed;
    public float headingRad;
    public int trackId;
    public int classId;
    public readonly List<VehicleProjectedWaypoint> waypoints = new List<VehicleProjectedWaypoint>();
    public int laneIndex = -1;
}

/// <summary>Result of stitching a VehicleTrack onto a road-center spline.</summary>
public sealed class VehicleProjectionResult
{
    public int subjectTrackId;
    public int subjectClassId;
    public Vector3 seedVelocity;
    public Vector3 seedAngular;
    public readonly List<VehicleProjectedWaypoint> waypoints = new List<VehicleProjectedWaypoint>();
    public readonly List<VehicleProjectedSegment> segments = new List<VehicleProjectedSegment>();
}

/// <summary>
/// Stitches detections across scene-cut segments onto one subject, then snaps to the road center spline.
/// Identity cascade: same trackId, then nearest same classId by spline s, then facing is applied only in unproject.
/// </summary>
public static class VehicleTrackProjector
{
    public const float SampleSpacingMeters = 2f;
    public const float StripWidthMeters = 8f;

    public static VehicleProjectionResult Project(
        VehicleTrack track,
        VehicleRoadCenterSpline spline,
        float facingYawDegrees,
        bool cabinCamera = false)
    {
        var result = new VehicleProjectionResult();
        // Cabin takes never treat YOLO detections as the ego vehicle.
        if (cabinCamera)
            return result;
        if (track == null || spline == null || track.frames == null || track.frames.Length == 0)
            return result;

        VehicleTrackSegment[] segs = track.segments != null && track.segments.Length > 0
            ? track.segments
            : new[]
            {
                new VehicleTrackSegment
                {
                    startMs = track.frames[0].tMs,
                    endMs = track.frames[track.frames.Length - 1].tMs,
                    subjectTrackId = track.frames[0].trackId,
                    subjectClassId = track.frames[0].classId
                }
            };

        VehicleTrackFrame[] firstFrames = FramesInSegment(track.frames, segs[0]);
        VehicleTrackFrame primary = PickPrimarySubject(firstFrames, spline, facingYawDegrees);
        if (primary == null)
            return result;

        int subjectTrackId = primary.trackId;
        int subjectClassId = primary.classId;
        result.subjectTrackId = subjectTrackId;
        result.subjectClassId = subjectClassId;
        float prevS = 0f;
        bool havePrevS = false;
        Vector2 prevCentroid = Vector2.zero;

        for (int si = 0; si < segs.Length; si++)
        {
            VehicleTrackSegment seg = segs[si];
            float facing = ResolveFacing(facingYawDegrees, seg);
            VehicleTrackFrame[] inSeg = FramesInSegment(track.frames, seg);
            var times = DistinctTimes(inSeg);
            var projectedSeg = new VehicleProjectedSegment
            {
                index = si,
                startMs = seg.startMs,
                endMs = seg.endMs,
                headingRad = seg.headingRad + facing * Mathf.Deg2Rad,
                trackId = subjectTrackId,
                classId = subjectClassId
            };

            VehicleProjectedWaypoint lastWp = null;
            for (int ti = 0; ti < times.Count; ti++)
            {
                double tMs = times[ti];
                var atT = FramesAtTime(inSeg, tMs);
                VehicleTrackFrame bound = ContinueIdentity(
                    subjectTrackId,
                    subjectClassId,
                    havePrevS ? prevS : float.NaN,
                    prevCentroid,
                    atT,
                    spline,
                    facing);
                if (bound == null)
                    continue;

                subjectTrackId = bound.trackId;
                subjectClassId = bound.classId;
                Vector3 ground = UnprojectCentroid(bound.cx, bound.cy, spline, facing);
                VehicleRoadCenterSpline.Projection proj = spline.Project(ground);
                var wp = new VehicleProjectedWaypoint
                {
                    world = proj.point,
                    s = proj.s,
                    tangent = proj.tangent,
                    trackId = bound.trackId,
                    classId = bound.classId,
                    tMs = bound.tMs,
                    segmentIndex = si,
                    laneIndex = InferLaneIndex(proj.lateral, bound.cx, spline)
                };
                if (lastWp != null)
                {
                    float dt = (float)((wp.tMs - lastWp.tMs) / 1000.0);
                    float ds = wp.s - lastWp.s;
                    wp.speed = dt > 1e-4f ? Mathf.Abs(ds / dt) : lastWp.speed;
                    wp.steerHintSigned01 = SteerHint(lastWp.tangent, wp.tangent);
                }
                projectedSeg.waypoints.Add(wp);
                result.waypoints.Add(wp);
                lastWp = wp;
                prevS = wp.s;
                havePrevS = true;
                prevCentroid = new Vector2(bound.cx, bound.cy);
            }

            projectedSeg.trackId = subjectTrackId;
            projectedSeg.classId = subjectClassId;
            if (projectedSeg.waypoints.Count > 0)
                projectedSeg.laneIndex = projectedSeg.waypoints[projectedSeg.waypoints.Count - 1].laneIndex;
            if (projectedSeg.waypoints.Count >= 2)
            {
                var a = projectedSeg.waypoints[0];
                var b = projectedSeg.waypoints[projectedSeg.waypoints.Count - 1];
                float dt = (float)((b.tMs - a.tMs) / 1000.0);
                projectedSeg.speed = dt > 1e-4f ? Mathf.Abs((b.s - a.s) / dt) : 0f;
            }
            else if (projectedSeg.waypoints.Count == 1)
                projectedSeg.speed = projectedSeg.waypoints[0].speed;
            result.segments.Add(projectedSeg);
        }

        ResampleSpacing(result, spline, SampleSpacingMeters);
        if (result.waypoints.Count > 0)
        {
            var first = result.waypoints[0];
            float speed = first.speed;
            if (speed < 1e-3f && result.segments.Count > 0)
                speed = result.segments[0].speed;
            result.seedVelocity = first.tangent.normalized * speed;
            if (first.tangent.sqrMagnitude > 1e-6f)
            {
                float yawRate = first.steerHintSigned01 * 0.5f;
                result.seedAngular = new Vector3(0f, yawRate, 0f);
            }
        }

        return result;
    }

    /// <summary>Cabin ego path: integrate polar speed along vehicle forward (optional spline snap).</summary>
    public static VehicleProjectionResult ProjectPolar(
        CabinPolarVelocity polar,
        Vector3 origin,
        Vector3 forward,
        VehicleRoadCenterSpline spline = null,
        float sampleSpacingMeters = SampleSpacingMeters)
    {
        var result = new VehicleProjectionResult();
        if (polar == null || polar.frames == null || polar.frames.Length == 0)
            return result;

        Vector3 fwd = forward.sqrMagnitude > 1e-6f ? forward.normalized : Vector3.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 1e-6f)
            fwd = Vector3.forward;
        fwd.Normalize();

        var seg = new VehicleProjectedSegment { index = 0, startMs = polar.frames[0].tMs, endMs = polar.frames[polar.frames.Length - 1].tMs };
        float arc = 0f;
        float lastKeptArc = -sampleSpacingMeters;
        Vector3 prevTan = fwd;
        for (int i = 0; i < polar.frames.Length; i++)
        {
            var f = polar.frames[i];
            if (i > 0)
            {
                float dt = (float)((f.tMs - polar.frames[i - 1].tMs) / 1000.0);
                if (dt > 0f)
                    arc += Mathf.Max(0f, f.speedHint) * dt;
            }
            Vector3 world = origin + fwd * arc;
            Vector3 tan = fwd;
            float s = arc;
            if (spline != null)
            {
                var proj = spline.Project(world);
                world = proj.point;
                tan = proj.tangent.sqrMagnitude > 1e-6f ? proj.tangent : fwd;
                s = proj.s;
            }
            bool keep = i == 0 || i == polar.frames.Length - 1 || (arc - lastKeptArc) >= sampleSpacingMeters;
            if (!keep)
                continue;
            lastKeptArc = arc;
            var wp = new VehicleProjectedWaypoint
            {
                world = world,
                s = s,
                tangent = tan,
                speed = f.speedHint,
                steerHintSigned01 = Mathf.Clamp(f.yawRateHint / 0.5f, -1f, 1f),
                tMs = f.tMs,
                segmentIndex = 0,
                laneIndex = spline != null ? InferLaneIndex(spline.Project(world).lateral, 0.5f, spline) : -1
            };
            if (result.waypoints.Count > 0)
                wp.steerHintSigned01 = SteerHint(prevTan, tan);
            prevTan = tan;
            result.waypoints.Add(wp);
            seg.waypoints.Add(wp);
        }
        if (result.waypoints.Count > 0)
        {
            result.seedVelocity = result.waypoints[0].tangent.normalized * result.waypoints[0].speed;
            result.seedAngular = Vector3.up * (polar.frames[0].yawRateHint);
            seg.speed = result.waypoints[0].speed;
            seg.endMs = result.waypoints[result.waypoints.Count - 1].tMs;
        }
        result.segments.Add(seg);
        return result;
    }

    /// <summary>Largest bbox of the chosen type that projects onto the spline; else largest vehicle bbox.</summary>
    public static VehicleTrackFrame PickPrimarySubject(
        VehicleTrackFrame[] frames,
        VehicleRoadCenterSpline spline,
        float facingYawDegrees,
        int preferredClassId = -1)
    {
        if (frames == null || frames.Length == 0)
            return null;

        VehicleTrackFrame bestTyped = null;
        float bestTypedArea = -1f;
        VehicleTrackFrame bestAny = null;
        float bestAnyArea = -1f;
        for (int i = 0; i < frames.Length; i++)
        {
            var f = frames[i];
            if (f == null) continue;
            float area = f.bbox != null ? f.bbox.Area : 0f;
            if (area > bestAnyArea)
            {
                bestAnyArea = area;
                bestAny = f;
            }
            bool typeOk = preferredClassId < 0 || f.classId == preferredClassId;
            if (!typeOk) continue;
            if (spline != null)
            {
                Vector3 g = UnprojectCentroid(f.cx, f.cy, spline, facingYawDegrees);
                var proj = spline.Project(g);
                if ((proj.point - g).sqrMagnitude > 25f) // more than 5m off the strip: skip typed
                    continue;
            }
            if (area > bestTypedArea)
            {
                bestTypedArea = area;
                bestTyped = f;
            }
        }
        return bestTyped != null ? bestTyped : bestAny;
    }

    /// <summary>Strict identity: same trackId, then nearest same classId by spline s (ties by bbox centroid). Facing is not used.</summary>
    public static VehicleTrackFrame ContinueIdentity(
        int subjectTrackId,
        int subjectClassId,
        float previousS,
        Vector2 previousCentroid,
        VehicleTrackFrame[] candidates,
        VehicleRoadCenterSpline spline,
        float facingYawDegrees)
    {
        if (candidates == null || candidates.Length == 0)
            return null;
        _ = facingYawDegrees;

        for (int i = 0; i < candidates.Length; i++)
        {
            if (candidates[i] != null && candidates[i].trackId == subjectTrackId)
                return candidates[i];
        }

        VehicleTrackFrame bestSame = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < candidates.Length; i++)
        {
            var c = candidates[i];
            if (c == null || c.classId != subjectClassId)
                continue;
            float sScore = 0f;
            if (spline != null && !float.IsNaN(previousS))
            {
                // Identity ignores facing; facing only reprojects after id/type bind.
                Vector3 g = UnprojectCentroid(c.cx, c.cy, spline, 0f);
                sScore = Mathf.Abs(spline.Project(g).s - previousS);
            }
            Vector2 cen = new Vector2(c.cx, c.cy);
            float cenDist = (cen - previousCentroid).sqrMagnitude;
            float score = sScore + cenDist * 0.01f;
            if (score < bestScore)
            {
                bestScore = score;
                bestSame = c;
            }
        }
        return bestSame;
    }

    public static Vector3 UnprojectCentroid(float cx, float cy, VehicleRoadCenterSpline spline, float facingYawDegrees)
    {
        float yaw = facingYawDegrees * Mathf.Deg2Rad;
        Vector3 origin = spline != null ? spline.Sample(0f) : Vector3.zero;
        Vector3 fwd = new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw));
        Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
        float stripLen = spline != null ? Mathf.Max(1f, spline.GetTotalLength()) : 16f;
        float depth = (1f - Mathf.Clamp01(cy)) * stripLen;
        float lateral = (cx - 0.5f) * StripWidthMeters;
        return origin + fwd * depth + right * lateral;
    }

    public static void ApplyToTravelAgent(VehicleProjectionResult result, TravelAgent agent, VehicleActor vehicle = null)
    {
        if (agent == null || result == null)
            return;
        if (agent.authoringRows == null)
            agent.authoringRows = new List<TravelAuthoringRow>();
        agent.authoringRows.Clear();
        var plan = new GenericMultiModalPathPlan();
        plan.segments = new List<MultiModalSegment>();

        if (result.segments.Count == 0 && result.waypoints.Count > 0)
        {
            AppendDriveSegment(plan, agent, result.waypoints, vehicle);
        }
        else
        {
            for (int i = 0; i < result.segments.Count; i++)
            {
                var seg = result.segments[i];
                if (seg.waypoints.Count == 0)
                    continue;
                AppendDriveSegment(plan, agent, seg.waypoints, vehicle);
            }
        }

        if (result.waypoints.Count > 0)
        {
            agent.previewStartWorld = result.waypoints[0].world;
            agent.previewGoalWorld = result.waypoints[result.waypoints.Count - 1].world;
        }
        if (vehicle != null)
            agent.hintVehicle = vehicle;
        agent.ReplaceCachedPlan(plan);
        if (agent.roadTravelBinding != null && plan.segments != null)
        {
            for (int i = 0; i < plan.segments.Count; i++)
                agent.roadTravelBinding.EnrichDriveSegment(plan.segments[i]);
        }
    }

    static void AppendDriveSegment(
        GenericMultiModalPathPlan plan,
        TravelAgent agent,
        List<VehicleProjectedWaypoint> wps,
        VehicleActor vehicle)
    {
        var mm = new MultiModalSegment
        {
            mode = TravelLegMode.Drive,
            medium = PhysicalPathingMedium.Ground,
            waypoints = new List<Vector3>(),
            optionalVehicleHint = vehicle
        };
        for (int i = 0; i < wps.Count; i++)
        {
            mm.waypoints.Add(wps[i].world);
            agent.authoringRows.Add(new TravelAuthoringRow
            {
                kind = TravelAuthoringRowKind.Coordinate,
                worldPosition = wps[i].world,
                notes = $"veh track {wps[i].trackId} s={wps[i].s:0.0}"
            });
        }
        if (mm.waypoints.Count > 0)
            mm.segmentEnd = mm.waypoints[mm.waypoints.Count - 1];
        plan.segments.Add(mm);
    }

    public static bool TrySample(VehicleProjectionResult result, double tMs, out VehicleProjectedWaypoint wp)
    {
        wp = null;
        if (result == null || result.waypoints == null || result.waypoints.Count == 0)
            return false;
        var wps = result.waypoints;
        if (wps.Count == 1 || tMs <= wps[0].tMs)
        {
            wp = wps[0];
            return true;
        }
        int last = wps.Count - 1;
        if (tMs >= wps[last].tMs)
        {
            wp = wps[last];
            return true;
        }
        for (int i = 1; i < wps.Count; i++)
        {
            if (tMs > wps[i].tMs) continue;
            var a = wps[i - 1];
            var b = wps[i];
            double span = b.tMs - a.tMs;
            float u = span <= 1e-6 ? 0f : (float)((tMs - a.tMs) / span);
            wp = new VehicleProjectedWaypoint
            {
                world = Vector3.Lerp(a.world, b.world, u),
                s = Mathf.Lerp(a.s, b.s, u),
                tangent = Vector3.Slerp(a.tangent.sqrMagnitude > 1e-6f ? a.tangent : Vector3.forward,
                    b.tangent.sqrMagnitude > 1e-6f ? b.tangent : Vector3.forward, u),
                speed = Mathf.Lerp(a.speed, b.speed, u),
                steerHintSigned01 = Mathf.Lerp(a.steerHintSigned01, b.steerHintSigned01, u),
                trackId = b.trackId,
                classId = b.classId,
                tMs = tMs,
                segmentIndex = b.segmentIndex,
                laneIndex = b.laneIndex
            };
            return true;
        }
        wp = wps[last];
        return true;
    }

    public static int InferLaneIndex(float lateral, float cx, VehicleRoadCenterSpline spline)
    {
        RoadLaneLayout layout = null;
        if (spline != null)
        {
            var bind = spline.GetComponent<RoadLaneSplineBinding>();
            if (bind != null) layout = bind.ResolveLayout();
        }
        if (layout == null)
        {
            layout = new RoadLaneLayout { laneCount = 2, laneWidthM = Mathf.Max(1f, StripWidthMeters / 2f) };
        }
        if (Mathf.Abs(lateral) < 1e-4f && cx > 0f)
        {
            // Image bins: left of center → lane 0, right → last lane.
            float half = 0.5f;
            lateral = (cx - half) * layout.laneWidthM * layout.laneCount;
        }
        return layout.LaneFromLateral(lateral);
    }

    static float SteerHint(Vector3 prevTan, Vector3 tan)
    {
        Vector3 a = prevTan;
        Vector3 b = tan;
        a.y = 0f;
        b.y = 0f;
        if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f)
            return 0f;
        a.Normalize();
        b.Normalize();
        float cross = a.x * b.z - a.z * b.x;
        float dot = Vector3.Dot(a, b);
        float ang = Mathf.Atan2(cross, dot);
        return Mathf.Clamp(ang / (Mathf.PI * 0.25f), -1f, 1f);
    }

    static void ResampleSpacing(VehicleProjectionResult result, VehicleRoadCenterSpline spline, float spacing)
    {
        if (result.waypoints.Count < 2 || spline == null || spacing <= 0.1f)
            return;
        var dense = new List<VehicleProjectedWaypoint>(result.waypoints);
        result.waypoints.Clear();
        result.waypoints.Add(dense[0]);
        float acc = 0f;
        for (int i = 1; i < dense.Count; i++)
        {
            acc += Mathf.Abs(dense[i].s - dense[i - 1].s);
            if (acc >= spacing || i == dense.Count - 1)
            {
                result.waypoints.Add(dense[i]);
                acc = 0f;
            }
        }
        for (int s = 0; s < result.segments.Count; s++)
        {
            var seg = result.segments[s];
            var kept = new List<VehicleProjectedWaypoint>();
            for (int i = 0; i < result.waypoints.Count; i++)
            {
                if (result.waypoints[i].segmentIndex == seg.index)
                    kept.Add(result.waypoints[i]);
            }
            if (kept.Count == 0 && seg.waypoints.Count > 0)
            {
                kept.Add(seg.waypoints[0]);
                kept.Add(seg.waypoints[seg.waypoints.Count - 1]);
            }
            seg.waypoints.Clear();
            seg.waypoints.AddRange(kept);
        }
    }

    static float ResolveFacing(float assetFacing, VehicleTrackSegment seg)
    {
        if (seg != null && seg.hasFacingYawOverride)
            return seg.facingYawOverride;
        return assetFacing;
    }

    static VehicleTrackFrame[] FramesInSegment(VehicleTrackFrame[] frames, VehicleTrackSegment seg)
    {
        var list = new List<VehicleTrackFrame>();
        if (frames == null) return Array.Empty<VehicleTrackFrame>();
        double lo = seg != null ? seg.startMs : double.NegativeInfinity;
        double hi = seg != null ? seg.endMs : double.PositiveInfinity;
        for (int i = 0; i < frames.Length; i++)
        {
            var f = frames[i];
            if (f == null) continue;
            if (f.tMs + 1e-3 >= lo && f.tMs - 1e-3 <= hi)
                list.Add(f);
        }
        return list.ToArray();
    }

    static List<double> DistinctTimes(VehicleTrackFrame[] frames)
    {
        var times = new List<double>();
        if (frames == null) return times;
        for (int i = 0; i < frames.Length; i++)
        {
            double t = frames[i].tMs;
            bool seen = false;
            for (int j = 0; j < times.Count; j++)
            {
                if (Math.Abs(times[j] - t) < 0.5)
                {
                    seen = true;
                    break;
                }
            }
            if (!seen)
                times.Add(t);
        }
        times.Sort();
        return times;
    }

    static VehicleTrackFrame[] FramesAtTime(VehicleTrackFrame[] frames, double tMs)
    {
        var list = new List<VehicleTrackFrame>();
        if (frames == null) return Array.Empty<VehicleTrackFrame>();
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null && Math.Abs(frames[i].tMs - tMs) < 0.5)
                list.Add(frames[i]);
        }
        return list.ToArray();
    }
}
