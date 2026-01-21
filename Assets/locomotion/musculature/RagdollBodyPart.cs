using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Musculature
{
    /// <summary>
    /// Common metadata + discovery helpers for anatomical ragdoll parts.
    /// Intended to stay lightweight and avoid caching (for now).
    /// </summary>
    public abstract class RagdollBodyPart : MonoBehaviour
    {
        [Header("Bone Transforms")]
        [Tooltip("Primary transform representing this part's bone/root. If null, defaults to this Transform.")]
        public Transform boneTransform;

        [Tooltip("Optional additional bone transforms associated with this part (e.g., multiple spines, wrist bones, toe bones).")]
        public List<Transform> additionalBoneTransforms = new List<Transform>();

        [Header("Explicit Links (optional)")]
        [Tooltip("If set, these muscle groups are considered associated with this part (in addition to discovered groups).")]
        public List<MuscleGroup> linkedMuscleGroups = new List<MuscleGroup>();

        [Tooltip("If set, these behavior trees are considered associated with this part (in addition to discovered trees).")]
        public List<BehaviorTree> linkedBehaviorTrees = new List<BehaviorTree>();

        protected virtual void Reset()
        {
            boneTransform = transform;
        }

        protected virtual void OnValidate()
        {
            if (boneTransform == null)
                boneTransform = transform;
        }

        public Transform PrimaryBoneTransform => boneTransform != null ? boneTransform : transform;

        public IEnumerable<Transform> EnumerateBoneTransforms()
        {
            yield return PrimaryBoneTransform;
            if (additionalBoneTransforms == null) yield break;
            for (int i = 0; i < additionalBoneTransforms.Count; i++)
            {
                var t = additionalBoneTransforms[i];
                if (t != null) yield return t;
            }
        }

        /// <summary>
        /// Find the nearest RagdollSystem in parents, if any.
        /// </summary>
        public RagdollSystem FindRagdollSystem()
        {
            return GetComponentInParent<RagdollSystem>();
        }

        private Transform GetSearchRoot()
        {
            var rs = FindRagdollSystem();
            if (rs != null && rs.ragdollRoot != null) return rs.ragdollRoot;
            if (rs != null) return rs.transform;
            return transform.root;
        }

        private bool IsAnyBoneWithin(Transform candidate)
        {
            if (candidate == null) return false;
            foreach (var b in EnumerateBoneTransforms())
            {
                if (b == null) continue;
                if (candidate == b) return true;
                if (candidate.IsChildOf(b)) return true;
            }
            return false;
        }

        /// <summary>
        /// Queries muscle groups associated with this part by:
        /// - explicit links, and
        /// - any MuscleGroup that contains Muscles under this part's bone transform(s).
        /// </summary>
        public List<MuscleGroup> QueryMuscleGroups(bool includeInactive = true)
        {
            var result = new List<MuscleGroup>(linkedMuscleGroups != null ? linkedMuscleGroups.Count : 0);
            var seen = new HashSet<MuscleGroup>();

            if (linkedMuscleGroups != null)
            {
                for (int i = 0; i < linkedMuscleGroups.Count; i++)
                {
                    var mg = linkedMuscleGroups[i];
                    if (mg == null) continue;
                    if (seen.Add(mg)) result.Add(mg);
                }
            }

            Transform root = GetSearchRoot();
            if (root == null) return result;

            var groups = root.GetComponentsInChildren<MuscleGroup>(includeInactive);
            for (int i = 0; i < groups.Length; i++)
            {
                var g = groups[i];
                if (g == null || !seen.Add(g)) continue;

                // Fast path: group transform under bone
                if (IsAnyBoneWithin(g.transform))
                {
                    result.Add(g);
                    continue;
                }

                // Robust path: any Muscle under bone
                var muscles = g.muscles;
                bool matched = false;
                if (muscles != null && muscles.Count > 0)
                {
                    for (int m = 0; m < muscles.Count; m++)
                    {
                        var mu = muscles[m];
                        if (mu == null) continue;
                        if (IsAnyBoneWithin(mu.transform) || (mu.attachedJoint != null && IsAnyBoneWithin(mu.attachedJoint.transform)))
                        {
                            matched = true;
                            break;
                        }
                    }
                }
                else
                {
                    // Fallback if list isn't populated yet.
                    var muChildren = g.GetComponentsInChildren<Muscle>(includeInactive);
                    for (int m = 0; m < muChildren.Length; m++)
                    {
                        var mu = muChildren[m];
                        if (mu == null) continue;
                        if (IsAnyBoneWithin(mu.transform) || (mu.attachedJoint != null && IsAnyBoneWithin(mu.attachedJoint.transform)))
                        {
                            matched = true;
                            break;
                        }
                    }
                }

                if (matched)
                    result.Add(g);
            }

            return result;
        }

        /// <summary>
        /// Queries behavior trees associated with this part by:
        /// - explicit links, and
        /// - any BehaviorTree whose available cards include actions targeting this part's muscle groups.
        /// </summary>
        public List<BehaviorTree> QueryBehaviorTrees(bool includeInactive = true)
        {
            var result = new List<BehaviorTree>(linkedBehaviorTrees != null ? linkedBehaviorTrees.Count : 0);
            var seen = new HashSet<BehaviorTree>();

            if (linkedBehaviorTrees != null)
            {
                for (int i = 0; i < linkedBehaviorTrees.Count; i++)
                {
                    var bt = linkedBehaviorTrees[i];
                    if (bt == null) continue;
                    if (seen.Add(bt)) result.Add(bt);
                }
            }

            Transform root = GetSearchRoot();
            if (root == null) return result;

            var allTrees = root.GetComponentsInChildren<BehaviorTree>(includeInactive);
            if (allTrees == null || allTrees.Length == 0) return result;

            var muscleGroups = QueryMuscleGroups(includeInactive);
            var groupNames = new HashSet<string>();
            for (int i = 0; i < muscleGroups.Count; i++)
            {
                var g = muscleGroups[i];
                if (g == null) continue;
                if (!string.IsNullOrEmpty(g.groupName))
                    groupNames.Add(g.groupName);
            }

            for (int i = 0; i < allTrees.Length; i++)
            {
                var bt = allTrees[i];
                if (bt == null || !seen.Add(bt)) continue;

                if (groupNames.Count == 0)
                    continue;

                var cards = bt.availableCards;
                if (cards == null) continue;

                bool matched = false;
                for (int c = 0; c < cards.Count; c++)
                {
                    var gs = cards[c];
                    if (gs == null) continue;
                    var stack = gs.impulseStack;
                    if (stack == null) continue;
                    for (int a = 0; a < stack.Count; a++)
                    {
                        var act = stack[a];
                        if (act == null) continue;
                        if (!string.IsNullOrEmpty(act.muscleGroup) && groupNames.Contains(act.muscleGroup))
                        {
                            matched = true;
                            break;
                        }
                    }
                    if (matched) break;
                }

                if (matched)
                    result.Add(bt);
            }

            return result;
        }

        /// <summary>
        /// Queries behavior trees associated with this part, including any behavior trees explicitly attached to
        /// cards (GoodSections) that target this part's muscle groups.
        /// </summary>
        public List<BehaviorTree> QueryAssociatedBehaviorTrees(bool includeInactive = true)
        {
            var result = QueryBehaviorTrees(includeInactive);
            var seen = new HashSet<BehaviorTree>(result);

            var cards = QueryCards(includeInactive);
            for (int i = 0; i < cards.Count; i++)
            {
                var gs = cards[i];
                if (gs?.behaviorTree == null) continue;
                if (seen.Add(gs.behaviorTree))
                    result.Add(gs.behaviorTree);
            }

            return result;
        }

        /// <summary>
        /// Queries cards (GoodSections) associated with this part by inspecting available cards on behavior trees
        /// and selecting cards with actions targeting this part's muscle groups.
        /// </summary>
        public List<GoodSection> QueryCards(bool includeInactive = true)
        {
            var result = new List<GoodSection>();
            var seen = new HashSet<GoodSection>();

            var muscleGroups = QueryMuscleGroups(includeInactive);
            var groupNames = new HashSet<string>();
            for (int i = 0; i < muscleGroups.Count; i++)
            {
                var g = muscleGroups[i];
                if (g == null) continue;
                if (!string.IsNullOrEmpty(g.groupName))
                    groupNames.Add(g.groupName);
            }

            if (groupNames.Count == 0)
                return result;

            Transform root = GetSearchRoot();
            if (root == null) return result;

            var trees = root.GetComponentsInChildren<BehaviorTree>(includeInactive);
            for (int i = 0; i < trees.Length; i++)
            {
                var bt = trees[i];
                if (bt == null) continue;

                var cards = bt.availableCards;
                if (cards == null) continue;

                for (int c = 0; c < cards.Count; c++)
                {
                    var gs = cards[c];
                    if (gs == null || !seen.Add(gs)) continue;

                    var stack = gs.impulseStack;
                    if (stack == null) continue;

                    bool matched = false;
                    for (int a = 0; a < stack.Count; a++)
                    {
                        var act = stack[a];
                        if (act == null) continue;
                        if (!string.IsNullOrEmpty(act.muscleGroup) && groupNames.Contains(act.muscleGroup))
                        {
                            matched = true;
                            break;
                        }
                    }

                    if (matched)
                        result.Add(gs);
                }
            }

            return result;
        }
    }

    public abstract class RagdollSidedBodyPart : RagdollBodyPart
    {
        public BodySide side;
    }
}

