using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action to change animation state.
    /// </summary>
    [Serializable]
    public class NarrativeChangeAnimationAction : NarrativeActionSpec
    {
        public enum AnimationState
        {
            Idle,
            Run,
            Walk,
            StartNew,
            Stop,
            Reset,
            Pause
        }

        [Tooltip("Key resolved via NarrativeBindings for the Animator GameObject")]
        public string animatorKey = "animator";

        [Tooltip("Animation state to change to")]
        public AnimationState animationState = AnimationState.Idle;

        [Tooltip("Animation parameter name (optional, uses state name if empty)")]
        public string parameterName = "";

        [Tooltip("Crossfade duration (0 = instant)")]
        public float crossfadeDuration = 0.25f;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(animatorKey, out var animatorGo) || animatorGo == null)
            {
                Debug.LogWarning("[NarrativeChangeAnimationAction] Could not resolve animator GameObject");
                return BehaviorTreeStatus.Failure;
            }

            Animator animator = animatorGo.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("[NarrativeChangeAnimationAction] GameObject does not have Animator component");
                return BehaviorTreeStatus.Failure;
            }

            // Determine parameter name
            string paramName = parameterName;
            if (string.IsNullOrEmpty(paramName))
            {
                paramName = animationState.ToString();
            }

            // Apply animation state change
            switch (animationState)
            {
                case AnimationState.Idle:
                case AnimationState.Run:
                case AnimationState.Walk:
                    // Set trigger or bool parameter
                    if (HasParameter(animator, paramName, AnimatorControllerParameterType.Trigger))
                    {
                        animator.SetTrigger(paramName);
                    }
                    else if (HasParameter(animator, paramName, AnimatorControllerParameterType.Bool))
                    {
                        animator.SetBool(paramName, true);
                    }
                    else
                    {
                        // Try to play state by name
                        animator.Play(paramName, 0, 0f);
                    }
                    break;

                case AnimationState.StartNew:
                    // Start new animation
                    if (!string.IsNullOrEmpty(paramName))
                    {
                        animator.Play(paramName, 0, 0f);
                    }
                    break;

                case AnimationState.Stop:
                    // Stop all animations
                    animator.enabled = false;
                    break;

                case AnimationState.Reset:
                    // Reset animator
                    animator.Rebind();
                    break;

                case AnimationState.Pause:
                    // Pause (set speed to 0)
                    animator.speed = 0f;
                    break;
            }

            return BehaviorTreeStatus.Success;
        }

        private bool HasParameter(Animator animator, string paramName, AnimatorControllerParameterType type)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return false;

            foreach (var param in animator.parameters)
            {
                if (param.name == paramName && param.type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
