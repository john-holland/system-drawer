// the shin (foreleg/lower leg) contains two bones (tibia and fibula) that rotate to turn the foot
// the shin connects the knee to the ankle/foot
// the shin muscles control foot movement and provide stability

using UnityEngine;

namespace Locomotion.Musculature
{
    /// <summary>
    /// Marker for the shin/foreleg (lower leg between knee and ankle).
    /// Contains the tibia and fibula bones.
    /// </summary>
    public sealed class RagdollShin : RagdollSidedBodyPart
    {
        [Header("Shin Properties")]
        [Tooltip("Reference to the knee component above this shin")]
        public RagdollKnee knee;

        [Tooltip("Reference to the foot component below this shin")]
        public RagdollFoot foot;

        [Header("Auto-Join Options")]
        [Tooltip("If true, auto-creates knee GameObject if missing, placing it halfway between hip and ankle")]
        public bool autoCreateKnee = false;

        private void Awake()
        {
            if (autoCreateKnee && knee == null)
            {
                AutoCreateKnee();
            }
        }

        /// <summary>
        /// Auto-create knee GameObject halfway between hip and ankle.
        /// </summary>
        private void AutoCreateKnee()
        {
            // Find hip (upper leg) and ankle (foot) positions
            Transform hipTransform = null;
            Transform ankleTransform = null;

            // Find leg (upper leg/thigh) - this is the hip connection point
            RagdollLeg leg = GetComponentInParent<RagdollLeg>();
            if (leg != null)
            {
                hipTransform = leg.PrimaryBoneTransform;
            }
            else
            {
                // Try to find leg by name
                Transform root = transform.root;
                hipTransform = FindBoneByName(root, new[] { "thigh", "upperleg", "upper_leg", "upleg", "leg" }, side);
            }

            // If still not found, try pelvis
            if (hipTransform == null)
            {
                RagdollPelvis pelvis = GetComponentInParent<RagdollPelvis>();
                if (pelvis != null)
                {
                    hipTransform = pelvis.PrimaryBoneTransform;
                }
                else
                {
                    // Try to find hip bone by name
                    Transform root = transform.root;
                    hipTransform = FindBoneByName(root, new[] { "hip", "pelvis", "hips" }, side);
                }
            }

            // Find foot/ankle
            if (foot != null)
            {
                ankleTransform = foot.PrimaryBoneTransform;
            }
            else
            {
                // Try to find foot by name
                Transform root = transform.root;
                ankleTransform = FindBoneByName(root, new[] { "foot", "ankle" }, side);
            }

            if (hipTransform == null || ankleTransform == null)
            {
                Debug.LogWarning($"[RagdollShin] Cannot auto-create knee: missing hip or ankle reference. Hip: {hipTransform != null}, Ankle: {ankleTransform != null}");
                return;
            }

            // Create knee GameObject halfway between hip and ankle
            Vector3 hipPos = hipTransform.position;
            Vector3 anklePos = ankleTransform.position;
            Vector3 kneePos = (hipPos + anklePos) * 0.5f;

            // Determine parent - prefer shin's parent, or leg if available
            Transform parentTransform = transform.parent;
            if (leg != null && leg.transform != null)
            {
                parentTransform = leg.transform.parent ?? transform.parent;
            }

            GameObject kneeObj = new GameObject($"{side}_Knee_Auto");
            kneeObj.transform.position = kneePos;
            kneeObj.transform.SetParent(parentTransform, worldPositionStays: true);

            // Add RagdollKnee component
            RagdollKnee kneeComponent = kneeObj.AddComponent<RagdollKnee>();
            kneeComponent.side = side;
            knee = kneeComponent;

            Debug.Log($"[RagdollShin] Auto-created knee GameObject at position {kneePos} for {side} leg");
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
