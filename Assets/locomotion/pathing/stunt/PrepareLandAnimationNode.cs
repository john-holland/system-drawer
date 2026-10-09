using UnityEngine;

public sealed class PrepareLandAnimationNode : TravelContextBehaviorTreeNode
{
    public MultiModalSegment segment;
    public Vector3 landingGoalOverride;
    public bool useLandingGoalOverride;
    public float landDurationSeconds = 1.2f;

    void Awake() { nodeType = NodeType.Action; }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        string tag = segment != null ? segment.animationGroupTag : null;
        if (!ParkourLandAnimationDriver.IsLandingTag(tag))
            return BehaviorTreeStatus.Success;
        Vector3 goal = useLandingGoalOverride ? landingGoalOverride : (segment != null ? segment.segmentEnd : landingGoalOverride);
        GameObject host = tree != null ? tree.gameObject : gameObject;
        var driver = ParkourLandAnimationDriver.FindOrCreate(host);
        var prep = new LandAnimationPrep();
        driver.PlayLanding(tag, goal, prep, landDurationSeconds);
        if (tree != null)
            tree.currentGoal = prep.BuildGoalAt(goal);
        return BehaviorTreeStatus.Success;
    }
}
