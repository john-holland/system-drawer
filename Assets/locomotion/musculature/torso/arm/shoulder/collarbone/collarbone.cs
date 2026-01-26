// the collarbone (clavicle) connects the neck to the shoulder
// it provides structural support and allows the shoulder to move independently
// the collarbone is a long bone that runs horizontally from the neck to the shoulder

using UnityEngine;

namespace Locomotion.Musculature
{
    public sealed class RagdollCollarbone : RagdollSidedBodyPart
    {
        [Header("Collarbone Properties")]
        [Tooltip("Reference to the neck component above this collarbone")]
        public RagdollNeck neck;

        [Tooltip("Reference to the shoulder component below this collarbone")]
        public RagdollShoulder shoulder;

        [Header("Auto-Join Options")]
        [Tooltip("If true, auto-creates shoulder GameObject if missing, placing it halfway between collarbone and upper arm")]
        public bool autoCreateShoulder = false;

        private void Awake()
        {
            if (autoCreateShoulder && shoulder == null)
            {
                AutoCreateShoulder();
            }
        }

        /// <summary>
        /// Auto-create shoulder GameObject halfway between collarbone and upper arm.
        /// </summary>
        private void AutoCreateShoulder()
        {
            // Find neck position
            Transform neckTransform = null;
            if (neck != null)
            {
                neckTransform = neck.PrimaryBoneTransform;
            }
            else
            {
                // Try to find neck by name
                Transform root = transform.root;
                neckTransform = FindBoneByName(root, new[] { "neck" }, null);
            }

            // Find upper arm position (to determine shoulder placement)
            Transform upperarmTransform = null;
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

            if (neckTransform == null || upperarmTransform == null)
            {
                Debug.LogWarning($"[RagdollCollarbone] Cannot auto-create shoulder: missing neck or upper arm reference. Neck: {neckTransform != null}, Upper Arm: {upperarmTransform != null}");
                return;
            }

            // Create shoulder GameObject halfway between collarbone and upper arm
            Vector3 collarbonePos = PrimaryBoneTransform.position;
            Vector3 upperarmPos = upperarmTransform.position;
            Vector3 shoulderPos = (collarbonePos + upperarmPos) * 0.5f;

            // Determine parent - prefer collarbone's parent
            Transform parentTransform = transform.parent;
            if (parentTransform == null && transform.root != null)
                parentTransform = transform.root;

            GameObject shoulderObj = new GameObject($"{side}_Shoulder_Auto");
            shoulderObj.transform.position = shoulderPos;
            shoulderObj.transform.SetParent(parentTransform, worldPositionStays: true);

            // Add RagdollShoulder component
            RagdollShoulder shoulderComponent = shoulderObj.AddComponent<RagdollShoulder>();
            shoulderComponent.side = side;
            shoulderComponent.collarbone = this;
            if (upperarm != null)
            {
                shoulderComponent.upperarm = upperarm;
            }
            shoulder = shoulderComponent;

            Debug.Log($"[RagdollCollarbone] Auto-created shoulder GameObject at position {shoulderPos} for {side} arm");
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
