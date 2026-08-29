using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Stores vote runs, issued/cast/spoiled counts, and certified property bags.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Vote Ledger")]
public sealed class VoteLedger : MonoBehaviour
{
    public List<VoteRun> runs = new List<VoteRun>();
    public VotePropertyBag certified = new VotePropertyBag();
    public ElectorateDemographics followOnDemographics;
    public int issued;
    public int spoiled;
    public string lastLemmaKey = VoteLemmaPropertyKeys.Vote;

    public int IssuedCount => issued;
    public int CastCount
    {
        get
        {
            int n = 0;
            if (runs == null) return 0;
            for (int i = 0; i < runs.Count; i++)
                if (runs[i] != null && runs[i].casts != null)
                    n += runs[i].casts.Count;
            return n;
        }
    }
    public int SpoiledCount => spoiled;

    public VoteRun StartRun(string gameSessionId, BallotSpec ballot)
    {
        if (runs == null) runs = new List<VoteRun>();
        string ballotId = ballot != null ? ballot.ballotId : "ballot";
        string runId = Guid.NewGuid().ToString("N");
        var run = new VoteRun
        {
            runId = runId,
            gameSessionId = gameSessionId ?? "",
            ballotId = ballotId,
            causalityLeafId = $"vote.{gameSessionId}.{runId}"
        };
        runs.Add(run);
        issued++;
        lastLemmaKey = VoteLemmaPropertyKeys.Ballot;
        return run;
    }

    public VoteCastRecord Cast(VoteRun run, string actorId, string optionId, string sliceId, bool spoiledCast = false, List<string> ranking = null)
    {
        if (run == null) return null;
        if (run.casts == null) run.casts = new List<VoteCastRecord>();
        var cleaned = RankedTally.Clean(ranking);
        string first = optionId ?? "";
        if (string.IsNullOrEmpty(first) && cleaned.Count > 0)
            first = cleaned[0];
        if (cleaned.Count == 0 && !string.IsNullOrEmpty(first))
            cleaned.Add(first);
        var rec = new VoteCastRecord
        {
            actorId = actorId ?? "",
            ballotId = run.ballotId,
            optionId = first,
            ranking = cleaned,
            demographicSliceId = sliceId ?? "",
            causalityLeafId = $"{run.causalityLeafId}.{actorId}",
            spoiled = spoiledCast
        };
        run.casts.Add(rec);
        if (spoiledCast) spoiled++;
        run.Tally();
        lastLemmaKey = VoteLemmaPropertyKeys.Tally;
        return rec;
    }

    public VoteRun Recount(VoteRun run)
    {
        if (run == null) return null;
        var copy = run.CloneForRecount();
        if (runs == null) runs = new List<VoteRun>();
        runs.Add(copy);
        lastLemmaKey = VoteLemmaPropertyKeys.Recount;
        return copy;
    }

    public void Certify(VoteRun run, BallotSpec ballot)
    {
        if (run == null) return;
        run.Tally();
        run.certified = true;
        run.result.certified = true;
        if (ballot == null || ballot.options == null) return;
        var winners = new HashSet<string>();
        if (ballot.IsRanked)
        {
            var rankings = new List<IList<string>>();
            if (run.casts != null)
            {
                for (int i = 0; i < run.casts.Count; i++)
                {
                    var c = run.casts[i];
                    if (c == null || c.spoiled) continue;
                    rankings.Add(c.ranking != null && c.ranking.Count > 0
                        ? (IList<string>)c.ranking
                        : new List<string> { c.optionId });
                }
            }
            var ids = new List<string>();
            for (int i = 0; i < ballot.options.Count; i++)
                if (ballot.options[i] != null)
                    ids.Add(ballot.options[i].optionId);
            var ranked = RankedTally.Run(rankings, ballot.tallyMethod, ballot.seats, ids);
            if (ranked.winners != null)
                for (int i = 0; i < ranked.winners.Count; i++)
                    winners.Add(ranked.winners[i]);
        }
        else
        {
            string winner = WinningOptionId(run);
            if (!string.IsNullOrEmpty(winner))
                winners.Add(winner);
        }
        for (int i = 0; i < ballot.options.Count; i++)
        {
            var opt = ballot.options[i];
            if (opt == null) continue;
            bool win = winners.Contains(opt.optionId);
            var list = win ? opt.win : opt.lose;
            Apply(list);
        }
        lastLemmaKey = VoteLemmaPropertyKeys.Tally;
        if (ballot != null && ballot.demographics != null)
            followOnDemographics = ballot.demographics;
    }

    public static string WinningOptionId(VoteRun run)
    {
        if (run == null) return null;
        var result = run.Tally();
        string best = null;
        int bestN = -1;
        if (result.tallies == null) return null;
        for (int i = 0; i < result.tallies.Count; i++)
        {
            var t = result.tallies[i];
            if (t == null) continue;
            if (t.count > bestN)
            {
                bestN = t.count;
                best = t.optionId;
            }
        }
        return best;
    }

    void Apply(List<VotePropertyAssignment> list)
    {
        if (list == null) return;
        if (certified == null) certified = new VotePropertyBag();
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i];
            if (a == null) continue;
            certified.Set(a.propertyName, a.propertyValue);
        }
    }

    public bool AccountingMatches(VoteResult local, VoteResult host)
    {
        if (local == null || host == null) return false;
        return local.tallyHash == host.tallyHash;
    }
}
