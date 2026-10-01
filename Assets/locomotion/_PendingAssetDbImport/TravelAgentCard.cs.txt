using UnityEngine;

/// <summary>Composes JusticeCard + TravelAgent path preview for staff/police BT grafts.</summary>
[System.Serializable]
public class TravelAgentCard : GoodSection
{
    [Header("TravelAgent")]
    public JusticeCard justice;
    public Vector3 goalWorld;
    public GameObject goalTarget;
    public string waypointGroup;
    public bool preferFlee;
    public bool useSocialDeescalate;
    [Header("Lane policy")]
    public TravelLanePolicy lanePolicy = TravelLanePolicy.StayInLanes;
    [Range(0f, 1f)] public float stayInLanes01 = 1f;
    [Min(0.1f)] public float followTimeSec = 3f;
    [Min(0f)] public float gridCarLengths = 1f;

    public TravelAgentCard()
    {
        isTravelAgentGoal = true;
        physicalPathingTag = "travel_agent";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "travel";
    }

    public static TravelAgentCard GenerateDefault(GameObject target)
    {
        return new TravelAgentCard
        {
            sectionName = "travel_agent_default",
            description = "TravelAgent + Justice",
            isTravelAgentGoal = true,
            goalTarget = target,
            justice = JusticeCard.Generate(JusticeAction.SecureArea, target),
            preferFlee = true,
            physicalPathingTag = "travel_agent"
        };
    }

    public static TravelAgentCard GeneratePatrol(Vector3 goal, JusticeAction action = JusticeAction.SecureArea)
    {
        return new TravelAgentCard
        {
            sectionName = "travel_agent_patrol",
            description = "Patrol",
            isTravelAgentGoal = true,
            goalWorld = goal,
            justice = JusticeCard.Generate(action, null),
            preferFlee = false,
            physicalPathingTag = "travel_agent_patrol"
        };
    }

    /// <summary>Apply to actor: flee or justice path via TravelAgent.</summary>
    public virtual void ApplyToActor(GameObject actor, float threat01, SocialSkills social = null)
    {
        if (actor == null) return;
        var ta = actor.GetComponent<TravelAgent>();
        Vector3 goal = goalTarget != null ? goalTarget.transform.position : goalWorld;
        bool flee = preferFlee;
        if (justice != null)
            flee = !justice.ShouldRespondPhysically(actor, threat01);

        if (flee)
        {
            Vector3 away = actor.transform.position + (actor.transform.position - goal).normalized * 8f;
            if (ta != null)
            {
                ApplyLanePolicy(ta);
                ta.previewGoalWorld = away;
                ta.RebuildCachedPlan();
            }
            var sched = actor.GetComponent<PersonalSchedule>();
            sched?.ForceFlee();
            return;
        }

        if (useSocialDeescalate && social != null)
        {
            var r = social.Interpret(SocialRequestChannel.Local, "calm down", goal);
            social.Apply(r);
        }

        if (ta != null)
        {
            ApplyLanePolicy(ta);
            ta.previewGoalWorld = goal;
            ta.RebuildCachedPlan();
        }
    }

    public void ApplyLanePolicy(TravelAgent ta)
    {
        if (ta == null) return;
        ta.lanePolicy = lanePolicy;
        ta.stayInLanes01 = lanePolicy == TravelLanePolicy.StayInLanes ? stayInLanes01 : 0f;
        ta.followTimeSec = followTimeSec;
        ta.gridCarLengths = gridCarLengths;
    }
}
