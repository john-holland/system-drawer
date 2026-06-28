using UnityEngine;

namespace DestructibleEnvironment
{
    public class DestructibleFallSequenceNode : BehaviorTreeNode
    {
        public DestructiblePlaybackController playback;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (children == null || children.Count == 0)
                return BehaviorTreeStatus.Success;

            for (int i = 0; i < children.Count; i++)
            {
                BehaviorTreeNode child = children[i];
                if (child == null)
                    continue;
                if (!child.Predicate(tree))
                    continue;

                BehaviorTreeStatus status = child.Execute(tree);
                if (status == BehaviorTreeStatus.Failure)
                    return BehaviorTreeStatus.Failure;
                if (status == BehaviorTreeStatus.Running)
                    return BehaviorTreeStatus.Running;
            }

            return BehaviorTreeStatus.Success;
        }
    }
}
