using System;
using System.Collections.Generic;
using UnityEngine;

namespace Locomotion.Narrative
{
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

        public BehaviorTreeGoal ToRuntimeGoal(NarrativeExecutionContext ctx)
        {
            var g = new BehaviorTreeGoal
            {
                goalName = goalName,
                type = type,
                targetPosition = targetPosition,
                priority = priority,
                requiresCleanup = requiresCleanup,
                cleanupUrgency = cleanupUrgency
            };

            if (!string.IsNullOrWhiteSpace(targetKey) && ctx.TryResolveObject(targetKey, out var obj))
            {
                if (obj is GameObject go) g.target = go;
                else if (obj is Component c) g.target = c.gameObject;
            }

            // Best-effort: map NarrativeValue into object values for the existing BT goal structure.
            // (Note: BehaviorTreeGoal.parameters uses Dictionary<string, object> which isn't Unity-serializable,
            // but is fine for runtime injection.)
            if (parameters != null)
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

                    g.parameters[p.key] = v;
                }
            }

            return g;
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

            var bt = go.GetComponent<BehaviorTree>();
            if (bt == null)
                return BehaviorTreeStatus.Failure;

            if (!started)
            {
                bt.SetGoal(goal.ToRuntimeGoal(ctx));
                started = true;
            }

            return bt.Execute();
        }
    }
}

