using UnityEngine;

[System.Serializable]
public class PrisonGuardCard : TravelAgentCard
{
    public WrestlingCard restraint;
    [Range(0f, 1f)] public float posturing01 = 0.4f;

    public PrisonGuardCard()
    {
        isCivilGoal = true;
        isTravelAgentGoal = true;
        physicalPathingTag = "prison_guard";
        traversabilityTag = "prison_guard";
        sectionName = "prison_guard";
        preferFlee = false;
    }

    public static PrisonGuardCard Generate(Vector3 goal, WrestlingCard restraint = null)
    {
        return new PrisonGuardCard
        {
            goalWorld = goal,
            restraint = restraint ?? new WrestlingCard { sectionName = "prison_restraint" },
            sectionName = "prison_guard",
            justice = JusticeCard.Generate(JusticeAction.SecureArea, null),
            isCivilGoal = true,
            isTravelAgentGoal = true
        };
    }
}

[System.Serializable]
public class PrisonerCard : TravelAgentCard
{
    public PrisonerStatus status = PrisonerStatus.Custody;
    public string cellId;

    public PrisonerCard()
    {
        isCivilGoal = true;
        isTravelAgentGoal = true;
        physicalPathingTag = "prisoner";
        traversabilityTag = "prisoner";
        sectionName = "prisoner";
        preferFlee = false;
    }

    public static PrisonerCard Generate(PrisonerStatus status, Vector3 goal, string cellId = null)
    {
        return new PrisonerCard
        {
            status = status,
            cellId = cellId,
            goalWorld = goal,
            sectionName = $"prisoner_{status}",
            isCivilGoal = true,
            isTravelAgentGoal = true
        };
    }
}

[System.Serializable]
public class PrisonWardenCard : TravelAgentCard
{
    public PrisonWardenCard()
    {
        isCivilGoal = true;
        isTravelAgentGoal = true;
        physicalPathingTag = "prison_warden";
        traversabilityTag = "prison_warden";
        sectionName = "prison_warden";
    }

    public static PrisonWardenCard Generate(Vector3 office)
    {
        return new PrisonWardenCard
        {
            goalWorld = office,
            sectionName = "prison_warden",
            justice = JusticeCard.Generate(JusticeAction.SecureArea, null)
        };
    }
}

[System.Serializable]
public class ParoleBoardCard : JusticeCard
{
    public ParoleBoardCard()
    {
        justiceAction = JusticeAction.SecureArea;
        sectionName = "parole_board";
        physicalPathingTag = "parole_board";
        violenceThreshold01 = 0.9f;
        defaultFleeUnlessTriggered = false;
    }

    public static ParoleBoardCard Generate(GameObject chamber)
    {
        return new ParoleBoardCard
        {
            hazardTarget = chamber,
            sectionName = "parole_board"
        };
    }
}
