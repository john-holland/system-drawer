using System.Collections.Generic;
using UnityEngine;

/// <summary>Polar slots, 9-way origins, wrap, and working-joint solve.</summary>
public static class RadialSlotMath
{
    public const float StartPostMatchMeters = 0.08f;
    public const float StartPostMatchDegrees = 12f;
    public const float JointMateMeters = 0.12f;

    public static Vector3 SideOrigin(Bounds cellBounds, RadialSide side)
    {
        Vector3 c = cellBounds.center;
        Vector3 e = cellBounds.extents;
        switch (side)
        {
            case RadialSide.UpperLeft: return c + new Vector3(-e.x, e.y, 0f);
            case RadialSide.Up: return c + new Vector3(0f, e.y, 0f);
            case RadialSide.UpperRight: return c + new Vector3(e.x, e.y, 0f);
            case RadialSide.Right: return c + new Vector3(e.x, 0f, 0f);
            case RadialSide.LowerRight: return c + new Vector3(e.x, -e.y, 0f);
            case RadialSide.Bottom: return c + new Vector3(0f, -e.y, 0f);
            case RadialSide.LowerLeft: return c + new Vector3(-e.x, -e.y, 0f);
            case RadialSide.Left: return c + new Vector3(-e.x, 0f, 0f);
            default: return c;
        }
    }

    public static float SlotAngleDeg(int index, int count, float startAngleDeg, float wrapAngleDeg)
    {
        float wrap = wrapAngleDeg <= 0f ? 360f : wrapAngleDeg;
        int n = Mathf.Max(1, count);
        int i = ((index % n) + n) % n;
        if (n == 1)
            return startAngleDeg;
        bool closed = Mathf.Abs(wrap - 360f) < 0.05f;
        if (closed)
            return startAngleDeg + wrap * i / n;
        return startAngleDeg + wrap * i / (n - 1);
    }

    public static Vector3 PolarSlot(
        Vector3 center,
        Vector3 axis,
        float radius,
        int index,
        int count,
        float startAngleDeg,
        float wrapAngleDeg)
    {
        Vector3 a = axis.sqrMagnitude > 1e-8f ? axis.normalized : Vector3.up;
        Vector3 radial = Quaternion.AngleAxis(SlotAngleDeg(index, count, startAngleDeg, wrapAngleDeg), a)
                         * Vector3.ProjectOnPlane(Vector3.forward, a).normalized;
        if (radial.sqrMagnitude < 1e-8f)
            radial = Quaternion.AngleAxis(SlotAngleDeg(index, count, startAngleDeg, wrapAngleDeg), a)
                     * Vector3.ProjectOnPlane(Vector3.right, a).normalized;
        return center + radial * Mathf.Max(0f, radius);
    }

    public static Quaternion YawToCenter(Vector3 slot, Vector3 center, Vector3 axis)
    {
        Vector3 a = axis.sqrMagnitude > 1e-8f ? axis.normalized : Vector3.up;
        Vector3 inward = Vector3.ProjectOnPlane(center - slot, a);
        if (inward.sqrMagnitude < 1e-8f)
            return Quaternion.identity;
        return Quaternion.LookRotation(inward.normalized, a);
    }

    public static float ResolveWrapDeg(
        CustomRadialSidePose pose,
        Vector3 center,
        Vector3 axis,
        Vector3 startWorld,
        bool hasStart)
    {
        if (pose.hasCustomAngleObject)
        {
            Vector3 from = hasStart ? startWorld : center + Vector3.forward;
            return SignedAzimuthDeg(center, axis, from, pose.customAngleObjectWorld);
        }
        if (pose.customAngle > 0f)
            return pose.customAngle;
        return 360f;
    }

    public static float SignedAzimuthDeg(Vector3 center, Vector3 axis, Vector3 from, Vector3 to)
    {
        Vector3 a = axis.sqrMagnitude > 1e-8f ? axis.normalized : Vector3.up;
        Vector3 f = Vector3.ProjectOnPlane(from - center, a);
        Vector3 t = Vector3.ProjectOnPlane(to - center, a);
        if (f.sqrMagnitude < 1e-10f || t.sqrMagnitude < 1e-10f)
            return 0f;
        return Vector3.SignedAngle(f, t, a);
    }

    public static float NaturalRadius(Vector3 pieceSize, int count, float wrapAngleDeg, float joinOffset)
    {
        int n = Mathf.Max(1, count);
        float chord = Mathf.Max(pieceSize.x, pieceSize.z) + Mathf.Max(0f, joinOffset);
        if (n == 1)
            return chord * 0.5f;
        float wrap = wrapAngleDeg <= 0f ? 360f : wrapAngleDeg;
        float stepDeg = Mathf.Abs(wrap - 360f) < 0.05f ? wrap / n : wrap / Mathf.Max(1, n - 1);
        float half = stepDeg * 0.5f * Mathf.Deg2Rad;
        float s = Mathf.Sin(Mathf.Max(1e-4f, half));
        return chord * 0.5f / s;
    }

    public static void NeighborJoinPoints(
        Vector3 center,
        Vector3 axis,
        float radius,
        int index,
        int count,
        float startAngleDeg,
        float wrapAngleDeg,
        Vector3 pieceSize,
        float joinOffset,
        out Vector3 towardPrev,
        out Vector3 towardNext)
    {
        Vector3 slot = PolarSlot(center, axis, radius, index, count, startAngleDeg, wrapAngleDeg);
        Vector3 prev = PolarSlot(center, axis, radius, index - 1, count, startAngleDeg, wrapAngleDeg);
        Vector3 next = PolarSlot(center, axis, radius, index + 1, count, startAngleDeg, wrapAngleDeg);
        float half = Mathf.Max(pieceSize.x, pieceSize.z) * 0.5f + joinOffset * 0.5f;
        towardPrev = slot + (prev - slot).normalized * half;
        towardNext = slot + (next - slot).normalized * half;
    }

    public static bool MatchesStartPost(
        Vector3 slot0,
        Quaternion slot0Yaw,
        Vector3 startAnchor,
        Vector3 startFacing,
        Vector3 axis)
    {
        if ((slot0 - startAnchor).sqrMagnitude > StartPostMatchMeters * StartPostMatchMeters)
            return false;
        if (startFacing.sqrMagnitude < 1e-8f)
            return true;
        Vector3 a = axis.sqrMagnitude > 1e-8f ? axis.normalized : Vector3.up;
        Vector3 face = Vector3.ProjectOnPlane(startFacing, a);
        Vector3 yaw = Vector3.ProjectOnPlane(slot0Yaw * Vector3.forward, a);
        if (face.sqrMagnitude < 1e-8f || yaw.sqrMagnitude < 1e-8f)
            return true;
        // Bounds face the first piece; slot yaw looks inward. Same axis, either sense, is a match.
        float ang = Mathf.Abs(Vector3.SignedAngle(yaw, face, a));
        return ang <= StartPostMatchDegrees || Mathf.Abs(ang - 180f) <= StartPostMatchDegrees;
    }

    public static bool FlyAwayOverlaps(Bounds a, Bounds b)
    {
        return a.Intersects(b);
    }

    /// <summary>
    /// Rank layouts. When startPost is set, returned configs all match that pose
    /// (custom wrap or auto). When unset, all working joints are returned.
    /// </summary>
    public static List<RadialSolvedConfig> SolveWorkingJoints(
        CustomRadialSidePose piecePose,
        Vector3 pieceSize,
        RadialJoinKind joinKind,
        float joinOffset,
        Vector3 center,
        Vector3 axis,
        bool hasStartPost,
        Vector3 startPostAnchor,
        Vector3 startPostFacing,
        IList<CustomRadialSidePose> sideCandidates = null)
    {
        var results = new List<RadialSolvedConfig>();
        float wrapHint = ResolveWrapDeg(piecePose, center, axis, startPostAnchor, hasStartPost);
        float startHint = 0f;
        if (hasStartPost)
            startHint = SignedAzimuthDeg(center, axis, center + Vector3.forward, startPostAnchor);

        int[] counts = { 2, 3, 4, 5, 6, 8, 12 };
        float[] wraps = wrapHint > 0.05f && Mathf.Abs(wrapHint - 360f) > 0.05f
            ? new[] { wrapHint, 360f }
            : new[] { 360f };
        if (piecePose.customAngle > 0f && Mathf.Abs(piecePose.customAngle - wrapHint) > 0.05f)
        {
            var list = new List<float>(wraps) { piecePose.customAngle };
            wraps = list.ToArray();
        }

        int sideCount = sideCandidates != null && sideCandidates.Count > 0 ? sideCandidates.Count : 1;
        for (int s = 0; s < sideCount; s++)
        {
            var side = sideCandidates != null && sideCandidates.Count > 0 ? sideCandidates[s] : piecePose;
            float sideWrap = ResolveWrapDeg(side, center, axis, startPostAnchor, hasStartPost);
            for (int w = 0; w < wraps.Length; w++)
            {
                float wrap = wraps[w] > 0f ? wraps[w] : sideWrap;
                for (int c = 0; c < counts.Length; c++)
                {
                    int n = counts[c];
                    if (wrap < 359f && n < 2)
                        continue;
                    float radius = NaturalRadius(pieceSize, n, wrap, joinOffset);
                    var cfg = TryConfig(
                        n, radius, startHint, wrap, joinKind, s, piecePose, pieceSize, joinOffset,
                        center, axis, hasStartPost, startPostAnchor, startPostFacing);
                    if (cfg != null)
                        results.Add(cfg);
                }
            }
        }

        if (hasStartPost)
        {
            for (int i = results.Count - 1; i >= 0; i--)
            {
                if (!results[i].matchesStartPostAnchor)
                    results.RemoveAt(i);
            }
        }

        results.Sort((a, b) => b.score.CompareTo(a.score));
        return results;
    }

    static RadialSolvedConfig TryConfig(
        int count,
        float radius,
        float startAngleDeg,
        float wrapAngleDeg,
        RadialJoinKind joinKind,
        int sidePoseIndex,
        CustomRadialSidePose piecePose,
        Vector3 pieceSize,
        float joinOffset,
        Vector3 center,
        Vector3 axis,
        bool hasStartPost,
        Vector3 startPostAnchor,
        Vector3 startPostFacing)
    {
        Vector3 slot0 = PolarSlot(center, axis, radius, 0, count, startAngleDeg, wrapAngleDeg);
        Quaternion yaw0 = YawToCenter(slot0, center, axis);
        bool match = !hasStartPost || MatchesStartPost(slot0, yaw0, startPostAnchor, startPostFacing, axis);

        if (hasStartPost && !match)
            return null;

        float mateSlack = 0f;
        int n = Mathf.Max(1, count);
        for (int i = 0; i < n; i++)
        {
            NeighborJoinPoints(
                center, axis, radius, i, n, startAngleDeg, wrapAngleDeg, pieceSize, joinOffset,
                out Vector3 towardPrev, out Vector3 towardNext);
            Vector3 slot = PolarSlot(center, axis, radius, i, n, startAngleDeg, wrapAngleDeg);
            Vector3 next = PolarSlot(center, axis, radius, i + 1, n, startAngleDeg, wrapAngleDeg);
            float gap = Vector3.Distance(towardNext, PolarSlot(center, axis, radius, i + 1, n, startAngleDeg, wrapAngleDeg)
                + (slot - next).normalized * (Mathf.Max(pieceSize.x, pieceSize.z) * 0.5f + joinOffset * 0.5f));
            mateSlack += gap;
            Bounds flyI = BoundsAt(piecePose.flyAway, slot, YawToCenter(slot, center, axis));
            Bounds flyN = BoundsAt(piecePose.flyAway, next, YawToCenter(next, center, axis));
            if (joinKind != RadialJoinKind.Offset && n > 1 && FlyAwayOverlaps(flyI, flyN)
                && Vector3.Distance(slot, next) > 1e-4f)
                return null;
        }

        float score = 10f - mateSlack - (hasStartPost && !match ? 5f : 0f);
        return new RadialSolvedConfig
        {
            count = count,
            radius = radius,
            startAngleDeg = startAngleDeg,
            wrapAngleDeg = wrapAngleDeg,
            joinKind = joinKind,
            sidePoseIndex = sidePoseIndex,
            score = score,
            matchesStartPostAnchor = match,
            label = ""
        };
    }

    static Bounds BoundsAt(Bounds local, Vector3 worldCenter, Quaternion rot)
    {
        Vector3 c = worldCenter + rot * local.center;
        return new Bounds(c, local.size);
    }
}
