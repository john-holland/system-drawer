using System;
using System.Collections.Generic;

public enum BallotTallyMethod
{
    Plurality = 0,
    Irv = 1,
    Stv = 2
}

[Serializable]
public sealed class RankedTallyRound
{
    public Dictionary<string, float> counts = new Dictionary<string, float>(StringComparer.Ordinal);
    public string elected;
    public string eliminated;
    public float surplus;
    public int quota;
    public int active;
}

[Serializable]
public sealed class RankedTallyResult
{
    public BallotTallyMethod method;
    public int seats = 1;
    public List<string> winners = new List<string>();
    public Dictionary<string, int> firstPreferences = new Dictionary<string, int>(StringComparer.Ordinal);
    public List<RankedTallyRound> rounds = new List<RankedTallyRound>();
    public int quota;
}

/// <summary>IRV (single-winner ranked choice) and STV (Droop quota, multi-winner) on the same ranking lists.</summary>
public static class RankedTally
{
    public static BallotTallyMethod Parse(string method)
    {
        if (string.IsNullOrEmpty(method)) return BallotTallyMethod.Plurality;
        string raw = method.Trim().ToLowerInvariant();
        if (raw == "irv" || raw == "ranked" || raw == "ranked-choice" || raw == "rankedchoice" || raw == "rcv")
            return BallotTallyMethod.Irv;
        if (raw == "stv")
            return BallotTallyMethod.Stv;
        return BallotTallyMethod.Plurality;
    }

    public static List<string> Clean(IList<string> ranking)
    {
        var outList = new List<string>();
        if (ranking == null) return outList;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < ranking.Count; i++)
        {
            string oid = ranking[i];
            if (string.IsNullOrEmpty(oid) || !seen.Add(oid)) continue;
            outList.Add(oid);
        }
        return outList;
    }

    public static RankedTallyResult Run(
        IList<IList<string>> rankings,
        BallotTallyMethod method,
        int seats,
        IList<string> candidates = null)
    {
        var ballots = new List<List<string>>();
        var first = new Dictionary<string, int>(StringComparer.Ordinal);
        if (rankings != null)
        {
            for (int i = 0; i < rankings.Count; i++)
            {
                var cleaned = Clean(rankings[i]);
                if (cleaned.Count == 0) continue;
                ballots.Add(cleaned);
                first.TryGetValue(cleaned[0], out int n);
                first[cleaned[0]] = n + 1;
            }
        }

        if (method == BallotTallyMethod.Plurality)
        {
            var result = new RankedTallyResult { method = BallotTallyMethod.Plurality, seats = 1, firstPreferences = first };
            string best = null;
            int bestN = -1;
            foreach (var kv in first)
            {
                if (kv.Value > bestN || (kv.Value == bestN && (best == null || string.CompareOrdinal(kv.Key, best) < 0)))
                {
                    bestN = kv.Value;
                    best = kv.Key;
                }
            }
            if (best != null) result.winners.Add(best);
            return result;
        }

        var remaining = new HashSet<string>(StringComparer.Ordinal);
        if (candidates != null)
        {
            for (int i = 0; i < candidates.Count; i++)
                if (!string.IsNullOrEmpty(candidates[i]))
                    remaining.Add(candidates[i]);
        }
        if (remaining.Count == 0)
        {
            for (int i = 0; i < ballots.Count; i++)
                for (int j = 0; j < ballots[i].Count; j++)
                    remaining.Add(ballots[i][j]);
        }

        if (method == BallotTallyMethod.Irv)
            return Irv(ballots, remaining, first);
        return Stv(ballots, remaining, first, Math.Max(2, seats));
    }

    static RankedTallyResult Irv(List<List<string>> ballots, HashSet<string> remaining, Dictionary<string, int> first)
    {
        var result = new RankedTallyResult { method = BallotTallyMethod.Irv, seats = 1, firstPreferences = first };
        var live = new HashSet<string>(remaining, StringComparer.Ordinal);
        while (live.Count > 0)
        {
            var counts = ActiveFirst(ballots, live, out int active);
            var rec = new RankedTallyRound { counts = ToFloat(counts), active = active };
            if (active <= 0)
            {
                result.rounds.Add(rec);
                break;
            }
            string leader = Extreme(live, counts, preferHigh: true);
            if (counts[leader] > active / 2f || live.Count <= 2)
            {
                result.winners.Add(leader);
                rec.elected = leader;
                result.rounds.Add(rec);
                break;
            }
            string loser = Extreme(live, counts, preferHigh: false);
            live.Remove(loser);
            rec.eliminated = loser;
            result.rounds.Add(rec);
        }
        return result;
    }

    static RankedTallyResult Stv(List<List<string>> ballots, HashSet<string> remaining, Dictionary<string, int> first, int seats)
    {
        var papers = new List<Paper>(ballots.Count);
        for (int i = 0; i < ballots.Count; i++)
            papers.Add(new Paper { ranking = ballots[i], value = 1f });
        var live = new HashSet<string>(remaining, StringComparer.Ordinal);
        var elected = new List<string>();
        int quota = ballots.Count / (seats + 1) + 1;
        var result = new RankedTallyResult
        {
            method = BallotTallyMethod.Stv,
            seats = seats,
            firstPreferences = first,
            quota = quota
        };

        while (elected.Count < seats && live.Count > 0)
        {
            var counts = CurrentCounts(papers, live);
            var rec = new RankedTallyRound { counts = new Dictionary<string, float>(counts), quota = quota };
            string pick = null;
            float pickVotes = -1f;
            foreach (var kv in counts)
            {
                if (kv.Value < quota) continue;
                if (kv.Value > pickVotes || (Math.Abs(kv.Value - pickVotes) < 1e-6f && (pick == null || string.CompareOrdinal(kv.Key, pick) < 0)))
                {
                    pick = kv.Key;
                    pickVotes = kv.Value;
                }
            }
            if (pick != null)
            {
                float surplus = Math.Max(0f, pickVotes - quota);
                live.Remove(pick);
                elected.Add(pick);
                rec.elected = pick;
                rec.surplus = surplus;
                TransferSurplus(papers, live, pick, pickVotes > 0f && surplus > 0f ? surplus / pickVotes : 0f);
                result.rounds.Add(rec);
                continue;
            }
            int leftover = seats - elected.Count;
            if (live.Count <= leftover)
            {
                var rest = new List<string>(live);
                rest.Sort((a, b) =>
                {
                    float da = counts.TryGetValue(a, out float va) ? va : 0f;
                    float db = counts.TryGetValue(b, out float vb) ? vb : 0f;
                    int cmp = db.CompareTo(da);
                    return cmp != 0 ? cmp : string.CompareOrdinal(a, b);
                });
                for (int i = 0; i < rest.Count && elected.Count < seats; i++)
                {
                    elected.Add(rest[i]);
                    rec.elected = rest[i];
                }
                live.Clear();
                result.rounds.Add(rec);
                break;
            }
            string loser = ExtremeFloat(live, counts, preferHigh: false);
            live.Remove(loser);
            rec.eliminated = loser;
            result.rounds.Add(rec);
        }
        for (int i = 0; i < elected.Count && i < seats; i++)
            result.winners.Add(elected[i]);
        return result;
    }

    static Dictionary<string, int> ActiveFirst(List<List<string>> ballots, HashSet<string> live, out int active)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (string c in live)
            counts[c] = 0;
        active = 0;
        for (int i = 0; i < ballots.Count; i++)
        {
            var ranking = ballots[i];
            for (int j = 0; j < ranking.Count; j++)
            {
                if (!live.Contains(ranking[j])) continue;
                counts[ranking[j]]++;
                active++;
                break;
            }
        }
        return counts;
    }

    static Dictionary<string, float> CurrentCounts(List<Paper> papers, HashSet<string> live)
    {
        var counts = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (string c in live)
            counts[c] = 0f;
        for (int i = 0; i < papers.Count; i++)
        {
            var p = papers[i];
            if (p.value <= 0f) continue;
            for (int j = 0; j < p.ranking.Count; j++)
            {
                if (!live.Contains(p.ranking[j])) continue;
                counts[p.ranking[j]] += p.value;
                break;
            }
        }
        return counts;
    }

    static void TransferSurplus(List<Paper> papers, HashSet<string> live, string pick, float factor)
    {
        for (int i = 0; i < papers.Count; i++)
        {
            var p = papers[i];
            for (int j = 0; j < p.ranking.Count; j++)
            {
                string oid = p.ranking[j];
                if (oid == pick)
                {
                    p.value *= factor;
                    break;
                }
                if (live.Contains(oid))
                    break;
            }
        }
    }

    static string Extreme(HashSet<string> live, Dictionary<string, int> counts, bool preferHigh)
    {
        string pick = null;
        int best = preferHigh ? int.MinValue : int.MaxValue;
        foreach (string c in live)
        {
            int n = counts.TryGetValue(c, out int v) ? v : 0;
            if (pick == null)
            {
                pick = c;
                best = n;
                continue;
            }
            if (preferHigh)
            {
                if (n > best || (n == best && string.CompareOrdinal(c, pick) < 0))
                {
                    pick = c;
                    best = n;
                }
            }
            else if (n < best || (n == best && string.CompareOrdinal(c, pick) < 0))
            {
                pick = c;
                best = n;
            }
        }
        return pick;
    }

    static string ExtremeFloat(HashSet<string> live, Dictionary<string, float> counts, bool preferHigh)
    {
        string pick = null;
        float best = preferHigh ? float.MinValue : float.MaxValue;
        foreach (string c in live)
        {
            float n = counts.TryGetValue(c, out float v) ? v : 0f;
            if (pick == null)
            {
                pick = c;
                best = n;
                continue;
            }
            if (preferHigh)
            {
                if (n > best || (Math.Abs(n - best) < 1e-6f && string.CompareOrdinal(c, pick) < 0))
                {
                    pick = c;
                    best = n;
                }
            }
            else if (n < best || (Math.Abs(n - best) < 1e-6f && string.CompareOrdinal(c, pick) < 0))
            {
                pick = c;
                best = n;
            }
        }
        return pick;
    }

    static Dictionary<string, float> ToFloat(Dictionary<string, int> counts)
    {
        var map = new Dictionary<string, float>(StringComparer.Ordinal);
        foreach (var kv in counts)
            map[kv.Key] = kv.Value;
        return map;
    }

    sealed class Paper
    {
        public List<string> ranking;
        public float value;
    }
}
