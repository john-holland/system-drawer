using UnityEngine;
using System;
using System.Collections.Generic;

namespace Locomotion.Narrative
{
    public class NarrativeExecutor : MonoBehaviour
    {
        [Header("Context")]
        public NarrativeClock clock;
        public NarrativeBindings bindings;
        public NarrativeScheduler scheduler;
        [Tooltip("WeatherSystem GameObject (uses reflection to avoid compile-time dependency)")]
        public GameObject weatherSystemObject;

        [Header("Debug")]
        public bool debugLogging = false;

        [SerializeField] private NarrativeRuntimeState runtimeState = new NarrativeRuntimeState();

        private NarrativeExecutionContext ctx;
        private NarrativeCalendarEvent activeEvent;
        private object weatherSystemComponent;
        private Type weatherSystemType;
        private readonly HashSet<string> _capturedThisEvent = new HashSet<string>();

        private void Awake()
        {
            if (clock == null) clock = FindAnyObjectByType<NarrativeClock>();
            if (bindings == null) bindings = FindAnyObjectByType<NarrativeBindings>();
            if (scheduler == null) scheduler = FindAnyObjectByType<NarrativeScheduler>();
            
            weatherSystemType = Type.GetType("Weather.WeatherSystem, Weather.Runtime");
            if (weatherSystemType != null)
            {
                if (weatherSystemObject == null)
                {
                    MonoBehaviour[] allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                    foreach (var mb in allMonoBehaviours)
                    {
                        if (weatherSystemType.IsAssignableFrom(mb.GetType()))
                        {
                            weatherSystemObject = mb.gameObject;
                            weatherSystemComponent = mb;
                            break;
                        }
                    }
                }
                else
                {
                    weatherSystemComponent = weatherSystemObject.GetComponent(weatherSystemType);
                }
            }
            
            ctx = new NarrativeExecutionContext(clock, bindings, weatherSystemComponent);
        }

        public NarrativeRuntimeState GetRuntimeState() => runtimeState;

        public void SetRuntimeState(NarrativeRuntimeState state)
        {
            runtimeState = state ?? new NarrativeRuntimeState();
        }

        public void PauseExecution()
        {
            activeEvent = null;
            runtimeState.activeEventId = null;
            runtimeState.isExecuting = false;
            runtimeState.nodeStack.Clear();
            runtimeState.childIndexStack.Clear();
            _capturedThisEvent.Clear();
        }

        public void StartEvent(NarrativeCalendarEvent evt)
        {
            if (evt == null) return;

            activeEvent = evt;
            runtimeState.activeEventId = evt.id;
            runtimeState.isExecuting = true;
            runtimeState.nodeStack.Clear();
            runtimeState.childIndexStack.Clear();
            _capturedThisEvent.Clear();

            if (debugLogging)
                Debug.Log($"[NarrativeExecutor] Start event '{evt.title}' ({evt.id})");
        }

        private void Update()
        {
            if (!runtimeState.isExecuting || activeEvent == null)
                return;

            if (activeEvent.tree != null && activeEvent.tree.root != null)
            {
                BehaviorTreeStatus treeStatus = ExecuteNode(activeEvent.tree.root);
                if (treeStatus == BehaviorTreeStatus.Running)
                    return;

                if (treeStatus == BehaviorTreeStatus.Failure)
                {
                    if (debugLogging)
                        Debug.LogWarning($"[NarrativeExecutor] Event '{activeEvent.title}' failed in tree.");
                    FinishEvent();
                    return;
                }
            }

            if (activeEvent.actions != null)
            {
                for (int i = 0; i < activeEvent.actions.Count; i++)
                {
                    var a = activeEvent.actions[i];
                    if (a == null) continue;
                    BehaviorTreeStatus s = a.Execute(ctx, runtimeState);
                    if (s == BehaviorTreeStatus.Running)
                        return;
                    if (s == BehaviorTreeStatus.Failure)
                    {
                        if (debugLogging)
                            Debug.LogWarning($"[NarrativeExecutor] Event '{activeEvent.title}' failed in action {i}.");
                        FinishEvent();
                        return;
                    }
                }
            }

            FinishEvent();
        }

        private void FinishEvent()
        {
            float finishTime = clock != null ? NarrativeCalendarMath.DateTimeToSeconds(clock.Now) : 0f;
            if (activeEvent != null && !string.IsNullOrWhiteSpace(activeEvent.id))
            {
                if (!runtimeState.triggeredEventIds.Contains(activeEvent.id))
                    runtimeState.triggeredEventIds.Add(activeEvent.id);
                for (int i = 0; i < runtimeState.executionLedger.Count; i++)
                {
                    if (runtimeState.executionLedger[i].eventId == activeEvent.id && runtimeState.executionLedger[i].finishTime <= 0f)
                        runtimeState.executionLedger[i].finishTime = finishTime;
                }
            }

            if (debugLogging && activeEvent != null)
                Debug.Log($"[NarrativeExecutor] Finished event '{activeEvent.title}' ({activeEvent.id})");

            activeEvent = null;
            runtimeState.activeEventId = null;
            runtimeState.isExecuting = false;
            runtimeState.nodeStack.Clear();
            runtimeState.childIndexStack.Clear();
            _capturedThisEvent.Clear();
        }

        private BehaviorTreeStatus ExecuteNode(NarrativeNode node)
        {
            if (node == null)
                return BehaviorTreeStatus.Success;

            if (!node.contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            CaptureBeforeExecIfNeeded(node);

            switch (node.NodeType)
            {
                case NarrativeNodeType.Action:
                {
                    var an = node as NarrativeActionNode;
                    if (an?.action == null)
                        return BehaviorTreeStatus.Success;
                    var status = an.action.Execute(ctx, runtimeState);
                    if (status != BehaviorTreeStatus.Failure)
                        PushActionLedgerEntry(an);
                    return status;
                }

                case NarrativeNodeType.Sequence:
                {
                    var seq = node as NarrativeSequenceNode;
                    if (seq == null || seq.children == null || seq.children.Count == 0)
                        return BehaviorTreeStatus.Success;

                    runtimeState.nodeStack.Add(node.id);
                    runtimeState.childIndexStack.Add(0);
                    for (int i = 0; i < seq.children.Count; i++)
                    {
                        runtimeState.childIndexStack[runtimeState.childIndexStack.Count - 1] = i;
                        BehaviorTreeStatus s = ExecuteNode(seq.children[i]);
                        if (s == BehaviorTreeStatus.Running || s == BehaviorTreeStatus.Failure)
                            return s;
                    }
                    runtimeState.nodeStack.RemoveAt(runtimeState.nodeStack.Count - 1);
                    runtimeState.childIndexStack.RemoveAt(runtimeState.childIndexStack.Count - 1);
                    return BehaviorTreeStatus.Success;
                }

                case NarrativeNodeType.Selector:
                {
                    var sel = node as NarrativeSelectorNode;
                    if (sel == null || sel.children == null || sel.children.Count == 0)
                        return BehaviorTreeStatus.Failure;

                    runtimeState.nodeStack.Add(node.id);
                    runtimeState.childIndexStack.Add(0);
                    for (int i = 0; i < sel.children.Count; i++)
                    {
                        runtimeState.childIndexStack[runtimeState.childIndexStack.Count - 1] = i;
                        BehaviorTreeStatus s = ExecuteNode(sel.children[i]);
                        if (s == BehaviorTreeStatus.Running)
                            return s;
                        if (s == BehaviorTreeStatus.Success)
                        {
                            runtimeState.nodeStack.RemoveAt(runtimeState.nodeStack.Count - 1);
                            runtimeState.childIndexStack.RemoveAt(runtimeState.childIndexStack.Count - 1);
                            return BehaviorTreeStatus.Success;
                        }
                    }
                    runtimeState.nodeStack.RemoveAt(runtimeState.nodeStack.Count - 1);
                    runtimeState.childIndexStack.RemoveAt(runtimeState.childIndexStack.Count - 1);
                    return BehaviorTreeStatus.Failure;
                }
            }

            return BehaviorTreeStatus.Success;
        }

        void CaptureBeforeExecIfNeeded(NarrativeNode node)
        {
            if (node == null || !node.captureStateBeforeExec || activeEvent == null)
                return;
            string captureKey = activeEvent.id + ":" + node.id;
            if (_capturedThisEvent.Contains(captureKey))
                return;
            _capturedThisEvent.Add(captureKey);

            var objects = ResolveAssociatedObjects(node);
            float narrativeTime = clock != null ? NarrativeCalendarMath.DateTimeToSeconds(clock.Now) : 0f;
            var store = NarrativeNodeExecStateStore.Instance;
            string storeKey = store != null
                ? store.Capture(activeEvent.id, node.id, objects, narrativeTime)
                : captureKey;

            runtimeState.executionLedger.Add(new NarrativeExecutionLedgerEntry
            {
                time = narrativeTime,
                eventId = activeEvent.id,
                nodeId = node.id,
                storeKey = storeKey,
                actionTypeName = node is NarrativeActionNode an && an.action != null ? an.action.GetType().Name : node.NodeType.ToString()
            });
        }

        List<GameObject> ResolveAssociatedObjects(NarrativeNode node)
        {
            var list = new List<GameObject>();
            if (node.associatedBindingKeys != null && bindings != null)
            {
                for (int i = 0; i < node.associatedBindingKeys.Length; i++)
                {
                    if (bindings.TryResolveGameObject(node.associatedBindingKeys[i], out var go) && go != null)
                        list.Add(go);
                }
            }
            if (list.Count == 0 && node is NarrativeActionNode actionNode && actionNode.action != null)
                InferBindingKeys(actionNode.action, list);
            return list;
        }

        void InferBindingKeys(NarrativeActionSpec action, List<GameObject> list)
        {
            if (action is RunBehaviorTreeAction rbt && !string.IsNullOrEmpty(rbt.actorKey))
                TryAddKey(list, rbt.actorKey);
            else if (action is SpawnPrefabAction spa && !string.IsNullOrEmpty(spa.parentKey))
                TryAddKey(list, spa.parentKey);
            else if (action is SetPropertyAction setProp && !string.IsNullOrEmpty(setProp.targetKey))
                TryAddKey(list, setProp.targetKey);
        }

        void TryAddKey(List<GameObject> list, string key)
        {
            if (bindings != null && bindings.TryResolveGameObject(key, out var go) && go != null)
                list.Add(go);
        }

        void PushActionLedgerEntry(NarrativeActionNode node)
        {
            if (node?.action == null || activeEvent == null)
                return;
            if (!node.action.SupportsUndo && !node.captureStateBeforeExec)
                return;
            float narrativeTime = clock != null ? NarrativeCalendarMath.DateTimeToSeconds(clock.Now) : 0f;
            runtimeState.executionLedger.Add(new NarrativeExecutionLedgerEntry
            {
                time = narrativeTime,
                eventId = activeEvent.id,
                nodeId = node.id,
                actionTypeName = node.action.GetType().Name
            });
        }

        public bool TryUndoLedgerEntry(NarrativeExecutionLedgerEntry entry, NarrativeExecutionContext undoCtx)
        {
            if (entry == null || undoCtx == null)
                return false;
            var evt = FindEventById(entry.eventId);
            var node = evt?.tree?.root != null ? FindNodeById(evt.tree.root, entry.nodeId) : null;
            if (node is NarrativeActionNode actionNode && actionNode.action != null && actionNode.action.SupportsUndo)
            {
                actionNode.action.Undo(undoCtx, runtimeState);
                return true;
            }
            if (node != null && node.restoreOnRewind && !string.IsNullOrEmpty(entry.storeKey))
                return NarrativeNodeExecStateStore.Instance != null && NarrativeNodeExecStateStore.Instance.Restore(entry.storeKey);
            if (!string.IsNullOrEmpty(entry.storeKey))
                return NarrativeNodeExecStateStore.Instance != null && NarrativeNodeExecStateStore.Instance.Restore(entry.storeKey);
            return false;
        }

        NarrativeCalendarEvent FindEventById(string eventId)
        {
            if (string.IsNullOrEmpty(eventId) || scheduler?.calendar == null)
                return null;
            var events = scheduler.calendar.events;
            if (events == null)
                return null;
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] != null && events[i].id == eventId)
                    return events[i];
            }
            return null;
        }

        static NarrativeNode FindNodeById(NarrativeNode node, string nodeId)
        {
            if (node == null || string.IsNullOrEmpty(nodeId))
                return null;
            if (node.id == nodeId)
                return node;
            if (node is NarrativeSequenceNode seq && seq.children != null)
            {
                for (int i = 0; i < seq.children.Count; i++)
                {
                    var found = FindNodeById(seq.children[i], nodeId);
                    if (found != null)
                        return found;
                }
            }
            if (node is NarrativeSelectorNode sel && sel.children != null)
            {
                for (int i = 0; i < sel.children.Count; i++)
                {
                    var found = FindNodeById(sel.children[i], nodeId);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }
    }
}
