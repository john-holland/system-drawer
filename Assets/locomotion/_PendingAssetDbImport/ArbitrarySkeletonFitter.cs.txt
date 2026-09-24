#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using Locomotion.Rig;
using UnityEngine;

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
        public readonly List<SkeletonFitPair> pairs = new List<SkeletonFitPair>();
        public readonly List<string> unmatchedSource = new List<string>();
        public readonly List<string> unmatchedTarget = new List<string>();
        public readonly List<string> offeredAnimalRows = new List<string>();

        public Dictionary<string, string> ToRemap()
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
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

    /// <summary>Name / synonym / hierarchy fit from an arbitrary skeleton onto a BoneMap.</summary>
    public static class ArbitrarySkeletonFitter
    {
        static readonly Dictionary<string, string[]> Synonyms = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "hips", new[] { "hips", "hip", "pelvis", "root", "centre", "center" } },
            { "spine", new[] { "spine", "spine1", "spine01", "back" } },
            { "chest", new[] { "chest", "spine2", "spine02", "thorax", "upperchest" } },
            { "neck", new[] { "neck", "neck1" } },
            { "head", new[] { "head", "skull", "cranium" } },
            { "upperleg", new[] { "upperleg", "thigh", "upleg", "hip" } },
            { "lowerleg", new[] { "lowerleg", "shin", "calf", "leg", "knee" } },
            { "foot", new[] { "foot", "ankle" } },
            { "shoulder", new[] { "shoulder", "clavicle", "collar" } },
            { "upperarm", new[] { "upperarm", "arm", "shoulder" } },
            { "lowerarm", new[] { "lowerarm", "forearm", "elbow" } },
            { "hand", new[] { "hand", "wrist" } },
            { "tail", new[] { "tail", "tail1", "tail01" } },
            { "wing", new[] { "wing", "wing1" } },
            { "ear", new[] { "ear" } },
        };

        public static string NormalizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";
            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }

        public static string SanitizeTraitSuffix(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "Bone";
            var sb = new StringBuilder(raw.Length);
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
                else if (c == '_' || c == '-')
                    sb.Append('_');
            }
            return sb.Length > 0 ? sb.ToString() : "Bone";
        }

        public static string InferLaterality(string name)
        {
            string n = NormalizeName(name);
            if (n.StartsWith("l") && (n.Contains("left") || n.StartsWith("lft") || n.StartsWith("lf") ||
                n.Contains("lthigh") || n.Contains("larm") || n.Contains("lhand") || n.Contains("lleg") ||
                n.Contains("lfoot") || n.Contains("lshin") || n.Contains("lcalf") || n.StartsWith("left")))
                return "Left";
            if (n.Contains("left") || n.StartsWith("left"))
                return "Left";
            if (n.Contains("right") || n.StartsWith("right") || n.StartsWith("rgt") ||
                n.Contains("rthigh") || n.Contains("rarm") || n.Contains("rhand") || n.Contains("rleg") ||
                n.Contains("rfoot"))
                return "Right";
            if (n.StartsWith("r") && n.Length > 1 && !n.StartsWith("root") && !n.StartsWith("rib"))
            {
                if (n.Contains("arm") || n.Contains("leg") || n.Contains("hand") || n.Contains("foot") ||
                    n.Contains("thigh") || n.Contains("shin") || n.Contains("wing") || n.Contains("ear"))
                    return "Right";
            }
            if (n.StartsWith("l") && n.Length > 1 && !n.StartsWith("lower") && !n.StartsWith("leg"))
            {
                if (n.Contains("arm") || n.Contains("leg") || n.Contains("hand") || n.Contains("foot") ||
                    n.Contains("thigh") || n.Contains("shin") || n.Contains("wing") || n.Contains("ear"))
                    return "Left";
            }
            return "";
        }

        public static string InferLateralityFromRestX(float worldX, float epsilon = 0.02f)
        {
            if (worldX > epsilon)
                return "Left";
            if (worldX < -epsilon)
                return "Right";
            return "";
        }

        public static SkeletonFitResult Fit(
            IList<string> sourceIds,
            IList<int> sourceParents,
            IList<string> targetTraitIds,
            string unmatchedPrefix = "Animal")
        {
            var result = new SkeletonFitResult();
            if (sourceIds == null)
                return result;

            var targets = new List<string>();
            var targetUsed = new HashSet<string>(StringComparer.Ordinal);
            if (targetTraitIds != null)
            {
                for (int i = 0; i < targetTraitIds.Count; i++)
                {
                    if (!string.IsNullOrEmpty(targetTraitIds[i]))
                        targets.Add(targetTraitIds[i]);
                }
            }

            var matchedSource = new HashSet<int>();
            for (int s = 0; s < sourceIds.Count; s++)
            {
                string src = sourceIds[s];
                if (string.IsNullOrEmpty(src))
                    continue;
                var hit = BestTarget(src, targets, targetUsed);
                if (hit != null)
                {
                    result.pairs.Add(hit);
                    targetUsed.Add(hit.targetTraitId);
                    matchedSource.Add(s);
                }
            }

            InferHierarchy(sourceIds, sourceParents, targets, targetUsed, matchedSource, result);

            for (int s = 0; s < sourceIds.Count; s++)
            {
                if (matchedSource.Contains(s) || string.IsNullOrEmpty(sourceIds[s]))
                    continue;
                string offered = (unmatchedPrefix ?? "Animal") + ":" + SanitizeTraitSuffix(sourceIds[s]);
                result.unmatchedSource.Add(sourceIds[s]);
                result.offeredAnimalRows.Add(offered);
                result.pairs.Add(new SkeletonFitPair
                {
                    sourceId = sourceIds[s],
                    targetTraitId = offered,
                    confidence = 0.2f,
                    inferred = true
                });
            }

            for (int t = 0; t < targets.Count; t++)
            {
                if (!targetUsed.Contains(targets[t]))
                    result.unmatchedTarget.Add(targets[t]);
            }

            return result;
        }

        public static SkeletonFitResult FitToBoneMap(
            IList<string> sourceIds,
            IList<int> sourceParents,
            BoneMap map,
            string unmatchedPrefix = "Animal")
        {
            var targets = new List<string>();
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

        public static void ApplyOfferedRows(BoneMap map, SkeletonFitResult fit)
        {
            if (map == null || fit == null)
                return;
            for (int i = 0; i < fit.offeredAnimalRows.Count; i++)
            {
                string id = fit.offeredAnimalRows[i];
                if (string.IsNullOrEmpty(id))
                    continue;
                if (map.TryGet(id, out _))
                    continue;
                map.Set(id, FindOrCreateSlot(map.transform, id));
            }
        }

        static Transform FindOrCreateSlot(Transform parent, string traitId)
        {
            if (parent == null)
                return null;
            Transform existing = parent.Find(traitId);
            if (existing != null)
                return existing;
            var go = new GameObject(traitId);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        static SkeletonFitPair BestTarget(string sourceId, List<string> targets, HashSet<string> used)
        {
            string norm = NormalizeName(sourceId);
            string side = InferLaterality(sourceId);
            SkeletonFitPair best = null;
            float bestScore = 0f;

            for (int t = 0; t < targets.Count; t++)
            {
                string target = targets[t];
                if (used.Contains(target))
                    continue;
                float score = Score(sourceId, norm, side, target);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = new SkeletonFitPair
                    {
                        sourceId = sourceId,
                        targetTraitId = target,
                        confidence = score,
                        inferred = score < 0.85f
                    };
                }
            }

            return bestScore >= 0.45f ? best : null;
        }

        static float Score(string sourceId, string norm, string side, string target)
        {
            string leaf = target;
            int colon = target.LastIndexOf(':');
            if (colon >= 0 && colon < target.Length - 1)
                leaf = target.Substring(colon + 1);
            string tNorm = NormalizeName(leaf);
            if (tNorm.Length == 0)
                return 0f;
            if (string.Equals(sourceId, target, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(norm, tNorm, StringComparison.Ordinal))
                return 1f;
            if (norm.Contains(tNorm) || tNorm.Contains(norm))
            {
                float s = 0.8f;
                return SideBonus(side, target, s);
            }

            foreach (var kv in Synonyms)
            {
                if (!ContainsAny(norm, kv.Value) && !ContainsAny(tNorm, kv.Value) &&
                    !string.Equals(tNorm, kv.Key, StringComparison.Ordinal))
                    continue;
                if (ContainsAny(norm, kv.Value) && (tNorm.Contains(kv.Key) || ContainsAny(tNorm, kv.Value)))
                    return SideBonus(side, target, 0.72f);
            }

            return 0f;
        }

        static float SideBonus(string side, string target, float baseScore)
        {
            if (string.IsNullOrEmpty(side))
                return baseScore;
            if (target.IndexOf(side, StringComparison.OrdinalIgnoreCase) >= 0)
                return Mathf.Min(1f, baseScore + 0.15f);
            if (target.IndexOf(side == "Left" ? "Right" : "Left", StringComparison.OrdinalIgnoreCase) >= 0)
                return baseScore * 0.35f;
            return baseScore;
        }

        static bool ContainsAny(string hay, string[] needles)
        {
            for (int i = 0; i < needles.Length; i++)
            {
                if (hay.Contains(needles[i]))
                    return true;
            }
            return false;
        }

        static void InferHierarchy(
            IList<string> sourceIds,
            IList<int> sourceParents,
            List<string> targets,
            HashSet<string> used,
            HashSet<int> matchedSource,
            SkeletonFitResult result)
        {
            if (sourceParents == null || sourceIds == null)
                return;
            int hips = IndexOfNormalized(sourceIds, "hips", "pelvis", "root");
            if (hips < 0)
                return;

            var children = ChildrenOf(sourceParents, hips);
            if (children.Count == 0)
                return;

            int longest = -1;
            int longestLen = 0;
            for (int c = 0; c < children.Count; c++)
            {
                int len = ChainLength(sourceParents, children[c]);
                if (len > longestLen)
                {
                    longestLen = len;
                    longest = children[c];
                }
            }

            if (longest >= 0 && !matchedSource.Contains(longest))
                TryAssign(sourceIds[longest], "Human:Spine", "Generic:Spine", "Animal:Spine", targets, used, matchedSource, longest, result);

            var limbs = new List<int>();
            for (int c = 0; c < children.Count; c++)
            {
                if (children[c] != longest)
                    limbs.Add(children[c]);
            }
            if (limbs.Count >= 2)
            {
                TryAssign(sourceIds[limbs[0]], "Human:LeftUpperLeg", "Generic:LeftUpperLeg", "Animal:LeftUpperLeg", targets, used, matchedSource, limbs[0], result);
                TryAssign(sourceIds[limbs[1]], "Human:RightUpperLeg", "Generic:RightUpperLeg", "Animal:RightUpperLeg", targets, used, matchedSource, limbs[1], result);
            }
        }

        static void TryAssign(
            string sourceId,
            string human,
            string generic,
            string animal,
            List<string> targets,
            HashSet<string> used,
            HashSet<int> matchedSource,
            int sourceIndex,
            SkeletonFitResult result)
        {
            if (matchedSource.Contains(sourceIndex))
                return;
            string pick = null;
            if (targets.Contains(human) && !used.Contains(human))
                pick = human;
            else if (targets.Contains(generic) && !used.Contains(generic))
                pick = generic;
            else if (targets.Contains(animal) && !used.Contains(animal))
                pick = animal;
            if (pick == null)
                return;
            result.pairs.Add(new SkeletonFitPair
            {
                sourceId = sourceId,
                targetTraitId = pick,
                confidence = 0.5f,
                inferred = true
            });
            used.Add(pick);
            matchedSource.Add(sourceIndex);
        }

        static int IndexOfNormalized(IList<string> ids, params string[] keys)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                string n = NormalizeName(ids[i]);
                for (int k = 0; k < keys.Length; k++)
                {
                    if (n.Contains(keys[k]))
                        return i;
                }
            }
            return -1;
        }

        static List<int> ChildrenOf(IList<int> parents, int parent)
        {
            var list = new List<int>();
            for (int i = 0; i < parents.Count; i++)
            {
                if (parents[i] == parent)
                    list.Add(i);
            }
            return list;
        }

        static int ChainLength(IList<int> parents, int start)
        {
            int len = 1;
            int current = start;
            for (int guard = 0; guard < parents.Count; guard++)
            {
                int child = -1;
                int count = 0;
                for (int i = 0; i < parents.Count; i++)
                {
                    if (parents[i] == current)
                    {
                        child = i;
                        count++;
                    }
                }
                if (count != 1)
                    break;
                current = child;
                len++;
            }
            return len;
        }
    }
}
#endif
