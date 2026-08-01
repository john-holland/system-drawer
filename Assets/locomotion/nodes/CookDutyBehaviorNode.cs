using UnityEngine;

/// <summary>BehaviorTreeNode wrapping CookDutyNode / ChefActivitySolvers for GoalType.Cooking.</summary>
[AddComponentMenu("Locomotion/Kitchen/Cook Duty Behavior Node")]
public sealed class CookDutyBehaviorNode : BehaviorTreeNode
{
    public CookDutyNode cookDuty;
    public ChefCard boundCard;
    public BehaviorTreeGoal goal;
    bool _started;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (cookDuty == null)
            cookDuty = GetComponent<CookDutyNode>() ?? gameObject.AddComponent<CookDutyNode>();
        if (boundCard != null)
            cookDuty.boundCard = boundCard;
        if (goal == null)
            goal = new BehaviorTreeGoal { type = GoalType.Cooking, goalName = "cook_duty" };

        if (!_started)
        {
            if (!cookDuty.Begin(goal))
            {
                status = BehaviorTreeStatus.Failure;
                return status;
            }
            _started = true;
            status = BehaviorTreeStatus.Running;
        }

        bool cont = cookDuty.Tick(Time.deltaTime);
        if (!cont)
        {
            _started = false;
            status = BehaviorTreeStatus.Success;
            return status;
        }
        status = BehaviorTreeStatus.Running;
        return status;
    }
}
