// the elbow is a powerful articulated joint similar to the knee
// the elbow connects the upper arm to the forearm
// the elbow can bend and straighten, allowing the forearm to rotate relative to the upper arm
// the elbow has a protrusion (olecranon) that prevents over-extension

using UnityEngine;

namespace Locomotion.Musculature
{
    public sealed class RagdollElbow : RagdollSidedBodyPart
    {
        [Header("Elbow Properties")]
        [Tooltip("Reference to the forearm component below this elbow")]
        public RagdollForearm forearm;

        [Tooltip("Reference to the hand component below this elbow")]
        public RagdollHand hand;

        [Header("Auto-Join Options")]
        [Tooltip("If true, auto-creates forearm GameObject if missing, placing it halfway between elbow and hand")]
        public bool autoCreateForearm = false;

        private void Awake()
        {
            if (autoCreateForearm && forearm == null)
            {
                AutoCreateForearm();
            }
        }

        /// <summary>
        /// Auto-create forearm GameObject halfway between elbow and hand.
        /// </summary>
        private void AutoCreateForearm()
        {
            // Find hand/wrist position
            Transform handTransform = null;

            // Find hand component
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

            if (handTransform == null)
            {
                Debug.LogWarning($"[RagdollElbow] Cannot auto-create forearm: missing hand reference for {side} arm");
                return;
            }

            // Create forearm GameObject halfway between elbow and hand
            Vector3 elbowPos = PrimaryBoneTransform.position;
            Vector3 handPos = handTransform.position;
            Vector3 forearmPos = (elbowPos + handPos) * 0.5f;

            // Determine parent - prefer elbow's parent
            Transform parentTransform = transform.parent;
            if (parentTransform == null && transform.root != null)
                parentTransform = transform.root;

            GameObject forearmObj = new GameObject($"{side}_Forearm_Auto");
            forearmObj.transform.position = forearmPos;
            forearmObj.transform.SetParent(parentTransform, worldPositionStays: true);

            // Add RagdollForearm component
            RagdollForearm forearmComponent = forearmObj.AddComponent<RagdollForearm>();
            forearmComponent.side = side;
            forearmComponent.elbow = this;
            if (hand != null)
            {
                forearmComponent.hand = hand;
            }
            forearm = forearmComponent;

            Debug.Log($"[RagdollElbow] Auto-created forearm GameObject at position {forearmPos} for {side} arm");
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
