using Locomotion.Rig;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// Thick polyline / markers for TravelAgent cached plan in Scene view.
public static class TravelAgentSceneHandles
{
    public static void DrawCachedPlan(TravelAgent agent)
    {
        if (agent == null || agent.CachedPlan == null || agent.CachedPlan.IsEmpty)
            return;

        if (agent.drawMultibodyBasePlan && agent.CachedPlanBeforeMultibody != null && !agent.CachedPlanBeforeMultibody.IsEmpty)
        {
            Handles.color = new Color(1f, 0.15f, 1f, 0.75f);
            foreach (MultiModalSegment seg in agent.CachedPlanBeforeMultibody.segments)
            {
                if (seg?.waypoints == null || seg.waypoints.Count < 2)
                    continue;
                var pts = seg.waypoints;
                for (int i = 1; i < pts.Count; i++)
                    Handles.DrawDottedLine(pts[i - 1], pts[i], 4f);
            }
        }

        GenericMultiModalPathPlan plan = agent.CachedPlan;
        Handles.color = new Color(0.2f, 0.85f, 1f, 0.95f);

        foreach (MultiModalSegment seg in plan.segments)
        {
            if (seg?.waypoints == null || seg.waypoints.Count < 2)
                continue;
            var pts = seg.waypoints;
            for (int i = 1; i < pts.Count; i++)
                Handles.DrawAAPolyLine(6f, pts[i - 1], pts[i]);
        }

        Handles.color = Color.yellow;
        for (int i = 1; i < plan.segments.Count; i++)
        {
            MultiModalSegment prev = plan.segments[i - 1];
            MultiModalSegment cur = plan.segments[i];
            if (prev == null || cur == null || cur.waypoints == null || cur.waypoints.Count == 0)
                continue;
            if (prev.mode != cur.mode)
                Handles.SphereHandleCap(0, cur.waypoints[0], Quaternion.identity, 0.35f, EventType.Repaint);
        }

        if (agent.showVelocityTrack || agent.showReverseBudget)
            DrawKinematicsOverlay(agent);

        if (agent.showReverseBudget && TravelPathReverseLimits.AllowsReverse(agent.reverseLegLimit01))
            DrawReverseBudgetBar(agent);

        if (agent.multibody != null)
        {
            if (agent.multibody.finalTarget != null)
            {
                Handles.color = new Color(0.35f, 1f, 0.45f, 0.95f);
                Handles.SphereHandleCap(0, agent.multibody.finalTarget.position, Quaternion.identity, 0.4f, EventType.Repaint);
            }
            else if (agent.multibody.finalTargetWorld.sqrMagnitude > 1e-4f)
            {
                Handles.color = new Color(0.35f, 1f, 0.45f, 0.95f);
                Handles.SphereHandleCap(0, agent.multibody.finalTargetWorld, Quaternion.identity, 0.4f, EventType.Repaint);
            }
        }

        var justice = agent as JusticeRehabilitationTravelAgent;
        if (justice != null)
            DrawJusticeStepPreview(justice);
        var education = agent as EducationalTravelAgent;
        if (education != null)
            DrawEducationStepPreview(education);
    }

    static void DrawJusticeStepPreview(JusticeRehabilitationTravelAgent justice)
    {
        Vector3 predicted = justice.PredictedPlacement();
        Vector3 inpaint = justice.InpaintPlacement();
        bool over = justice.SelectedOverLimit();
        if (over)
        {
            Handles.color = new Color(1f, 0.15f, 0.1f, 0.85f);
            Handles.SphereHandleCap(0, predicted, Quaternion.identity, 0.55f, EventType.Repaint);
        }
        Handles.color = new Color(0.25f, 0.45f, 1f, 0.95f);
        Handles.SphereHandleCap(0, inpaint, Quaternion.identity, 0.35f, EventType.Repaint);
        Handles.color = Color.white;
        Handles.DrawDottedLine(predicted, inpaint.sqrMagnitude > 1e-6f ? inpaint : predicted + Vector3.forward, 4f);
    }

    static void DrawEducationStepPreview(EducationalTravelAgent education)
    {
        Vector3 predicted = education.PredictedPlacement();
        Vector3 inpaint = education.InpaintPlacement();
        Handles.color = new Color(0.25f, 0.45f, 1f, 0.95f);
        Handles.SphereHandleCap(0, inpaint, Quaternion.identity, 0.35f, EventType.Repaint);
        Handles.color = Color.white;
        Handles.SphereHandleCap(0, predicted, Quaternion.identity, 0.28f, EventType.Repaint);
        Handles.DrawDottedLine(predicted, inpaint.sqrMagnitude > 1e-6f ? inpaint : predicted + Vector3.forward, 4f);
    }

    static void DrawKinematicsOverlay(TravelAgent agent)
    {
        TravelPathKinematicsProfile profile = TravelPathKinematicsProfile.Build(
            agent.CachedPlan,
            agent.reverseLegLimit01,
            agent.velocityTrackSpacingMeters);

        foreach (TravelPathSample sample in profile.Samples)
        {
            if (sample.reverse && !agent.showReverseBudget)
                continue;
            if (!sample.reverse && !agent.showVelocityTrack)
                continue;

            Color c = ModeColor(sample.mode, sample.speed);
            if (sample.reverse)
                c = new Color(0.45f, 0.55f, 1f, 0.9f);

            Handles.color = c;
            Vector3 dir = sample.reverse ? -sample.tangent : sample.tangent;
            if (dir.sqrMagnitude < 1e-6f)
                continue;

            Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);
            float size = sample.reverse ? 0.35f : 0.45f;
            Handles.ArrowHandleCap(0, sample.position, rot, size, EventType.Repaint);

            if (sample.reverse)
                Handles.DrawDottedLine(sample.position, sample.position + dir * 0.5f, 3f);
        }
    }

    static Color ModeColor(TravelLegMode mode, float speed)
    {
        float t = Mathf.Clamp01(speed / 10f);
        return mode switch
        {
            TravelLegMode.Drive => Color.Lerp(new Color(1f, 0.7f, 0.2f), new Color(1f, 0.3f, 0.1f), t),
            TravelLegMode.Fly => Color.Lerp(new Color(0.5f, 0.9f, 1f), new Color(0.2f, 0.5f, 1f), t),
            _ => Color.Lerp(new Color(0.3f, 1f, 0.4f), new Color(0.1f, 0.7f, 0.2f), t)
        };
    }

    static void DrawReverseBudgetBar(TravelAgent agent)
    {
        if (agent.CachedPlan == null || agent.CachedPlan.IsEmpty)
            return;

        List<Vector3> pts = agent.CachedPlan.FlattenWaypointsForGizmos();
        if (pts == null || pts.Count == 0)
            return;

        Vector3 anchor = pts[pts.Count - 1] + Vector3.up * 0.5f;
        float barWidth = 2f;
        Handles.color = new Color(0.2f, 0.2f, 0.2f, 0.6f);
        Handles.DrawLine(anchor - Vector3.right * barWidth * 0.5f, anchor + Vector3.right * barWidth * 0.5f);
        Handles.color = new Color(0.4f, 0.55f, 1f, 0.95f);
        float fill = barWidth * agent.reverseLegLimit01;
        Handles.DrawLine(anchor - Vector3.right * barWidth * 0.5f, anchor - Vector3.right * barWidth * 0.5f + Vector3.right * fill);

        Handles.Label(anchor + Vector3.up * 0.25f,
            TravelPathReverseLimits.FormatDistanceLabel(agent.ReverseBudgetMeters, agent.TotalPathLengthMeters));
    }
}


namespace Locomotion.EditorTools
{
    public sealed class SkeletonFitPair
    {
        public string sourceId;
        public string targetTraitId;
        public float confidence;
        public bool inferred;
    }

    public sealed class SkeletonFitResult
    {
        public readonly System.Collections.Generic.List<SkeletonFitPair> pairs = new System.Collections.Generic.List<SkeletonFitPair>();
        public readonly System.Collections.Generic.List<string> unmatchedSource = new System.Collections.Generic.List<string>();
        public readonly System.Collections.Generic.List<string> unmatchedTarget = new System.Collections.Generic.List<string>();
        public readonly System.Collections.Generic.List<string> offeredAnimalRows = new System.Collections.Generic.List<string>();

        public System.Collections.Generic.Dictionary<string, string> ToRemap()
        {
            var map = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
            for (int i = 0; i < pairs.Count; i++)
            {
                var p = pairs[i];
                if (p == null || string.IsNullOrEmpty(p.sourceId) || string.IsNullOrEmpty(p.targetTraitId))
                    continue;
                map[p.sourceId] = p.targetTraitId;
            }
            return map;
        }
    }
}


public static class PoseTrackPlayer
{
    public static int Apply(PoseTrack track, BoneMap map, float timeMs) => 0;
}

public static class PoseTrackClipBaker
{
    public static int BakeAndAddSet(RagdollIKAnimationManager ik, PoseTrack track, BoneMap map, UnityEngine.Transform root, string name) => -1;
}

public static class BvhPoseTrackImporter
{
    public sealed class Joint { public string name; public int parent = -1; }
    public static void CollectJoints(string bvh, System.Collections.Generic.List<Joint> joints) { }
    public static PoseTrack FromFile(string path, string modelSpec) => new PoseTrack { modelSpec = modelSpec };
}

public static class ContinuuuumRemotePoseAnimationDetector
{
    public static PoseTrack TryLoadJson(string path)
    {
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;
        return PoseTrack.FromJson(System.IO.File.ReadAllText(path));
    }
}


namespace Locomotion.EditorTools
{
    public static class ArbitrarySkeletonFitter
    {
        public static SkeletonFitResult FitToBoneMap(
            System.Collections.Generic.IList<string> sourceIds,
            System.Collections.Generic.IList<int> sourceParents,
            BoneMap map,
            string unmatchedPrefix = "Animal")
        {
            var result = new SkeletonFitResult();
            if (sourceIds == null) return result;
            for (int i = 0; i < sourceIds.Count; i++)
            {
                string id = sourceIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                result.pairs.Add(new SkeletonFitPair { sourceId = id, targetTraitId = id, confidence = 0.5f });
            }
            _ = sourceParents;
            _ = map;
            _ = unmatchedPrefix;
            return result;
        }
    }
}

public static class WebcamAnimTimelineFields
{
    public static float DrawPlayheadMs(string label, float value, float maxMs)
    {
        return UnityEditor.EditorGUILayout.Slider(label, value, 0f, UnityEngine.Mathf.Max(0.001f, maxMs));
    }

    public static float PlayheadMaxMs(WebcamAnimRecordingAsset recording, PoseTrack track)
    {
        if (track != null && track.Count > 0)
            return UnityEngine.Mathf.Max(1f, track.LatestTimeMs());
        _ = recording;
        return 1000f;
    }
}
