using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class VoteCastRecord
{
    public string actorId;
    public string ballotId;
    public string optionId;
    public List<string> ranking = new List<string>();
    public string demographicSliceId;
    public string causalityLeafId;
    public bool spoiled;
}

[Serializable]
public sealed class VoteTallyEntry
{
    public string optionId;
    public int count;
}

[Serializable]
public sealed class VoteResult
{
    public string runId;
    public string gameSessionId;
    public string causalityLeafId;
    public bool certified;
    public List<VoteTallyEntry> tallies = new List<VoteTallyEntry>();
    public int tallyHash;

    public int CountFor(string optionId)
    {
        if (tallies == null) return 0;
        for (int i = 0; i < tallies.Count; i++)
            if (tallies[i] != null && tallies[i].optionId == optionId)
                return tallies[i].count;
        return 0;
    }
}

[Serializable]
public sealed class VoteRun
{
    public string runId;
    public string gameSessionId;
    public string ballotId;
    public string causalityLeafId;
    public bool certified;
    public List<VoteCastRecord> casts = new List<VoteCastRecord>();
    public VoteResult result = new VoteResult();
    public VotePropertyBag properties = new VotePropertyBag();

    public VoteResult Tally()
    {
        if (result == null) result = new VoteResult();
        result.runId = runId;
        result.gameSessionId = gameSessionId;
        result.causalityLeafId = causalityLeafId;
        result.certified = certified;
        var map = new Dictionary<string, int>(StringComparer.Ordinal);
        if (casts != null)
        {
            for (int i = 0; i < casts.Count; i++)
            {
                var c = casts[i];
                if (c == null || c.spoiled || string.IsNullOrEmpty(c.optionId)) continue;
                map.TryGetValue(c.optionId, out int n);
                map[c.optionId] = n + 1;
            }
        }
        result.tallies = new List<VoteTallyEntry>();
        var keys = new List<string>(map.Keys);
        keys.Sort(StringComparer.Ordinal);
        int hash = 17;
        for (int i = 0; i < keys.Count; i++)
        {
            string k = keys[i];
            int v = map[k];
            result.tallies.Add(new VoteTallyEntry { optionId = k, count = v });
            unchecked
            {
                hash = hash * 31 + k.GetHashCode();
                hash = hash * 31 + v;
            }
        }
        result.tallyHash = hash;
        return result;
    }

    public VoteRun CloneForRecount()
    {
        var copy = new VoteRun
        {
            runId = Guid.NewGuid().ToString("N"),
            gameSessionId = gameSessionId,
            ballotId = ballotId,
            causalityLeafId = (causalityLeafId ?? "") + ".recount",
            certified = false,
            casts = new List<VoteCastRecord>()
        };
        if (casts != null)
        {
            for (int i = 0; i < casts.Count; i++)
            {
                var c = casts[i];
                if (c == null) continue;
                copy.casts.Add(new VoteCastRecord
                {
                    actorId = c.actorId,
                    ballotId = c.ballotId,
                    optionId = c.optionId,
                    ranking = c.ranking != null ? new List<string>(c.ranking) : new List<string>(),
                    demographicSliceId = c.demographicSliceId,
                    causalityLeafId = c.causalityLeafId,
                    spoiled = c.spoiled
                });
            }
        }
        copy.Tally();
        return copy;
    }
}
