using System;
using System.Collections.Generic;
using UnityEngine;

public enum BallotKind
{
    Question = 0,
    Measure = 1,
    Candidate = 2
}

[Serializable]
public sealed class BallotOption
{
    public string optionId = "yes";
    public string displayName = "Yes";
    public List<VotePropertyAssignment> win = new List<VotePropertyAssignment>();
    public List<VotePropertyAssignment> lose = new List<VotePropertyAssignment>();
}

[CreateAssetMenu(fileName = "BallotSpec", menuName = "Locomotion/Civil/Ballot Spec")]
public sealed class BallotSpec : ScriptableObject
{
    public string ballotId = "ballot";
    public string title = "Ballot";
    public BallotKind kind = BallotKind.Question;
    public BallotTallyMethod tallyMethod = BallotTallyMethod.Plurality;
    public int seats = 1;
    [TextArea] public string prompt;
    public List<BallotOption> options = new List<BallotOption>();
    public List<VoteCausalityGate> causalityGates = new List<VoteCausalityGate>();
    public ElectorateDemographics demographics;

    public bool IsRanked => tallyMethod == BallotTallyMethod.Irv || tallyMethod == BallotTallyMethod.Stv;

    public void EnsureQuestionDefaults()
    {
        if (options != null && options.Count > 0) return;
        options = new List<BallotOption>
        {
            new BallotOption { optionId = "yes", displayName = "Yes" },
            new BallotOption { optionId = "no", displayName = "No" }
        };
    }

    public BallotOption FindOption(string optionId)
    {
        if (options == null || string.IsNullOrEmpty(optionId)) return null;
        for (int i = 0; i < options.Count; i++)
            if (options[i] != null && options[i].optionId == optionId)
                return options[i];
        return null;
    }
}
