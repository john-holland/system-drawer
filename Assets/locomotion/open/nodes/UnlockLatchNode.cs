using UnityEngine;

namespace Locomotion.Open.Nodes
{
    /// <summary>Unlocks an OpenableLatch before child open runs.</summary>
    public sealed class UnlockLatchNode : BehaviorTreeNode
    {
        public OpenableLatch latch;
        public string toolLemma;
        public string topologyNodeId;
        public OpenCloseBeatProfile profile;

        void Awake() => nodeType = NodeType.Action;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (latch == null)
                return BehaviorTreeStatus.Success;
            if (!latch.TryUnlock(toolLemma))
                return BehaviorTreeStatus.Failure;
            OpenCloseBeatMessageBus.RaiseUnlock(
                !string.IsNullOrEmpty(topologyNodeId) ? topologyNodeId : latch.name,
                profile,
                latch.transform.position);
            return BehaviorTreeStatus.Success;
        }
    }
}
