// the knee is a powerful artiulated joint and is like a protected elbow
// the lower leg has two bones that turn to turn the foot, where the upper leg has a large crutch to catch the lower legs
//   and at the top, a large ball joint like the shoulder, with similar limits although a steeper eliptoid limit on most adults
// the knee can bend anywhere between zero, and 180 to 270
// the knee has a fluid packet between the patella (knee bone), that is pressed against to protect the lower leg bones from grinding
// against the upper leg bone
//  this fluid packet and the sinew allow the knee to sort of "float" the weight, like one of those floating corded tables
//
// the top leg front muscles (name?) pull the patella up
// the top leg back muscles (name?) pull the patella taught
// the bottom leg muscles are mostly connected to the bottom leg bones, but have some small anchors in the patella and knee region
// sinew passes through the leg under patella and fluid packet, while the pattella is pulled like a second person pushing your legs

using UnityEngine;

namespace Locomotion.Musculature
{
    public sealed class RagdollKnee : RagdollSidedBodyPart
    {
        [Header("Knee Properties")]
        [Tooltip("Reference to the shin component below this knee")]
        public RagdollShin shin;

        [Tooltip("Reference to the foot component below this knee")]
        public RagdollFoot foot;

        [Header("Auto-Join Options")]
        [Tooltip("If true, auto-creates shin GameObject if missing, placing it halfway between knee and foot")]
        public bool autoCreateShin = false;

        private void Awake()
        {
            if (autoCreateShin && shin == null)
            {
                AutoCreateShin();
            }
        }

        /// <summary>
        /// Auto-create shin GameObject halfway between knee and foot.
        /// </summary>
        private void AutoCreateShin()
        {
            // Find foot/ankle position
            Transform footTransform = null;

            // Find foot component
            if (foot != null)
            {
                footTransform = foot.PrimaryBoneTransform;
            }
            else
            {
                // Try to find foot by name
                Transform root = transform.root;
                footTransform = FindBoneByName(root, new[] { "foot", "ankle" }, side);
            }

            if (footTransform == null)
            {
                Debug.LogWarning($"[RagdollKnee] Cannot auto-create shin: missing foot reference for {side} leg");
                return;
            }

            // Create shin GameObject halfway between knee and foot
            Vector3 kneePos = PrimaryBoneTransform.position;
            Vector3 footPos = footTransform.position;
            Vector3 shinPos = (kneePos + footPos) * 0.5f;

            // Determine parent - prefer knee's parent
            Transform parentTransform = transform.parent;
            if (parentTransform == null && transform.root != null)
                parentTransform = transform.root;

            GameObject shinObj = new GameObject($"{side}_Shin_Auto");
            shinObj.transform.position = shinPos;
            shinObj.transform.SetParent(parentTransform, worldPositionStays: true);

            // Add RagdollShin component
            RagdollShin shinComponent = shinObj.AddComponent<RagdollShin>();
            shinComponent.side = side;
            shinComponent.knee = this;
            if (foot != null)
            {
                shinComponent.foot = foot;
            }
            shin = shinComponent;

            Debug.Log($"[RagdollKnee] Auto-created shin GameObject at position {shinPos} for {side} leg");
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
