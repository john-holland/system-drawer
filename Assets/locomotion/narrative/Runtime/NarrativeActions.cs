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

        public virtual bool SupportsUndo => false;

        public abstract BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state);

        public virtual void Undo(NarrativeExecutionContext ctx, NarrativeRuntimeState state) { }
    }

    [Serializable]
    public class SpawnPrefabAction : NarrativeActionSpec
    {
        public GameObject prefab;
        public string parentKey;
        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public bool worldSpace = false;

        [NonSerialized] GameObject _spawnedInstance;
        [NonSerialized] int _spawnedInstanceId;

        public override bool SupportsUndo => true;

        internal int LastSpawnedInstanceId => _spawnedInstanceId;

        internal void RestoreUndoInstanceId(int instanceId)
        {
            if (_spawnedInstanceId == 0 && instanceId != 0)
                _spawnedInstanceId = instanceId;
        }

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (prefab == null)
                return BehaviorTreeStatus.Failure;

            var parent = ResolveParent(ctx);
            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            TrackSpawnedInstance(instance, parent);
            if (_spawnedInstance != null)
            {
                if (worldSpace)
                {
                    _spawnedInstance.transform.position = localPosition;
                    _spawnedInstance.transform.rotation = Quaternion.Euler(localEulerAngles);
                }
                else
                {
                    _spawnedInstance.transform.localPosition = localPosition;
                    _spawnedInstance.transform.localRotation = Quaternion.Euler(localEulerAngles);
                }
                var reportTag = _spawnedInstance.GetComponentInChildren<ContinuuuumLemmaReportTag>();
                if (reportTag != null && !string.IsNullOrEmpty(reportTag.entryId))
                {
                    LemmaComponentReportCollector.NotifyPrefabSpawned(
                        reportTag.entryId,
                        _spawnedInstance,
                        reportTag.prefabRef);
                }
            }

            return BehaviorTreeStatus.Success;
        }

        public override void Undo(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            var instance = ResolveTrackedInstance(ctx);
            if (instance == null)
                return;
            // Rewind/undo must remove the instance synchronously (Destroy is end-of-frame).
            UnityEngine.Object.DestroyImmediate(instance);
            _spawnedInstance = null;
            _spawnedInstanceId = 0;
        }

        static Transform ResolveParent(NarrativeExecutionContext ctx, string key)
        {
            if (string.IsNullOrWhiteSpace(key) || !ctx.TryResolveObject(key, out var obj))
                return null;
            if (obj is GameObject go)
                return go.transform;
            if (obj is Component c)
                return c.transform;
            return null;
        }

        Transform ResolveParent(NarrativeExecutionContext ctx) => ResolveParent(ctx, parentKey);

        void TrackSpawnedInstance(GameObject instance, Transform parent)
        {
            if (instance == null || instance == prefab)
                instance = FindSpawnedClone(parent);
            else if (!IsSpawnClone(instance))
                instance = FindSpawnedClone(parent) ?? instance;
            _spawnedInstance = instance;
            _spawnedInstanceId = instance != null ? instance.GetInstanceID() : 0;
        }

        GameObject ResolveTrackedInstance(NarrativeExecutionContext ctx)
        {
            if (_spawnedInstance != null)
                return _spawnedInstance;
            if (_spawnedInstanceId != 0)
            {
                var byId = FindByInstanceId(_spawnedInstanceId);
                if (byId != null)
                    return byId;
            }
            return FindSpawnedClone(ResolveParent(ctx));
        }

        bool IsSpawnClone(GameObject candidate) =>
            candidate != null && prefab != null && candidate.name == prefab.name + "(Clone)";

        GameObject FindSpawnedClone(Transform parent)
        {
            if (prefab == null)
                return null;

            string cloneName = prefab.name + "(Clone)";
            if (parent != null)
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i).gameObject;
                    if (child.name == cloneName)
                        return child;
                }
                return null;
            }

            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go != prefab && go.name == cloneName)
                    return go;
            }

            return null;
        }

        static GameObject FindByInstanceId(int instanceId)
        {
            if (instanceId == 0)
                return null;
            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go != null && go.GetInstanceID() == instanceId)
                    return go;
            }
            return null;
        }
    }

    [Serializable]
    public class SetPropertyAction : NarrativeActionSpec
    {
        public string targetKey;
        public string componentTypeName;
        public string memberName;
        public NarrativeValue value;

        [NonSerialized] NarrativeValue _previousValue;
        [NonSerialized] bool _hasPrevious;

        public override bool SupportsUndo => true;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (!ctx.TryResolveGameObject(targetKey, out var go) || go == null)
                return BehaviorTreeStatus.Failure;

            if (!_hasPrevious)
            {
                _previousValue = NarrativeValue.FromObject(
                    NarrativeReflection.TryGetMemberValue(go, componentTypeName, memberName));
                _hasPrevious = true;
            }

            bool ok = NarrativeReflection.TrySetMemberValue(go, componentTypeName, memberName, value);
            return ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }

        public override void Undo(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!_hasPrevious)
                return;
            if (ctx.TryResolveGameObject(targetKey, out var go) && go != null)
                NarrativeReflection.TrySetMemberValue(go, componentTypeName, memberName, _previousValue);
            _hasPrevious = false;
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

    /// <summary>
    /// Defines a behavior tree goal for Run Behavior Tree actions. Set goalName (e.g. "GoTo", "PickUp"),
    /// type (Movement, Interaction, Composite), targetKey (NarrativeBindings key for the target GameObject),
    /// targetPosition (world position), and optional parameters. The behavior tree runs with this goal when the action executes.
    /// </summary>
    [Serializable]
    public class BehaviorTreeGoalSpec
    {
        [Tooltip("Goal name the behavior tree recognizes (e.g. GoTo, PickUp, Sit).")]
        public string goalName;
        [Tooltip("Movement = go to position/target; Interaction = use object; Composite = multiple sub-goals.")]
        public GoalType type = GoalType.Movement;

        [Tooltip("NarrativeBindings key for the goal target GameObject (e.g. \"chair\", \"player\").")]
        public string targetKey;

        [Tooltip("World position for movement goals when no targetKey is used.")]
        public Vector3 targetPosition;
        public int priority = 5;
        public bool requiresCleanup = false;
        public CleanupUrgency cleanupUrgency = CleanupUrgency.AfterTask;

        [Tooltip("Optional key-value parameters passed to the behavior tree goal.")]
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

        public override bool SupportsUndo => true;

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

        public override void Undo(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            started = false;
            if (!ctx.TryResolveGameObject(actorKey, out var go) || go == null)
                return;
            var behaviorTreeType = System.Type.GetType("BehaviorTree, Locomotion.Runtime")
                ?? System.Type.GetType("BehaviorTree, Assembly-CSharp");
            if (behaviorTreeType == null)
                return;
            var bt = go.GetComponent(behaviorTreeType);
            if (bt == null)
                return;
            var rootProp = behaviorTreeType.GetProperty("rootNode");
            var currentProp = behaviorTreeType.GetProperty("currentNode");
            if (rootProp != null && currentProp != null)
                currentProp.SetValue(bt, rootProp.GetValue(bt));
        }
    }
    /// <summary>
    /// Resolve two actor keys to brain components and dispatch a thought message via Locomotion.Runtime.
    /// </summary>
    [Serializable]
    public class SendThoughtAction : NarrativeActionSpec
    {
        public string senderKey;
        public string receiverKey;
        public NarrativeThoughtType thoughtType = NarrativeThoughtType.Decision;

        [Tooltip("Used when thoughtType is Decision.")]
        public NarrativeDecisionThoughtPayload decisionPayload = new NarrativeDecisionThoughtPayload();

        [Tooltip("Used when thoughtType is Query.")]
        public NarrativeQueryThoughtPayload queryPayload = new NarrativeQueryThoughtPayload();

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (string.IsNullOrWhiteSpace(senderKey) || string.IsNullOrWhiteSpace(receiverKey))
                return BehaviorTreeStatus.Failure;
            if (!ctx.TryResolveGameObject(senderKey, out var sgo) || sgo == null)
                return BehaviorTreeStatus.Failure;
            if (!ctx.TryResolveGameObject(receiverKey, out var rgo) || rgo == null)
                return BehaviorTreeStatus.Failure;

            object payload = BuildPayload();
            if (!TryDispatchThought(sgo, rgo, (int)thoughtType, payload))
                return BehaviorTreeStatus.Failure;
            return BehaviorTreeStatus.Success;
        }

        static bool TryDispatchThought(GameObject sender, GameObject receiver, int thoughtTypeOrdinal, object payload)
        {
            var dispatchType = System.Type.GetType("LocomotionThoughtDispatch, Locomotion.Runtime");
            var method = dispatchType?.GetMethod(
                "TrySendThought",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
                return false;
            return (bool)method.Invoke(null, new object[] { sender, receiver, thoughtTypeOrdinal, payload });
        }

        private object BuildPayload()
        {
            switch (thoughtType)
            {
                case NarrativeThoughtType.Decision:
                    return decisionPayload ?? new NarrativeDecisionThoughtPayload();
                case NarrativeThoughtType.Query:
                    if (queryPayload == null || string.IsNullOrEmpty(queryPayload.queryId))
                        return new NarrativeQueryThoughtPayload { queryId = Guid.NewGuid().ToString("N"), channels = NarrativeQueryChannel.All };
                    return queryPayload;
                case NarrativeThoughtType.Alert:
                    return new NarrativeAlertThoughtPayload();
                case NarrativeThoughtType.BehaviorTree:
                    return new NarrativeBehaviorTreeThoughtPayload();
                case NarrativeThoughtType.RequestPrune:
                    return new NarrativeRequestPruneThoughtPayload();
                default:
                    return null;
            }
        }
    }

    /// <summary>
    /// Opens a dialogue session and optionally waits for first line audio.
    /// </summary>
    [Serializable]
    public class RunDialogueAction : NarrativeActionSpec
    {
        public string setId = "book-concert";
        public string speakerKeyFallback = "actor";
        public bool openOnExecute = true;
        public bool waitForLineAudio = true;

        [NonSerialized] DialogueRunner _runner;
        [NonSerialized] bool _started;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (_runner == null)
            {
                _runner = UnityEngine.Object.FindAnyObjectByType<DialogueRunner>();
                if (_runner == null)
                {
                    var go = new GameObject("DialogueRunner");
                    _runner = go.AddComponent<DialogueRunner>();
                }
                _runner.setId = setId;
                _runner.executor = UnityEngine.Object.FindAnyObjectByType<NarrativeExecutor>();
                _runner.bindings = ctx.bindings;
            }

            if (!_started && openOnExecute)
            {
                _runner.OpenSession();
                _started = true;
                return BehaviorTreeStatus.Running;
            }

            if (waitForLineAudio && _runner.IsAudioPlaying())
                return BehaviorTreeStatus.Running;

            return BehaviorTreeStatus.Success;
        }
    }

    /// <summary>Opens a quest session and activates first objective.</summary>
    [Serializable]
    public class RunQuestObjectiveActionSpec : NarrativeActionSpec
    {
        public string setId = "little-prince-tour";
        public string objectiveId;
        public bool openOnExecute = true;

        [NonSerialized] QuestRunner _runner;
        [NonSerialized] bool _started;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            if (_runner == null)
            {
                _runner = UnityEngine.Object.FindAnyObjectByType<QuestRunner>();
                if (_runner == null)
                {
                    var go = new GameObject("QuestRunner");
                    _runner = go.AddComponent<QuestRunner>();
                }
                _runner.setId = setId;
                _runner.executor = UnityEngine.Object.FindAnyObjectByType<NarrativeExecutor>();
                _runner.bindings = ctx.bindings;
            }

            if (!_started && openOnExecute)
            {
                _runner.OpenQuestSet(resp =>
                {
                    if (resp != null && resp.ok && !string.IsNullOrEmpty(objectiveId))
                        _runner.ActivateObjective(objectiveId);
                });
                _started = true;
                return BehaviorTreeStatus.Running;
            }

            return BehaviorTreeStatus.Success;
        }
    }

    /// <summary>Run night sleep sim → populate dream buffer → recall fragment on wake.</summary>
    [Serializable]
    public class DreamMemoryNarrativeAction : NarrativeActionSpec
    {
        public MonoBehaviour dayRunner;
        public MonoBehaviour nightRunner;
        public MonoBehaviour dreamMemoryLstm;
        public bool recallOnWake = true;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            dayRunner?.GetType().GetMethod("RunDayComplete")?.Invoke(dayRunner, null);
            nightRunner?.GetType().GetMethod("RunNightComplete")?.Invoke(nightRunner, null);

            if (recallOnWake && dreamMemoryLstm != null)
            {
                var recall = dreamMemoryLstm.GetType().GetMethod("RecallDreamFragment");
                recall?.Invoke(dreamMemoryLstm, null);
            }

            return BehaviorTreeStatus.Success;
        }
    }
}

