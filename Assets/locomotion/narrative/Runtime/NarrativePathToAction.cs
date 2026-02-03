using System;
using UnityEngine;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Action for pathfinding to a target position.
    /// </summary>
    [Serializable]
    public class NarrativePathToAction : NarrativeActionSpec
    {
        [Tooltip("Key resolved via NarrativeBindings for the target GameObject")]
        public string targetKey = "target";

        [Tooltip("Target position (world space, used if targetKey is empty)")]
        public Vector3 targetPosition = Vector3.zero;

        [Tooltip("Key resolved via NarrativeBindings for the pathfinding agent GameObject")]
        public string pathfindingAgentKey = "agent";

        [Tooltip("Tolerance for considering path complete")]
        public float arrivalTolerance = 0.5f;

        [Tooltip("Maximum pathfinding distance")]
        public float maxPathDistance = 50f;

        [Tooltip("Use flying pathfinding (no slope blocking, Y interpolated between start and goal)")]
        public bool useFlyingPathfinding = false;

        [NonSerialized]
        private bool pathfindingStarted = false;

        [NonSerialized]
        private Vector3 currentTarget = Vector3.zero;

        public override BehaviorTreeStatus Execute(NarrativeExecutionContext ctx, NarrativeRuntimeState state)
        {
            if (!contingency.Evaluate(ctx))
                return BehaviorTreeStatus.Success;

            // Resolve target position
            Vector3 target = targetPosition;
            if (!string.IsNullOrEmpty(targetKey) && ctx.TryResolveGameObject(targetKey, out var targetGo))
            {
                target = targetGo.transform.position;
            }

            if (target == Vector3.zero)
            {
                Debug.LogWarning("[NarrativePathToAction] No valid target position");
                return BehaviorTreeStatus.Failure;
            }

            // Resolve pathfinding agent
            if (!ctx.TryResolveGameObject(pathfindingAgentKey, out var agentGo) || agentGo == null)
            {
                Debug.LogWarning("[NarrativePathToAction] Could not resolve pathfinding agent");
                return BehaviorTreeStatus.Failure;
            }

            if (!pathfindingStarted)
            {
                currentTarget = target;
                if (StartPathfinding(agentGo, target))
                {
                    pathfindingStarted = true;
                }
                else
                {
                    return BehaviorTreeStatus.Failure;
                }
            }

            // Check if arrived
            float distance = Vector3.Distance(agentGo.transform.position, currentTarget);
            if (distance <= arrivalTolerance)
            {
                pathfindingStarted = false;
                return BehaviorTreeStatus.Success;
            }

            // Check if pathfinding is still active
            // This would check with the pathfinding system
            return BehaviorTreeStatus.Running;
        }

        private bool StartPathfinding(GameObject agent, Vector3 target)
        {
            // Use HierarchicalPathFinding if available
            var pathfindingType = System.Type.GetType("HierarchicalPathingSolver, HierarchicalPathFinding");
            if (pathfindingType == null)
            {
                pathfindingType = System.Type.GetType("HierarchicalPathingSolver, Assembly-CSharp");
            }

            if (pathfindingType != null)
            {
                var pathfindingSolver = UnityEngine.Object.FindAnyObjectByType(pathfindingType);
                if (pathfindingSolver != null)
                {
                    object savedMode = null;
                    var pathingModeProp = pathfindingType.GetProperty("pathingMode");
                    if (useFlyingPathfinding && pathingModeProp != null)
                    {
                        savedMode = pathingModeProp.GetValue(pathfindingSolver);
                        var pathingModeEnum = pathfindingType.Assembly.GetType("PathingMode");
                        if (pathingModeEnum != null)
                        {
                            object flyMode = System.Enum.Parse(pathingModeEnum, "Fly");
                            pathingModeProp.SetValue(pathfindingSolver, flyMode);
                        }
                    }

                    try
                    {
                        var findPathMethod = pathfindingType.GetMethod("FindPath");
                        if (findPathMethod != null)
                        {
                            object path = findPathMethod.Invoke(pathfindingSolver, new object[] { agent.transform.position, target });
                            if (path != null)
                            {
                                return true;
                            }
                        }
                    }
                    finally
                    {
                        if (savedMode != null && pathingModeProp != null)
                            pathingModeProp.SetValue(pathfindingSolver, savedMode);
                    }
                }
            }

            // Fallback: use Unity NavMesh if available
            UnityEngine.AI.NavMeshAgent navAgent = agent.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.SetDestination(target);
                return true;
            }

            // Fallback: simple movement
            // This would be handled by a movement system
            return true;
        }
    }
}
