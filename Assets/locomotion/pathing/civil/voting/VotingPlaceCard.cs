using UnityEngine;

/// <summary>Polling-place perimeter Justice card. Enqueues voters onto <see cref="LaneGrid"/>.</summary>
[System.Serializable]
public class VotingPlaceCard : JusticeCard
{
    public LaneGrid laneGrid;
    public VotingQueueHub hub;
    public bool developerInpaint;
    public VotingPlaceBioRhythm bioRhythm;

    public VotingPlaceCard()
    {
        justiceAction = JusticeAction.SecureArea;
        violenceThreshold01 = 0.6f;
        sectionName = "voting_place";
        physicalPathingTag = "justice_voting_place";
        isJusticeGoal = true;
    }

    public bool EnqueueVoter(BaseAmbulatingActor actor)
    {
        if (developerInpaint) return false;
        var ingress = hub != null ? hub.CentralQueue : laneGrid;
        if (ingress == null) return false;
        return ingress.TryEnqueue(actor);
    }

    public static VotingPlaceCard Generate(LaneGrid grid, GameObject perimeter = null, RagdollState state = null)
    {
        var c = new VotingPlaceCard
        {
            laneGrid = grid,
            hub = grid != null ? grid.GetComponent<VotingQueueHub>() : null,
            hazardTarget = perimeter,
            requiredState = state?.CopyState(),
            targetState = state?.CopyState()
        };
        return c;
    }
}
