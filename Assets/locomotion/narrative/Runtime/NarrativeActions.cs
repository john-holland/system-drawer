using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Behavior tree execution status enum (local copy to avoid Runtime dependency).
    /// </summary>
    public enum BehaviorTreeStatus
    {
        Success,
        Failure,
        Running
    }

    /// <summary>
    /// Types of behavior tree goals (local copy to avoid Runtime dependency).
    /// </summary>
    public enum GoalType
    {
        ToolUsage,
        Movement,
        Interaction,
        Cleanup,
        Composite // Multiple sub-goals
    }

    /// <summary>
    /// Cleanup urgency levels for tool return goals (local copy to avoid Runtime dependency).
    /// </summary>
    public enum CleanupUrgency
    {
        Immediate,    // Return immediately after use
        AfterTask,    // Return after current task complete
        LowPriority   // Return when convenient
    }

    [Serializable]
    public abstract class NarrativeActionSpec
    {
        public NarrativeContingency contingency = new NarrativeContingency();

        /// <summary>
        /// Execute one step of this action. Return Running to continue across frames.
        /// </summary>
        public abstract BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state);
    }

    [Serializable]
    public class SpawnPrefabAction : NarrativeActionSpec
    {
        public GameObject prefab;
        public string parentKey;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public bool worldSpace = false;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (prefab == null)
                return BehaviorTreeStatus.Failure;

            Transform parent = null;
            if (!string.IsNullOrWhiteSpace(parentKey) && ctx.TryResolveObject(parentKey, out var obj))
            {
                if (obj is GameObject go) parent = go.transform;
                else if (obj is Component c) parent = c.transform;
            }

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            if (instance != null)
            {
                if (worldSpace)
                {
                    instance.transform.position = localPosition;
                    instance.transform.rotation = Quaternion.Euler(localEulerAngles);
                }
                else
                {
                    instance.transform.localPosition = localPosition;
                    instance.transform.localRotation = Quaternion.Euler(localEulerAngles);
                }
            }

            return BehaviorTreeStatus.Success;
        }
    }

    [Serializable]
    public class SetPropertyAction : NarrativeActionSpec
    {
        public string targetKey;
        public string componentTypeName;
        public string memberName;
        public NarrativeValue value;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(targetKey, out var go) || go == null)
                return BehaviorTreeStatus.Failure;

            bool ok = NarrativeReflection.TrySetMemberValue(go, componentTypeName, memberName, value);
            return ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }
    }

    [Serializable]
    public class CallMethodAction : NarrativeActionSpec
    {
        public string targetKey;
        public string componentTypeName;
        public string methodName;
        public NarrativeValue[] args = Array.Empty<NarrativeValue>();

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(targetKey, out var go) || go == null)
                return BehaviorTreeStatus.Failure;

            bool ok = NarrativeReflection.TryInvokeMethod(go, componentTypeName, methodName, args, out _);
            return ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }
    }

    [Serializable]
    public class NarrativeGoalParam
    {
        public string key;
        public NarrativeValue value;
    }

    [Serializable]
    public class BehaviorTreeGoalSpec
    {
        public string goalName;
        public GoalType type = GoalType.Movement;

        [Tooltip("Key resolved via NarrativeBindings for the goal target.")]
        public string targetKey;

        public Vector3 targetPosition;
        public int priority = 5;
        public bool requiresCleanup = false;
        public CleanupUrgency cleanupUrgency = CleanupUrgency.AfterTask;

        public List<NarrativeGoalParam> parameters = new List<NarrativeGoalParam>();

        public object ToRuntimeGoal(NarrativeExecutionContext ctx)
        {
            // Use reflection to create BehaviorTreeGoal from Runtime assembly
            var goalType = System.Type.GetType("BehaviorTreeGoal, Locomotion.Runtime");
            if (goalType == null)
            {
                // Fallback to Assembly-CSharp if Runtime is in default assembly
                goalType = System.Type.GetType("BehaviorTreeGoal, Assembly-CSharp");
            }
            if (goalType == null)
            {
                Debug.LogError("[BehaviorTreeGoalSpec] Could not find BehaviorTreeGoal type");
                return null;
            }

            var g = System.Activator.CreateInstance(goalType);
            if (g == null)
                return null;

            // Set properties using reflection
            SetProperty(g, "goalName", goalName);
            SetProperty(g, "type", Convert.ToInt32(type)); // Convert enum to int
            SetProperty(g, "targetPosition", targetPosition);
            SetProperty(g, "priority", priority);
            SetProperty(g, "requiresCleanup", requiresCleanup);
            SetProperty(g, "cleanupUrgency", Convert.ToInt32(cleanupUrgency)); // Convert enum to int

            if (!string.IsNullOrWhiteSpace(targetKey) && ctx.TryResolveObject(targetKey, out var obj))
            {
                GameObject targetGo = null;
                if (obj is GameObject go) targetGo = go;
                else if (obj is Component c) targetGo = c.gameObject;
                SetProperty(g, "target", targetGo);
            }

            // Set parameters dictionary
            var parametersProp = goalType.GetProperty("parameters");
            if (parametersProp != null)
            {
                var parametersDict = parametersProp.GetValue(g) as System.Collections.IDictionary;
                if (parametersDict != null && parameters != null)
                {
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        var p = parameters[i];
                        if (p == null || string.IsNullOrWhiteSpace(p.key))
                            continue;

                        object v = p.value.type switch
                        {
                            NarrativeValueType.Bool => p.value.boolValue,
                            NarrativeValueType.Int => p.value.intValue,
                            NarrativeValueType.Float => p.value.floatValue,
                            NarrativeValueType.String => p.value.stringValue,
                            NarrativeValueType.Vector3 => p.value.vector3Value,
                            _ => null
                        };

                        if (v != null)
                            parametersDict[p.key] = v;
                    }
                }
            }

            return g;
        }

        private void SetProperty(object obj, string propertyName, object value)
        {
            if (obj == null) return;
            var prop = obj.GetType().GetProperty(propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, value);
            }
        }
    }

    [Serializable]
    public class RunBehaviorTreeAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the BehaviorTree host GameObject.")]
        public string actorKey;

        public BehaviorTreeGoalSpec goal = new BehaviorTreeGoalSpec();

        [NonSerialized] private bool started;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(actorKey, out var go) || go == null)
                return BehaviorTreeStatus.Failure;

            // Use reflection to get BehaviorTree component
            var behaviorTreeType = System.Type.GetType("BehaviorTree, Locomotion.Runtime");
            if (behaviorTreeType == null)
            {
                // Fallback to Assembly-CSharp if Runtime is in default assembly
                behaviorTreeType = System.Type.GetType("BehaviorTree, Assembly-CSharp");
            }
            if (behaviorTreeType == null)
            {
                Debug.LogError("[RunBehaviorTreeAction] Could not find BehaviorTree type");
                return BehaviorTreeStatus.Failure;
            }

            var bt = go.GetComponent(behaviorTreeType);
            if (bt == null)
                return BehaviorTreeStatus.Failure;

            if (!started)
            {
                var goalObj = goal.ToRuntimeGoal(ctx);
                if (goalObj != null)
                {
                    var setGoalMethod = behaviorTreeType.GetMethod("SetGoal");
                    if (setGoalMethod != null)
                    {
                        setGoalMethod.Invoke(bt, new object[] { goalObj });
                    }
                }
                started = true;
            }

            // Execute behavior tree
            var executeMethod = behaviorTreeType.GetMethod("Execute");
            if (executeMethod != null)
            {
                var result = executeMethod.Invoke(bt, null);
                if (result != null)
                {
                    // Convert Runtime's BehaviorTreeStatus to our local enum
                    int statusInt = Convert.ToInt32(result);
                    if (statusInt >= 0 && statusInt <= 2)
                    {
                        return (BehaviorTreeStatus)statusInt;
                    }
                }
            }

            return BehaviorTreeStatus.Failure;
        }
    }
}

