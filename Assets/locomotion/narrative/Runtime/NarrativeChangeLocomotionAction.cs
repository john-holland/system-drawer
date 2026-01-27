using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to change locomotion mode.
    /// </summary>
    [Serializable]
    public class NarrativeChangeLocomotionAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the locomotion system GameObject")]
        public string locomotionSystemKey = "locomotion";

        [Tooltip("Locomotion mode to change to")]
        public string locomotionMode = "walk";

        [Tooltip("Speed multiplier for locomotion")]
        [Range(0.1f, 2f)]
        public float speedMultiplier = 1f;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(locomotionSystemKey, out var locomotionGo) || locomotionGo == null)
            {
                Debug.LogWarning("[NarrativeChangeLocomotionAction] Could not resolve locomotion system GameObject");
                return BehaviorTreeStatus.Failure;
            }

            // Try to find locomotion-related components
            // This would depend on the specific locomotion system implementation
            // For now, use reflection to find common locomotion components

            // Try FirstPersonController using reflection
            var firstPersonControllerType = System.Type.GetType("FirstPersonController, Assembly-CSharp");
            if (firstPersonControllerType == null)
            {
                firstPersonControllerType = System.Type.GetType("FirstPersonController, Locomotion.Runtime");
            }

            if (firstPersonControllerType != null)
            {
                var firstPersonController = locomotionGo.GetComponent(firstPersonControllerType);
                if (firstPersonController != null)
                {
                    // Use reflection to set walk/run speed
                    var walkSpeedProp = firstPersonControllerType.GetField("walkSpeed", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                    var runSpeedProp = firstPersonControllerType.GetField("runSpeed", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

                    if (walkSpeedProp != null && runSpeedProp != null)
                    {
                        float baseWalkSpeed = (float)walkSpeedProp.GetValue(firstPersonController);
                        float baseRunSpeed = (float)runSpeedProp.GetValue(firstPersonController);

                        if (locomotionMode.ToLower() == "walk")
                        {
                            walkSpeedProp.SetValue(firstPersonController, baseWalkSpeed * speedMultiplier);
                        }
                        else if (locomotionMode.ToLower() == "run")
                        {
                            runSpeedProp.SetValue(firstPersonController, baseRunSpeed * speedMultiplier);
                        }
                    }
                }
            }

            // Try to find other locomotion systems via reflection
            var locomotionTypes = new[] { 
                "LocomotionSystem", 
                "CharacterController", 
                "RagdollSystem" 
            };

            foreach (var typeName in locomotionTypes)
            {
                var locomotionType = System.Type.GetType(typeName + ", Assembly-CSharp");
                if (locomotionType == null)
                {
                    locomotionType = System.Type.GetType(typeName + ", Locomotion.Runtime");
                }

                if (locomotionType != null)
                {
                    var component = locomotionGo.GetComponent(locomotionType);
                    if (component != null)
                    {
                        // Try to set locomotion mode
                        var modeProp = locomotionType.GetProperty("mode");
                        if (modeProp != null && modeProp.CanWrite)
                        {
                            modeProp.SetValue(component, locomotionMode);
                            return BehaviorTreeStatus.Success;
                        }

                        var setModeMethod = locomotionType.GetMethod("SetMode");
                        if (setModeMethod != null)
                        {
                            setModeMethod.Invoke(component, new object[] { locomotionMode });
                            return BehaviorTreeStatus.Success;
                        }
                    }
                }
            }

            Debug.LogWarning($"[NarrativeChangeLocomotionAction] Could not change locomotion mode to: {locomotionMode}");
            return BehaviorTreeStatus.Failure;
        }
    }
}
