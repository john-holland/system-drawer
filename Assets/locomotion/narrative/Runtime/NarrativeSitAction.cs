using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action for sitting behavior - lowers seatbones and holds position.
    /// </summary>
    [Serializable]
    public class NarrativeSitAction : NarrativeActionSpec
    {
        public enum SitMode
        {
            AtTarget,
            InPlace
        }

        [Header("Sit Settings")]
        [Tooltip("When enabled, node will not defer")]
        public bool shouldSit = true;

        [Tooltip("Sit mode: AtTarget moves to target position, InPlace sits at current location")]
        public SitMode sitMode = SitMode.InPlace;

        [Tooltip("Target position for AtTarget mode (world space)")]
        public Vector3 targetPosition = Vector3.zero;

        [Tooltip("Speed at which seatbones lower (units per second)")]
        [Range(0.1f, 10f)]
        public float seatBoneLoweringSpeed = 1f;

        [Tooltip("Key resolved via NarrativeBindings for the ragdoll actor GameObject")]
        public string actorKey = "actor";

        [Tooltip("Target height offset for sitting (how far to lower pelvis)")]
        public float sitHeightOffset = -0.3f;

        [Tooltip("Tolerance for considering sitting complete")]
        public float sitTolerance = 0.05f;

        [NonSerialized]
        private bool isSitting = false;

        [NonSerialized]
        private bool started = false;

        [NonSerialized]
        private Vector3 targetSitPosition = Vector3.zero;

        [NonSerialized]
        private float currentLoweringProgress = 0f;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!shouldSit)
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(actorKey, out var actorGo) || actorGo == null)
            {
                Debug.LogWarning("[NarrativeSitAction] Could not resolve actor GameObject");
                return BehaviorTreeStatus.Failure;
            }

            // Get RagdollSystem using reflection
            var ragdollSystemType = System.Type.GetType("RagdollSystem, Assembly-CSharp");
            if (ragdollSystemType == null)
            {
                ragdollSystemType = System.Type.GetType("RagdollSystem, Locomotion.Runtime");
            }

            if (ragdollSystemType == null)
            {
                Debug.LogError("[NarrativeSitAction] Could not find RagdollSystem type");
                return BehaviorTreeStatus.Failure;
            }

            var ragdollSystem = actorGo.GetComponent(ragdollSystemType);
            if (ragdollSystem == null)
            {
                Debug.LogWarning("[NarrativeSitAction] Actor does not have RagdollSystem component");
                return BehaviorTreeStatus.Failure;
            }

            if (!started)
            {
                InitializeSitting(ragdollSystem, actorGo);
                started = true;
            }

            if (isSitting)
            {
                // Check if we've reached target position
                if (HasReachedSitPosition(ragdollSystem, actorGo))
                {
                    return BehaviorTreeStatus.Success;
                }

                // Continue lowering seatbones
                return LowerSeatbones(ragdollSystem, actorGo);
            }

            // Move to target position if needed
            if (sitMode == SitMode.AtTarget)
            {
                return MoveToTargetPosition(ragdollSystem, actorGo);
            }

            // Start sitting in place
            isSitting = true;
            return BehaviorTreeStatus.Running;
        }

        private void InitializeSitting(object ragdollSystem, GameObject actorGo)
        {
            // Get pelvis component using reflection
            var pelvisComponentProp = ragdollSystem.GetType().GetProperty("pelvisComponent");
            System.Reflection.FieldInfo pelvisComponentField = null;
            if (pelvisComponentProp == null)
            {
                pelvisComponentField = ragdollSystem.GetType().GetField("pelvisComponent");
            }

            object pelvisComponent = null;
            if (pelvisComponentProp != null)
            {
                pelvisComponent = pelvisComponentProp.GetValue(ragdollSystem);
            }
            else if (pelvisComponentField != null)
            {
                pelvisComponent = pelvisComponentField.GetValue(ragdollSystem);
            }

            if (pelvisComponent == null)
            {
                Debug.LogWarning("[NarrativeSitAction] Could not find pelvis component");
                return;
            }

            // Get pelvis transform
            var pelvisTransformProp = pelvisComponent.GetType().GetProperty("PrimaryBoneTransform");
            if (pelvisTransformProp == null)
            {
                pelvisTransformProp = pelvisComponent.GetType().GetProperty("boneTransform");
            }

            Transform pelvisTransform = null;
            if (pelvisTransformProp != null)
            {
                pelvisTransform = pelvisTransformProp.GetValue(pelvisComponent) as Transform;
            }

            if (pelvisTransform == null)
            {
                Debug.LogWarning("[NarrativeSitAction] Could not get pelvis transform");
                return;
            }

            // Calculate target sit position
            if (sitMode == SitMode.AtTarget)
            {
                targetSitPosition = targetPosition;
            }
            else
            {
                // In place - use current position with height offset
                targetSitPosition = pelvisTransform.position + Vector3.up * sitHeightOffset;
            }

            currentLoweringProgress = 0f;
        }

        private BehaviorTreeStatus MoveToTargetPosition(object ragdollSystem, GameObject actorGo)
        {
            // For now, assume we can move to position
            // In a full implementation, this would use pathfinding or movement system
            isSitting = true;
            return BehaviorTreeStatus.Running;
        }

        private BehaviorTreeStatus LowerSeatbones(object ragdollSystem, GameObject actorGo)
        {
            // Get pelvis component
            var pelvisComponentProp = ragdollSystem.GetType().GetProperty("pelvisComponent");
            System.Reflection.FieldInfo pelvisComponentField = null;
            if (pelvisComponentProp == null)
            {
                pelvisComponentField = ragdollSystem.GetType().GetField("pelvisComponent");
            }

            if (pelvisComponentProp == null && pelvisComponentField == null)
                return BehaviorTreeStatus.Failure;

            object pelvisComponent = null;
            if (pelvisComponentProp != null)
            {
                pelvisComponent = pelvisComponentProp.GetValue(ragdollSystem);
            }
            else if (pelvisComponentField != null)
            {
                pelvisComponent = pelvisComponentField.GetValue(ragdollSystem);
            }
            if (pelvisComponent == null)
                return BehaviorTreeStatus.Failure;

            // Get pelvis transform
            var pelvisTransformProp = pelvisComponent.GetType().GetProperty("PrimaryBoneTransform");
            if (pelvisTransformProp == null)
            {
                pelvisTransformProp = pelvisComponent.GetType().GetProperty("boneTransform");
            }

            if (pelvisTransformProp == null)
                return BehaviorTreeStatus.Failure;

            var pelvisTransform = pelvisTransformProp.GetValue(pelvisComponent) as Transform;
            if (pelvisTransform == null)
                return BehaviorTreeStatus.Failure;

            // Calculate lowering progress
            float deltaTime = Time.deltaTime;
            currentLoweringProgress += seatBoneLoweringSpeed * deltaTime;
            currentLoweringProgress = Mathf.Clamp01(currentLoweringProgress);

            // Interpolate pelvis position
            Vector3 currentPos = pelvisTransform.position;
            Vector3 startPos = currentPos;
            Vector3 targetPos = targetSitPosition;

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, currentLoweringProgress);

            // Apply position change via Rigidbody if available
            var pelvisRigidbody = pelvisTransform.GetComponent<Rigidbody>();
            if (pelvisRigidbody != null && !pelvisRigidbody.isKinematic)
            {
                // Use MovePosition for smooth movement
                pelvisRigidbody.MovePosition(newPos);
            }
            else
            {
                pelvisTransform.position = newPos;
            }

            return BehaviorTreeStatus.Running;
        }

        private bool HasReachedSitPosition(object ragdollSystem, GameObject actorGo)
        {
            // Get pelvis component
            var pelvisComponentProp = ragdollSystem.GetType().GetProperty("pelvisComponent");
            System.Reflection.FieldInfo pelvisComponentField = null;
            if (pelvisComponentProp == null)
            {
                pelvisComponentField = ragdollSystem.GetType().GetField("pelvisComponent");
            }

            if (pelvisComponentProp == null && pelvisComponentField == null)
                return false;

            object pelvisComponent = null;
            if (pelvisComponentProp != null)
            {
                pelvisComponent = pelvisComponentProp.GetValue(ragdollSystem);
            }
            else if (pelvisComponentField != null)
            {
                pelvisComponent = pelvisComponentField.GetValue(ragdollSystem);
            }
            if (pelvisComponent == null)
                return false;

            // Get pelvis transform
            var pelvisTransformProp = pelvisComponent.GetType().GetProperty("PrimaryBoneTransform");
            if (pelvisTransformProp == null)
            {
                pelvisTransformProp = pelvisComponent.GetType().GetProperty("boneTransform");
            }

            if (pelvisTransformProp == null)
                return false;

            var pelvisTransform = pelvisTransformProp.GetValue(pelvisComponent) as Transform;
            if (pelvisTransform == null)
                return false;

            // Check distance to target
            float distance = Vector3.Distance(pelvisTransform.position, targetSitPosition);
            return distance <= sitTolerance && currentLoweringProgress >= 1f;
        }

        /// <summary>
        /// Reset sitting state (for reentry).
        /// </summary>
        public void Reset()
        {
            isSitting = false;
            started = false;
            currentLoweringProgress = 0f;
        }
    }
}
