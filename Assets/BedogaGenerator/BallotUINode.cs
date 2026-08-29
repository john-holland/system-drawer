using System.Collections.Generic;
using UnityEngine;

/// <summary>SG2D ballot UI: question / measure / candidate rows. Confirm commits to VoteBehaviorTreeNode.</summary>
[AddComponentMenu("Bedoga/Ballot UI Node")]
public sealed class BallotUINode : SGBehaviorTreeNode2D
{
    public BallotSpec ballot;
    public VoteBehaviorTreeNode voteNode;
    public string hoveredOptionId;
    public string confirmedOptionId;
    public List<string> ranking = new List<string>();
    public bool accordionOpen = true;

    public string BallotLabel => "Ballot";

    public string KindListLabel() => BallotGovFold.ListLabel(ballot != null ? ballot.kind : BallotKind.Question);

    public string[] OptionDisplayNames()
    {
        if (ballot == null || ballot.options == null) return System.Array.Empty<string>();
        var names = new string[ballot.options.Count];
        for (int i = 0; i < ballot.options.Count; i++)
        {
            var opt = ballot.options[i];
            names[i] = opt == null ? "" : (string.IsNullOrEmpty(opt.displayName) ? opt.optionId : opt.displayName);
        }
        return names;
    }

    public void Bind(BallotSpec spec, VoteBehaviorTreeNode node)
    {
        ballot = spec;
        voteNode = node;
        if (ballot != null)
            BallotGovFold.EnsureKindDefaults(ballot);
    }

    public bool Hover(string optionId)
    {
        hoveredOptionId = optionId ?? "";
        return true;
    }

    public bool Rank(string optionId)
    {
        if (string.IsNullOrEmpty(optionId)) return false;
        if (ranking == null) ranking = new List<string>();
        ranking.Remove(optionId);
        ranking.Add(optionId);
        return true;
    }

    public bool TryConfirmHovered()
    {
        if (string.IsNullOrEmpty(hoveredOptionId)) return false;
        if (ballot != null && ballot.IsRanked)
            Rank(hoveredOptionId);
        confirmedOptionId = ranking != null && ranking.Count > 0 ? ranking[0] : hoveredOptionId;
        if (voteNode != null)
        {
            if (voteNode.voter == null)
                voteNode.voter = new VoterCard();
            voteNode.voter.hasChosen = true;
            voteNode.voter.chosenOptionId = confirmedOptionId;
            voteNode.ballot = ballot;
            voteNode.ranking = ranking != null ? new List<string>(ranking) : new List<string>();
        }
        return true;
    }

    public string[] OptionIds()
    {
        if (ballot == null || ballot.options == null) return System.Array.Empty<string>();
        var ids = new string[ballot.options.Count];
        for (int i = 0; i < ballot.options.Count; i++)
            ids[i] = ballot.options[i] != null ? ballot.options[i].optionId : "";
        return ids;
    }
}
