using System.Collections.Generic;
using Locomotion.Narrative;
using UnityEngine;

/// <summary>High-priority vote action. Causality gates must pass unless the place is developer in-paint.</summary>
[AddComponentMenu("Locomotion/Civil/Vote Behavior Tree Node")]
public sealed class VoteBehaviorTreeNode : BehaviorTreeNode
{
    public VoterCard voter = new VoterCard();
    public VoteLedger ledger;
    public BallotSpec ballot;
    public ComputerPeripheryStation webtop;
    public NarrativeExecutor executor;
    public ElectorateDemographics demographics;
    public string gameSessionId;
    public string lastCastOptionId;
    public List<string> ranking = new List<string>();
    public bool lastAccountingOk = true;

    void Awake()
    {
        nodeType = NodeType.Action;
        if (ledger == null) ledger = GetComponent<VoteLedger>();
        if (executor == null) executor = GetComponent<NarrativeExecutor>();
        if (webtop == null) webtop = GetComponent<ComputerPeripheryStation>();
    }

    public override bool Predicate(BehaviorTree tree)
    {
        if (voter != null && voter.BlockedByDeveloperInpaint())
            return false;
        var spec = ballot != null ? ballot : voter != null ? voter.ballot : null;
        if (spec == null || spec.causalityGates == null || spec.causalityGates.Count == 0)
            return true;
        for (int i = 0; i < spec.causalityGates.Count; i++)
        {
            var g = spec.causalityGates[i];
            if (g != null && !g.Evaluate(executor, webtop))
                return false;
        }
        return true;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (!Predicate(tree))
            return BehaviorTreeStatus.Running;
        if (ledger == null) ledger = GetComponent<VoteLedger>() ?? gameObject.AddComponent<VoteLedger>();
        var spec = ballot != null ? ballot : voter != null ? voter.ballot : null;
        if (spec == null)
            return BehaviorTreeStatus.Failure;
        BallotGovFold.EnsureKindDefaults(spec);
        var errors = BallotGovFold.ErrorsFor(spec.kind, ResolveGovMix(), spec.tallyMethod);
        if (errors.Count > 0)
        {
            Debug.LogError("[VoteBehaviorTreeNode] " + string.Join("; ", errors));
            status = BehaviorTreeStatus.Failure;
            return status;
        }
        var run = ledger.StartRun(gameSessionId ?? "", spec);
        string option = ResolveOption(spec, tree);
        lastCastOptionId = option;
        string actorId = tree != null ? tree.gameObject.name : gameObject.name;
        string slice = voter != null ? voter.demographicSliceId : "";
        var rec = ledger.Cast(run, actorId, option, slice, false, ResolveRanking(spec, option));
        if (voter != null)
        {
            voter.hasChosen = true;
            voter.chosenOptionId = option;
        }
        lastAccountingOk = rec != null;
        status = lastAccountingOk ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        return status;
    }

    string ResolveOption(BallotSpec spec, BehaviorTree tree)
    {
        if (voter != null && voter.hasChosen && !string.IsNullOrEmpty(voter.chosenOptionId))
            return voter.chosenOptionId;
        ElectorateSlice slice = null;
        ElectorateDemographics demo = demographics != null ? demographics : (spec.demographics != null ? spec.demographics : ledger != null ? ledger.followOnDemographics : null);
        if (demo != null)
        {
            int seed = (tree != null ? tree.gameObject.GetInstanceID() : GetInstanceID()) ^ spec.ballotId.GetHashCode();
            slice = demo.Sample(seed);
            if (voter != null && slice != null)
                voter.demographicSliceId = slice.sliceId;
            if (spec.kind != BallotKind.Candidate)
                return demo.TiltYesNo(slice, false, null, seed + 11);
        }
        if (spec.options != null && spec.options.Count > 0 && spec.options[0] != null)
            return spec.options[0].optionId;
        return "yes";
    }

    List<string> ResolveRanking(BallotSpec spec, string first)
    {
        var ranking = RankedTally.Clean(this.ranking);
        if (ranking.Count == 0 && voter != null && voter.hasChosen && !string.IsNullOrEmpty(voter.chosenOptionId))
            ranking.Add(voter.chosenOptionId);
        else if (ranking.Count == 0 && !string.IsNullOrEmpty(first))
            ranking.Add(first);
        if (spec == null || !spec.IsRanked || spec.options == null)
            return ranking;
        for (int i = 0; i < spec.options.Count; i++)
        {
            var opt = spec.options[i];
            if (opt == null || string.IsNullOrEmpty(opt.optionId)) continue;
            if (!ranking.Contains(opt.optionId))
                ranking.Add(opt.optionId);
        }
        return ranking;
    }

    GovernmentFlavorMix ResolveGovMix()
    {
        var ragdoll = GetComponent<GovernmentModelRagdoll>();
        if (ragdoll != null && ragdoll.mix != null)
            return ragdoll.mix;
        var bio = GetComponent<GovernmentModelBioRhythm>();
        return bio != null ? bio.mix : null;
    }
}
