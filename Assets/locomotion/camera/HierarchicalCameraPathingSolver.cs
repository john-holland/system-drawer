using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Camera
{
    [Serializable]
    public struct CameraPlannerHints
    {
        public CameraFocusMode preferredMode;
        public float memorabilityScore;
        public float userRatingMean;
        public float lstmWeight;
        public float[] modeHintBias;

        public static CameraPlannerHints Default()
        {
            return new CameraPlannerHints
            {
                preferredMode = CameraFocusMode.Character,
                memorabilityScore = 0.5f,
                userRatingMean = 3f,
                lstmWeight = 1f,
                modeHintBias = new float[8],
            };
        }
    }

    /// <summary>Plans camera rig paths over landmark samples with LSTM/user hint biasing.</summary>
    public sealed class HierarchicalCameraPathingSolver : MonoBehaviour
    {
        [Header("Octree")]
        public Bounds worldBounds = new Bounds(Vector3.zero, Vector3.one * 200f);
        public int maxDepth = 4;
        public float minLeafExtent = 2f;

        [Header("Path")]
        public int landmarkSamples = 6;
        public int maxExpandedNodes = 8000;

        HierarchicalPathingOctTree _octTree;
        readonly Dictionary<CameraFocusMode, ICameraFocusStrategy> _strategies = new Dictionary<CameraFocusMode, ICameraFocusStrategy>();

        void Awake()
        {
            RegisterDefaults();
            RebuildIfNeeded(force: true);
        }

        public void RegisterDefaults()
        {
            _strategies.Clear();
            Register(new Strategies.ObjectFocusStrategy());
            Register(new Strategies.CharacterFocusStrategy());
            Register(new Strategies.FirstPersonFocusStrategy());
            Register(new Strategies.SceneCompositionFocusStrategy());
            Register(new Strategies.CentroidFocusStrategy());
            Register(new Strategies.ActorVisionTrainingFocusStrategy());
        }

        public void Register(ICameraFocusStrategy strategy)
        {
            if (strategy != null)
                _strategies[strategy.Mode] = strategy;
        }

        public void RebuildIfNeeded(bool force = false)
        {
            if (!force && _octTree != null) return;
            _octTree = HierarchicalPathingOctTree.Build(worldBounds, maxDepth, minLeafExtent, _ => false);
        }

        public IReadOnlyList<HierarchicalPathingOctTree.Leaf> Leaves => _octTree?.Leaves;

        public CameraRigPose ComputePose(CameraFocusMode mode, CameraPathingContext ctx)
        {
            if (ctx.pathingOctTree == null)
                ctx.pathingOctTree = _octTree;
            if (_strategies.TryGetValue(mode, out var s))
                return s.ComputePose(ctx);
            return CameraRigPose.FromCamera(ctx.camera, mode);
        }

        public List<CameraRigPose> PlanCameraPath(
            CameraRigPose start,
            CameraRigPose goal,
            in CameraPlannerHints hints,
            CameraPathingContext ctx)
        {
            RebuildIfNeeded();
            ctx.pathingOctTree = _octTree;

            var landmarks = BuildLandmarks(start.position, goal.position, landmarkSamples);
            var poses = new List<CameraRigPose> { start };

            CameraRigPose current = start;
            for (int i = 1; i < landmarks.Count; i++)
            {
                var segmentGoal = new CameraRigPose
                {
                    position = landmarks[i],
                    rotation = goal.rotation,
                    fieldOfView = goal.fieldOfView,
                    focusMode = hints.preferredMode,
                };

                float bestCost = float.PositiveInfinity;
                CameraRigPose best = segmentGoal;
                foreach (var mode in SteadyModes())
                {
                    ctx.memorabilityMl = hints.memorabilityScore;
                    var candidate = ComputePose(mode, ctx);
                    candidate.position = Vector3.Lerp(current.position, landmarks[i], 0.85f);
                    float cost = Vector3.Distance(current.position, candidate.position);
                    cost += CameraPathingHeuristic.ModeCostDelta(mode, in hints);
                    if (_strategies.TryGetValue(mode, out var strat))
                        cost -= strat.ScoreCandidate(candidate, ctx) * 0.5f;
                    if (cost < bestCost)
                    {
                        bestCost = cost;
                        best = candidate;
                        best.focusMode = mode;
                    }
                }

                poses.Add(best);
                current = best;
            }

            poses.Add(goal);
            return poses;
        }

        static IEnumerable<CameraFocusMode> SteadyModes()
        {
            yield return CameraFocusMode.ObjectFocus;
            yield return CameraFocusMode.Character;
            yield return CameraFocusMode.FirstPerson;
            yield return CameraFocusMode.SceneFocus;
            yield return CameraFocusMode.CentroidFocus;
            yield return CameraFocusMode.MlActorVisionTrainingFocus;
        }

        static List<Vector3> BuildLandmarks(Vector3 start, Vector3 goal, int samples)
        {
            var list = new List<Vector3> { start };
            int n = Mathf.Max(2, samples);
            for (int i = 1; i < n; i++)
            {
                float t = i / (float)n;
                list.Add(Vector3.Lerp(start, goal, t));
            }
            list.Add(goal);
            return list;
        }
    }
}
