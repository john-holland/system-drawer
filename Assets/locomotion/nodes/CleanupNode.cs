using UnityEngine;

/// <summary>
/// Behavior tree node for cleanup tasks.
/// Processes cleanup goals from the nervous system.
/// </summary>
public class CleanupNode : BehaviorTreeNode
{
    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        // Process all pending cleanup goals
        NervousSystem nervousSystem = tree.GetComponent<NervousSystem>();
        if (nervousSystem != null)
        {
            nervousSystem.ProcessCleanupGoals();
            
            // Check if cleanup is complete
            bool cleanupComplete = nervousSystem.cleanupStack.Count == 0;
            
            return cleanupComplete ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Running;
        }

        return BehaviorTreeStatus.Failure;
    }
}
