using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action for standing behavior - eschews current pose and resets to standing.
    /// </summary>
    [Serializable]
    public class NarrativeStandAction : NarrativeActionSpec
    {
        public enum StandMode
        {
            AtTarget,
            InPlace
        }

        [Header("Stand Settings")]
        [Tooltip("When enabled, node will not defer")]
        public bool shouldStand = true;

        [Tooltip("Stand mode: AtTarget moves to target position, InPlace stands at current location")]
        public StandMode standMode = StandMode.InPlace;

        [Tooltip("Target position for AtTarget mode (world space)")]
        public Vector3 targetPosition = Vector3.zero;

        [Tooltip("Key resolved via NarrativeBindings for the ragdoll actor GameObject")]
        public string actorKey = "actor";

        [Tooltip("Stand height offset (how high to raise pelvis)")]
        public float standHeightOffset = 0.5f;

        [Tooltip("Tolerance for considering standing complete")]
        public float standTolerance = 0.05f;

        [Tooltip("Speed at which to release constraints and stand up")]
        [Range(0.1f, 10f)]
        public float standUpSpeed = 2f;

        [NonSerialized]
        private bool isStanding = false;

        [NonSerialized]
        private bool started = false;

        [NonSerialized]
        private Vector3 targetStandPosition = Vector3.zero;

        [NonSerialized]
        private float currentStandingProgress = 0f;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!shouldStand)
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(actorKey, out var actorGo) || actorGo == null)
            {
                Debug.LogWarning("[NarrativeStandAction] Could not resolve actor GameObject");
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
                Debug.LogError("[NarrativeStandAction] Could not find RagdollSystem type");
                return BehaviorTreeStatus.Failure;
            }

            var ragdollSystem = actorGo.GetComponent(ragdollSystemType);
            if (ragdollSystem == null)
            {
                Debug.LogWarning("[NarrativeStandAction] Actor does not have RagdollSystem component");
                return BehaviorTreeStatus.Failure;
            }

            if (!started)
            {
                InitializeStanding(ragdollSystem, actorGo);
                started = true;
            }

            if (isStanding)
            {
                // Check if we've reached standing position
                if (HasReachedStandPosition(ragdollSystem, actorGo))
                {
                    return BehaviorTreeStatus.Success;
                }

                // Continue standing up
                return StandUp(ragdollSystem, actorGo);
            }

            // Release constraints and start standing
            ReleaseConstraints(ragdollSystem, actorGo);
            isStanding = true;
            return BehaviorTreeStatus.Running;
        }

        private void InitializeStanding(object ragdollSystem, GameObject actorGo)
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
                Debug.LogWarning("[NarrativeStandAction] Could not find pelvis component");
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
                Debug.LogWarning("[NarrativeStandAction] Could not get pelvis transform");
                return;
            }

            // Calculate target stand position
            if (standMode == StandMode.AtTarget)
            {
                targetStandPosition = targetPosition;
            }
            else
            {
                // In place - use current position with height offset
                targetStandPosition = pelvisTransform.position + Vector3.up * standHeightOffset;
            }

            currentStandingProgress = 0f;
        }

        private void ReleaseConstraints(object ragdollSystem, GameObject actorGo)
        {
            // Release any fixed joints or constraints that might be holding the pose
            // This would typically involve:
            // 1. Breaking any fixed joints
            // 2. Releasing muscle group activations
            // 3. Resetting joint limits

            // Get all FixedJoints and break them
            var fixedJoints = actorGo.GetComponentsInChildren<FixedJoint>();
            foreach (var joint in fixedJoints)
            {
                if (joint != null)
                {
                    // Break the joint by destroying it
                    UnityEngine.Object.Destroy(joint);
                }
            }

            // Release muscle group activations using reflection
            var activateMethod = ragdollSystem.GetType().GetMethod("ActivateMuscleGroup");
            if (activateMethod != null)
            {
                // Deactivate all muscle groups
                var muscleGroupsProp = ragdollSystem.GetType().GetProperty("muscleGroups");
                System.Reflection.FieldInfo muscleGroupsField = null;
                if (muscleGroupsProp == null)
                {
                    muscleGroupsField = ragdollSystem.GetType().GetField("muscleGroups");
                }

                System.Collections.IList muscleGroups = null;
                if (muscleGroupsProp != null)
                {
                    muscleGroups = muscleGroupsProp.GetValue(ragdollSystem) as System.Collections.IList;
                }
                else if (muscleGroupsField != null)
                {
                    muscleGroups = muscleGroupsField.GetValue(ragdollSystem) as System.Collections.IList;
                }

                if (muscleGroups != null)
                {
                    foreach (var group in muscleGroups)
                    {
                        if (group != null)
                        {
                            var groupNameProp = group.GetType().GetProperty("groupName");
                            if (groupNameProp != null)
                            {
                                string groupName = groupNameProp.GetValue(group) as string;
                                if (!string.IsNullOrEmpty(groupName))
                                {
                                    activateMethod.Invoke(ragdollSystem, new object[] { groupName, 0f });
                                }
                            }
                        }
                    }
                }
            }
        }

        private BehaviorTreeStatus StandUp(object ragdollSystem, GameObject actorGo)
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

            // Calculate standing progress
            float deltaTime = Time.deltaTime;
            currentStandingProgress += standUpSpeed * deltaTime;
            currentStandingProgress = Mathf.Clamp01(currentStandingProgress);

            // Interpolate pelvis position
            Vector3 currentPos = pelvisTransform.position;
            Vector3 startPos = currentPos;
            Vector3 targetPos = targetStandPosition;

            Vector3 newPos = Vector3.Lerp(startPos, targetPos, currentStandingProgress);

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

        private bool HasReachedStandPosition(object ragdollSystem, GameObject actorGo)
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
            float distance = Vector3.Distance(pelvisTransform.position, targetStandPosition);
            return distance <= standTolerance && currentStandingProgress >= 1f;
        }

        /// <summary>
        /// Reset standing state (for reentry).
        /// </summary>
        public void Reset()
        {
            isStanding = false;
            started = false;
            currentStandingProgress = 0f;
        }
    }
}
