// the shoulder is a ball-and-socket joint connecting the upper arm to the torso
// the shoulder has a wide range of motion (6 DOF, ~170 degrees) allowing the arm to move in multiple directions
// the shoulder connects to the clavicle (collarbone) and scapula (shoulder blade)
// muscles that control the shoulder include the trapezius, deltoids, and rotator cuff muscles

using UnityEngine;

namespace Locomotion.Musculature
{
    public sealed class RagdollShoulder : RagdollSidedBodyPart
    {
        [Header("Shoulder Properties")]
        [Tooltip("Reference to the collarbone component above this shoulder")]
        public RagdollCollarbone collarbone;

        [Tooltip("Reference to the upper arm component below this shoulder")]
        public RagdollUpperarm upperarm;

        [Tooltip("Reference to the elbow component below this shoulder")]
        public RagdollElbow elbow;

        [Header("Auto-Join Options")]
        [Tooltip("If true, auto-creates upper arm GameObject if missing, placing it halfway between shoulder and elbow")]
        public bool autoCreateUpperarm = false;

        private void Awake()
        {
            if (autoCreateUpperarm && upperarm == null)
            {
                AutoCreateUpperarm();
            }
        }

        /// <summary>
        /// Auto-create upper arm GameObject halfway between shoulder and elbow.
        /// </summary>
        private void AutoCreateUpperarm()
        {
            // Find elbow position
            Transform elbowTransform = null;

            // Find elbow component
            if (elbow != null)
            {
                elbowTransform = elbow.PrimaryBoneTransform;
            }
            else
            {
                // Try to find elbow by name
                Transform root = transform.root;
                elbowTransform = FindBoneByName(root, new[] { "elbow" }, side);
            }

            if (elbowTransform == null)
            {
                Debug.LogWarning($"[RagdollShoulder] Cannot auto-create upper arm: missing elbow reference for {side} arm");
                return;
            }

            // Create upper arm GameObject halfway between shoulder and elbow
            Vector3 shoulderPos = PrimaryBoneTransform.position;
            Vector3 elbowPos = elbowTransform.position;
            Vector3 upperarmPos = (shoulderPos + elbowPos) * 0.5f;

            // Determine parent - prefer shoulder's parent
            Transform parentTransform = transform.parent;
            if (parentTransform == null && transform.root != null)
                parentTransform = transform.root;

            GameObject upperarmObj = new GameObject($"{side}_Upperarm_Auto");
            upperarmObj.transform.position = upperarmPos;
            upperarmObj.transform.SetParent(parentTransform, worldPositionStays: true);

            // Add RagdollUpperarm component
            RagdollUpperarm upperarmComponent = upperarmObj.AddComponent<RagdollUpperarm>();
            upperarmComponent.side = side;
            upperarmComponent.shoulder = this;
            if (elbow != null)
            {
                upperarmComponent.elbow = elbow;
            }
            upperarm = upperarmComponent;

            Debug.Log($"[RagdollShoulder] Auto-created upper arm GameObject at position {upperarmPos} for {side} arm");
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
