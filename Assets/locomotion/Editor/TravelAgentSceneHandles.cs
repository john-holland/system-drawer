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


public static class PoseTrackClipBaker
{
    public static int BakeAndAddSet(
        RagdollIKAnimationManager ik,
        PoseTrack track,
        BoneMap map,
        UnityEngine.Transform root,
        string name,
        bool syncSelection = true)
    {
        _ = ik;
        _ = track;
        _ = map;
        _ = root;
        _ = name;
        _ = syncSelection;
        return -1;
    }
}


namespace Locomotion.EditorTools
{
    public static class ArbitrarySkeletonFitter
    {
        public static SkeletonFitResult Fit(
            System.Collections.Generic.IList<string> sourceIds,
            System.Collections.Generic.IList<int> sourceParents,
            System.Collections.Generic.IList<string> targetTraitIds,
            string unmatchedPrefix = "Animal")
        {
            var result = new SkeletonFitResult();
            if (sourceIds == null) return result;
            var used = new System.Collections.Generic.HashSet<string>(System.StringComparer.Ordinal);
            for (int i = 0; i < sourceIds.Count; i++)
            {
                string id = sourceIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                string hit = null;
                if (targetTraitIds != null)
                {
                    for (int t = 0; t < targetTraitIds.Count; t++)
                    {
                        string cand = targetTraitIds[t];
                        if (string.IsNullOrEmpty(cand) || used.Contains(cand)) continue;
                        if (BonesMatch(id, cand))
                        {
                            hit = cand;
                            break;
                        }
                    }
                }
                if (hit != null)
                {
                    used.Add(hit);
                    result.pairs.Add(new SkeletonFitPair { sourceId = id, targetTraitId = hit, confidence = 1f });
                }
                else
                {
                    result.unmatchedSource.Add(id);
                    string offered = string.IsNullOrEmpty(unmatchedPrefix) ? id : unmatchedPrefix + ":" + id;
                    result.offeredAnimalRows.Add(offered);
                    result.pairs.Add(new SkeletonFitPair { sourceId = id, targetTraitId = offered, confidence = 0.2f, inferred = true });
                }
            }
            _ = sourceParents;
            return result;
        }

        public static SkeletonFitResult FitToBoneMap(
            System.Collections.Generic.IList<string> sourceIds,
            System.Collections.Generic.IList<int> sourceParents,
            BoneMap map,
            string unmatchedPrefix = "Animal")
        {
            var targets = new System.Collections.Generic.List<string>();
            if (map != null && map.entries != null)
            {
                for (int i = 0; i < map.entries.Count; i++)
                {
                    if (map.entries[i] != null && !string.IsNullOrEmpty(map.entries[i].traitId))
                        targets.Add(map.entries[i].traitId);
                }
            }
            return Fit(sourceIds, sourceParents, targets, unmatchedPrefix);
        }

        public static string InferLaterality(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string n = name.ToLowerInvariant();
            if (n.Contains("left")) return "Left";
            if (n.Contains("right")) return "Right";
            bool limb = n.Contains("thigh") || n.Contains("arm") || n.Contains("leg") || n.Contains("hand")
                || n.Contains("foot") || n.Contains("wing") || n.Contains("shin");
            if (limb && n.StartsWith("l")) return "Left";
            if (limb && n.StartsWith("r") && !n.StartsWith("root") && !n.StartsWith("rib")) return "Right";
            return "";
        }

        static bool BonesMatch(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return false;
            if (string.Equals(source, target, System.StringComparison.OrdinalIgnoreCase)) return true;
            if (target.EndsWith(":" + source, System.StringComparison.OrdinalIgnoreCase)) return true;
            string latS = InferLaterality(source);
            string latT = InferLaterality(target);
            if (latS.Length > 0 && latT.Length > 0 && !string.Equals(latS, latT, System.StringComparison.OrdinalIgnoreCase))
                return false;
            string s = source.ToLowerInvariant();
            string t = target.ToLowerInvariant();
            int colon = t.LastIndexOf(':');
            if (colon >= 0) t = t.Substring(colon + 1);
            if (s == t) return true;
            bool sThigh = s.Contains("thigh") || s.Contains("upperleg");
            bool tThigh = t.Contains("thigh") || t.Contains("upperleg");
            return sThigh && tThigh;
        }

        public static void ApplyOfferedRows(BoneMap map, SkeletonFitResult fit)
        {
            if (map == null || fit == null) return;
            for (int i = 0; i < fit.offeredAnimalRows.Count; i++)
            {
                string id = fit.offeredAnimalRows[i];
                if (string.IsNullOrEmpty(id) || map.TryGet(id, out _)) continue;
                Transform parent = map.transform;
                Transform slot = parent != null ? parent.Find(id) : null;
                if (slot == null && parent != null)
                {
                    var go = new UnityEngine.GameObject(id);
                    go.transform.SetParent(parent, false);
                    slot = go.transform;
                }
                if (slot != null)
                    map.Set(id, slot);
            }
        }
    }
}
