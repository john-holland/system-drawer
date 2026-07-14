using UnityEngine;

namespace Locomotion.Open
{
    /// <summary>
    /// Compatibility orchestrator for open/close topology beats.
    /// Delegates bake/execute to <see cref="ObjectOpenCloseTopologyPlanNode"/>.
    /// </summary>
    public sealed class OpenCloseSequenceNode : BehaviorTreeNode
    {
        public OpenCloseTopologyAsset topology;
        public OpenCloseLemmaProperties lemmaOverrides;
        public OpenCloseCameraSequence cameraSequence;
        public Transform actor;

        ObjectOpenCloseTopologyPlanNode _plan;

        void Awake() => nodeType = NodeType.Sequence;

        public ObjectOpenCloseTopologyPlanNode Plan => EnsurePlan();

        public void RebuildFromTopology()
        {
            var plan = EnsurePlan();
            SyncPlanFields(plan);
            plan.BakeFromTopology();
            children.Clear();
            children.AddRange(plan.children);
        }

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            var plan = EnsurePlan();
            SyncPlanFields(plan);
            var status = plan.Execute(tree);
            if (children.Count != plan.children.Count)
            {
                children.Clear();
                children.AddRange(plan.children);
            }
            return status;
        }

        ObjectOpenCloseTopologyPlanNode EnsurePlan()
        {
            if (_plan != null)
                return _plan;

            _plan = GetComponent<ObjectOpenCloseTopologyPlanNode>();
            if (_plan == null)
                _plan = gameObject.AddComponent<ObjectOpenCloseTopologyPlanNode>();
            return _plan;
        }

        void SyncPlanFields(ObjectOpenCloseTopologyPlanNode plan)
        {
            plan.topology = topology;
            plan.lemmaOverrides = lemmaOverrides;
            plan.cameraSequence = cameraSequence;
            plan.actor = actor;
            plan.persistBakedSteps = true;
        }
    }
}
