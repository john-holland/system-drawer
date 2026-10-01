using UnityEngine;

/// <summary>Actor vote card. Refs ballot UI host, BallotSpec, and demographic slice.</summary>
[System.Serializable]
public class VoterCard : GoodSection
{
    public GameObject ballotUiHost;
    public BallotSpec ballot;
    public string demographicSliceId;
    public VotingPlaceCard place;
    public string chosenOptionId;
    public bool hasChosen;

    public VoterCard()
    {
        isVoteGoal = true;
        isCivilGoal = true;
        sectionName = "voter";
        physicalPathingTag = "vote";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "vote";
    }

    public bool BlockedByDeveloperInpaint() =>
        place != null && place.developerInpaint;

    public static VoterCard GenerateDefault(GameObject target = null)
    {
        return new VoterCard { ballotUiHost = target };
    }
}
