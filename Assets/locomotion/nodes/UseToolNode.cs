using UnityEngine;

/// <summary>
/// Behavior tree node for tool usage.
/// Handles tool discovery, evaluation, usage, and cleanup.
/// </summary>
public class UseToolNode : BehaviorTreeNode
{
    [Header("Tool Properties")]
    [Tooltip("Tool to use (if null, will find tool based on task)")]
    public GameObject tool;

    [Tooltip("Task to perform with tool")]
    public string task = "default_task";

    [Tooltip("Should we cleanup (return tool) after use?")]
    public bool cleanupAfterUse = true;

    [Tooltip("Cleanup urgency level")]
    public CleanupUrgency cleanupUrgency = CleanupUrgency.AfterTask;

    private bool toolUsed = false;
    private bool cleanupScheduled = false;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        // Check if tool is available
        if (!ToolAvailable(tree))
        {
            return BehaviorTreeStatus.Failure;
        }

        // Get tool usage cards from Consider component
        Consider consider = tree.GetComponent<Consider>();
        if (consider == null)
        {
            return BehaviorTreeStatus.Failure;
        }

        List<GoodSection> cards = consider.GenerateToolUsageCards(tool, task, tree.GetComponent<RagdollSystem>()?.GetCurrentState() ?? new RagdollState());

        if (cards == null || cards.Count == 0)
        {
            return BehaviorTreeStatus.Failure;
        }

        // Execute tool usage sequence (simplified: assume success)
        toolUsed = ExecuteCardSequence(cards, tree);

        // If cleanup enabled, add cleanup goal
        if (toolUsed && cleanupAfterUse && !cleanupScheduled)
        {
            NervousSystem nervousSystem = tree.GetComponent<NervousSystem>();
            if (nervousSystem != null)
            {
                BehaviorTreeGoal cleanupGoal = nervousSystem.GenerateCleanupGoal(tool, cleanupUrgency);
                if (cleanupGoal != null)
                {
                    nervousSystem.AddCleanupGoal(cleanupGoal);
                    cleanupScheduled = true;
                }
            }
        }

        return toolUsed ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
    }

    private bool ToolAvailable(BehaviorTree tree)
    {
        if (tool != null)
        {
            return tool.activeInHierarchy;
        }

        // Find tool based on task
        Consider consider = tree.GetComponent<Consider>();
        if (consider != null)
        {
            var tools = consider.ScanForTools(10f, tree.currentGoal);
            if (tools != null && tools.Count > 0)
            {
                tool = tools[0].gameObject;
                return true;
            }
        }

        return false;
    }

    private bool ExecuteCardSequence(List<GoodSection> cards, BehaviorTree tree)
    {
        // Simplified: execute cards in sequence
        // In practice, this would be handled by the card solver and nervous system
        return cards != null && cards.Count > 0;
    }
}
