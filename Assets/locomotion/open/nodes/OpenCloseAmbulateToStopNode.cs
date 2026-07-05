using System.Collections.Generic;
using Locomotion.Open.Nodes;
using UnityEngine;

namespace Locomotion.Open.Nodes
{
    /// <summary>Ambulates to approach anchor; gates child open per arrival blend coefficient.</summary>
    public sealed class OpenCloseAmbulateToStopNode : BehaviorTreeNode
    {
        [Header("Stop")]
        public Vector3 approachAnchor;
        public Transform handlePoint;
        public float arrivalBlendCoefficient;
        public float reachRadiusMeters = 0.6f;
        public float reachedDistance = 0.5f;
        public bool requireFacingTarget = true;

        [Header("Pathing")]
        public HierarchicalPathingSolver pathfindingSolver;
        public float waypointReachedDistance = 0.5f;

        PathfindingNode _pathNode;
        int _childIndex;
        bool _pathBuilt;
        bool _ambulationDone;
        Rigidbody _body;

        void Awake() => nodeType = NodeType.Sequence;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (tree == null)
                return BehaviorTreeStatus.Failure;

            _body ??= tree.GetComponent<Rigidbody>();
            if (pathfindingSolver == null)
                pathfindingSolver = Object.FindAnyObjectByType<HierarchicalPathingSolver>();

            if (!_pathBuilt)
                BuildPathChildren(tree);

            if (!_ambulationDone && _pathNode != null)
            {
                var pathStatus = _pathNode.Execute(tree);
                if (pathStatus == BehaviorTreeStatus.Running)
                    return TryOpenWhileMoving(tree);
                if (pathStatus == BehaviorTreeStatus.Failure)
                    return BehaviorTreeStatus.Failure;
                _ambulationDone = true;
            }

            return TryOpenGate(tree);
        }

        void BuildPathChildren(BehaviorTree tree)
        {
            _pathBuilt = true;
            _pathNode = gameObject.GetComponent<PathfindingNode>();
            if (_pathNode == null)
                _pathNode = gameObject.AddComponent<PathfindingNode>();

            _pathNode.origin = tree.transform.position;
            _pathNode.destination = approachAnchor;
            _pathNode.pathfindingSolver = pathfindingSolver;
            _pathNode.waypointReachedDistance = waypointReachedDistance;
            _pathNode.nodeType = NodeType.Sequence;
        }

        BehaviorTreeStatus TryOpenWhileMoving(BehaviorTree tree)
        {
            if (arrivalBlendCoefficient <= 0f)
                return BehaviorTreeStatus.Running;
            if (!ShouldAttemptOpen(tree))
                return BehaviorTreeStatus.Running;
            return ExecuteOpenChild(tree);
        }

        BehaviorTreeStatus TryOpenGate(BehaviorTree tree)
        {
            if (!ShouldAttemptOpen(tree))
                return BehaviorTreeStatus.Running;
            return ExecuteOpenChild(tree);
        }

        bool ShouldAttemptOpen(BehaviorTree tree)
        {
            Vector3 actorPos = tree.transform.position;
            Vector3 handle = handlePoint != null ? handlePoint.position : approachAnchor;
            float stop = OpenCloseArrivalGate.ComputeStopProgress(actorPos, approachAnchor, reachedDistance, _body);
            float reach = OpenCloseArrivalGate.ComputeReachProgress(actorPos, handle, reachRadiusMeters);
            bool facing = OpenCloseArrivalGate.IsFacingTarget(tree.transform, handle);
            float gate = OpenCloseArrivalGate.ComputeGate(arrivalBlendCoefficient, stop, reach, requireFacingTarget, facing);
            bool inReach = reach >= 1f;
            return OpenCloseArrivalGate.ShouldAttemptOpen(gate, arrivalBlendCoefficient, inReach);
        }

        BehaviorTreeStatus ExecuteOpenChild(BehaviorTree tree)
        {
            if (children == null || children.Count == 0)
                return BehaviorTreeStatus.Success;

            for (; _childIndex < children.Count; _childIndex++)
            {
                var child = children[_childIndex];
                if (child == null)
                    continue;
                var status = child.Execute(tree);
                if (status == BehaviorTreeStatus.Running)
                    return BehaviorTreeStatus.Running;
                if (status == BehaviorTreeStatus.Failure)
                {
                    if (arrivalBlendCoefficient >= 1f)
                        return BehaviorTreeStatus.Running;
                    return BehaviorTreeStatus.Failure;
                }
            }
            return BehaviorTreeStatus.Success;
        }
    }
}
