using UnityEngine;
using System;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Executes narrative trees and/or action queues produced by the scheduler.
    /// MVP: single-threaded, one active event at a time.
    /// </summary>
    public class NarrativeExecutor : MonoBehaviour
    {
        [Header("Context")]
        public NarrativeClock clock;
        public NarrativeBindings bindings;
        [Tooltip("WeatherSystem GameObject (uses reflection to avoid compile-time dependency)")]
        public GameObject weatherSystemObject;

        [Header("Debug")]
        public bool debugLogging = false;

        [SerializeField] private NarrativeRuntimeState runtimeState = new NarrativeRuntimeState();

        private NarrativeExecutionContext ctx;
        private NarrativeCalendarEvent activeEvent;
        private object weatherSystemComponent; // WeatherSystem via reflection
        private Type weatherSystemType;

        private void Awake()
        {
            if (clock == null) clock = FindObjectOfType<NarrativeClock>();
            if (bindings == null) bindings = FindObjectOfType<NarrativeBindings>();
            
            // Use reflection to find WeatherSystem
            weatherSystemType = Type.GetType("Weather.WeatherSystem, Weather.Runtime");
            if (weatherSystemType != null)
            {
                if (weatherSystemObject == null)
                {
                    MonoBehaviour[] allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
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

        public void StartEvent(NarrativeCalendarEvent evt)
        {
            if (evt == null) return;

            activeEvent = evt;
            runtimeState.activeEventId = evt.id;
            runtimeState.isExecuting = true;
            runtimeState.nodeStack.Clear();
            runtimeState.childIndexStack.Clear();

            if (debugLogging)
                Debug.Log($"[NarrativeExecutor] Start event '{evt.title}' ({evt.id})");
        }

        private void Update()
        {
            if (!runtimeState.isExecuting || activeEvent == null)
                return;

            // Execute tree (if present)
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

            // Execute direct actions (if any)
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
            if (activeEvent != null && !string.IsNullOrWhiteSpace(activeEvent.id))
            {
                if (!runtimeState.triggeredEventIds.Contains(activeEvent.id))
                    runtimeState.triggeredEventIds.Add(activeEvent.id);
            }

            if (debugLogging && activeEvent != null)
                Debug.Log($"[NarrativeExecutor] Finished event '{activeEvent.title}' ({activeEvent.id})");

            activeEvent = null;
            runtimeState.activeEventId = null;
            runtimeState.isExecuting = false;
            runtimeState.nodeStack.Clear();
            runtimeState.childIndexStack.Clear();
        }

        private BehaviorTreeStatus ExecuteNode(NarrativeNode node)
        {
            if (node == null)
                return BehaviorTreeStatus.Success;

            if (!node.contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            switch (node.NodeType)
            {
                case NarrativeNodeType.Action:
                {
                    var an = node as NarrativeActionNode;
                    if (an?.action == null)
                        return BehaviorTreeStatus.Success;
                    return an.action.Execute(ctx, runtimeState);
                }

                case NarrativeNodeType.Sequence:
                {
                    var seq = node as NarrativeSequenceNode;
                    if (seq == null || seq.children == null || seq.children.Count == 0)
                        return BehaviorTreeStatus.Success;

                    for (int i = 0; i < seq.children.Count; i++)
                    {
                        BehaviorTreeStatus s = ExecuteNode(seq.children[i]);
                        if (s == BehaviorTreeStatus.Running || s == BehaviorTreeStatus.Failure)
                            return s;
                    }
                    return BehaviorTreeStatus.Success;
                }

                case NarrativeNodeType.Selector:
                {
                    var sel = node as NarrativeSelectorNode;
                    if (sel == null || sel.children == null || sel.children.Count == 0)
                        return BehaviorTreeStatus.Failure;

                    for (int i = 0; i < sel.children.Count; i++)
                    {
                        BehaviorTreeStatus s = ExecuteNode(sel.children[i]);
                        if (s == BehaviorTreeStatus.Running)
                            return s;
                        if (s == BehaviorTreeStatus.Success)
                            return BehaviorTreeStatus.Success;
                    }
                    return BehaviorTreeStatus.Failure;
                }
            }

            return BehaviorTreeStatus.Success;
        }
    }
}

