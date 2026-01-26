// the forearm contains a set of two bones wrapped in sinew that rotate at one end to turn the hand
// the non-turning end of the forearm connects to the upper arm at the elblow, the bone furthest out (clostest to the pinky) is the protrusion of the elbow

using UnityEngine;

namespace Locomotion.Musculature
{
    public sealed class RagdollForearm : RagdollSidedBodyPart
    {
        [Header("Forearm Properties")]
        [Tooltip("Reference to the elbow component above this forearm")]
        public RagdollElbow elbow;

        [Tooltip("Reference to the hand component below this forearm")]
        public RagdollHand hand;

        [Header("Auto-Join Options")]
        [Tooltip("If true, auto-creates elbow GameObject if missing, placing it halfway between upper arm and hand")]
        public bool autoCreateElbow = false;

        private void Awake()
        {
            if (autoCreateElbow && elbow == null)
            {
                AutoCreateElbow();
            }
        }

        /// <summary>
        /// Auto-create elbow GameObject halfway between upper arm and hand.
        /// </summary>
        private void AutoCreateElbow()
        {
            // Find upper arm (shoulder) and hand positions
            Transform upperarmTransform = null;
            Transform handTransform = null;

            // Find upper arm (upperarm) - this is the shoulder connection point
            RagdollUpperarm upperarm = GetComponentInParent<RagdollUpperarm>();
            if (upperarm != null)
            {
                upperarmTransform = upperarm.PrimaryBoneTransform;
            }
            else
            {
                // Try to find upper arm by name
                Transform root = transform.root;
                upperarmTransform = FindBoneByName(root, new[] { "upperarm", "upper_arm", "arm" }, side);
            }

            // If still not found, try shoulder
            if (upperarmTransform == null)
            {
                // Try to find shoulder bone by name
                Transform root = transform.root;
                upperarmTransform = FindBoneByName(root, new[] { "shoulder" }, side);
            }

            // Find hand/wrist
            if (hand != null)
            {
                handTransform = hand.PrimaryBoneTransform;
            }
            else
            {
                // Try to find hand by name
                Transform root = transform.root;
                handTransform = FindBoneByName(root, new[] { "hand", "wrist" }, side);
            }

            if (upperarmTransform == null || handTransform == null)
            {
                Debug.LogWarning($"[RagdollForearm] Cannot auto-create elbow: missing upper arm or hand reference. Upper Arm: {upperarmTransform != null}, Hand: {handTransform != null}");
                return;
            }

            // Create elbow GameObject halfway between upper arm and hand
            Vector3 upperarmPos = upperarmTransform.position;
            Vector3 handPos = handTransform.position;
            Vector3 elbowPos = (upperarmPos + handPos) * 0.5f;

            // Determine parent - prefer forearm's parent, or upper arm if available
            Transform parentTransform = transform.parent;
            if (upperarm != null && upperarm.transform != null)
            {
                parentTransform = upperarm.transform.parent ?? transform.parent;
            }

            GameObject elbowObj = new GameObject($"{side}_Elbow_Auto");
            elbowObj.transform.position = elbowPos;
            elbowObj.transform.SetParent(parentTransform, worldPositionStays: true);

            // Add RagdollElbow component
            RagdollElbow elbowComponent = elbowObj.AddComponent<RagdollElbow>();
            elbowComponent.side = side;
            elbowComponent.forearm = this;
            if (hand != null)
            {
                elbowComponent.hand = hand;
            }
            elbow = elbowComponent;

            Debug.Log($"[RagdollForearm] Auto-created elbow GameObject at position {elbowPos} for {side} arm");
        }

        /// <summary>
        /// Find a bone by name heuristics.
        /// </summary>
        private Transform FindBoneByName(Transform root, string[] nameTokens, BodySide? side)
        {
            if (root == null) return null;

            string sideName = side.HasValue ? side.Value.ToString().ToLowerInvariant() : null;
            string sideShort = side.HasValue ? (side.Value == BodySide.Left ? "l" : "r") : null;

            Transform best = null;
            int bestScore = int.MinValue;

            foreach (Transform child in root.GetComponentsInChildren<Transform>())
            {
                if (child == null) continue;
                string name = child.name.ToLowerInvariant();
                int score = 0;

                // Check name tokens
                bool nameMatch = false;
                foreach (var token in nameTokens)
                {
                    if (name.Contains(token.ToLowerInvariant()))
                    {
                        score += 10;
                        nameMatch = true;
                    }
                }
                if (!nameMatch) continue;

                // Check side
                if (side.HasValue)
                {
                    if (!string.IsNullOrEmpty(sideName) && name.Contains(sideName))
                        score += 5;
                    if (!string.IsNullOrEmpty(sideShort) && (name.StartsWith(sideShort + "_") || name.EndsWith("_" + sideShort)))
                        score += 5;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = child;
                }
            }

            return best;
        }
    }
}
