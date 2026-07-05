using UnityEngine;

namespace Locomotion.Open.Nodes
{
    /// <summary>Unlocks an OpenableLatch before child open runs.</summary>
    public sealed class UnlockLatchNode : BehaviorTreeNode
    {
        public OpenableLatch latch;
        public string toolLemma;

        void Awake() => nodeType = NodeType.Action;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (latch == null)
                return BehaviorTreeStatus.Success;
            return latch.TryUnlock(toolLemma) ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        }
    }
}
