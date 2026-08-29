using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ElectorateSlice
{
    public string sliceId = "slice";
    public string groupProperty = "party";
    public string groupValue = "democrat";
    [Range(0f, 1f)] public float share01 = 0.5f;
    [Range(0f, 1f)] public float yesTilt01 = 0.5f;
}

/// <summary>Demographic slices that always renormalize to a 100% whole. Default from gov-glove society features.</summary>
[Serializable]
public sealed class ElectorateDemographics
{
    public List<ElectorateSlice> slices = new List<ElectorateSlice>();

    public static ElectorateDemographics DefaultTwoParty()
    {
        var d = new ElectorateDemographics();
        d.slices = new List<ElectorateSlice>
        {
            new ElectorateSlice { sliceId = "dem", groupProperty = "party", groupValue = "democrat", share01 = 0.5f, yesTilt01 = 0.62f },
            new ElectorateSlice { sliceId = "rep", groupProperty = "party", groupValue = "republican", share01 = 0.5f, yesTilt01 = 0.38f }
        };
        d.Renormalize();
        return d;
    }

    public static ElectorateDemographics FromSocietyFeatures(IReadOnlyDictionary<string, float> societyFeatures)
    {
        var d = DefaultTwoParty();
        if (societyFeatures == null) return d;
        if (Try(societyFeatures, "congressStability", out float stab))
        {
            d.slices[0].yesTilt01 = Mathf.Clamp01(0.5f + stab * 0.2f);
            d.slices[1].yesTilt01 = Mathf.Clamp01(0.5f - stab * 0.2f);
        }
        if (Try(societyFeatures, "lobbyistActivity", out float lobby))
        {
            d.slices[0].share01 = Mathf.Clamp01(0.5f - lobby * 0.1f);
            d.slices[1].share01 = Mathf.Clamp01(0.5f + lobby * 0.1f);
        }
        d.Renormalize();
        return d;
    }

    public ElectorateSlice AddSlice(string groupProperty, string groupValue, float share01, float yesTilt01 = 0.5f)
    {
        if (slices == null) slices = new List<ElectorateSlice>();
        var slice = new ElectorateSlice
        {
            sliceId = (groupValue ?? "slice") + "-" + (slices.Count + 1),
            groupProperty = groupProperty ?? "group",
            groupValue = groupValue ?? "",
            share01 = Mathf.Clamp01(share01),
            yesTilt01 = Mathf.Clamp01(yesTilt01)
        };
        slices.Add(slice);
        ReconcileChanged(slices.Count - 1);
        return slice;
    }

    /// <summary>Keep the changed share; split 1 - share evenly across the others. Leftover hundredths go to the last other slice.</summary>
    public void ReconcileChanged(int changedIndex)
    {
        if (slices == null || slices.Count == 0) return;
        if (slices.Count == 1)
        {
            if (slices[0] != null)
                slices[0].share01 = 1f;
            return;
        }
        int n = slices.Count;
        int ci = Mathf.Clamp(changedIndex, 0, n - 1);
        int changed = Mathf.Clamp(Mathf.RoundToInt((slices[ci] != null ? slices[ci].share01 : 0f) * 100f), 0, 100);
        int remainder = 100 - changed;
        int others = n - 1;
        int even = remainder / others;
        int extra = remainder - even * others;
        int lastOther = ci == n - 1 ? n - 2 : n - 1;
        for (int i = 0; i < n; i++)
        {
            if (slices[i] == null) continue;
            if (i == ci)
                slices[i].share01 = changed / 100f;
            else
                slices[i].share01 = (even + (i == lastOther ? extra : 0)) / 100f;
        }
    }

    public void Renormalize()
    {
        if (slices == null || slices.Count == 0) return;
        float sum = 0f;
        for (int i = 0; i < slices.Count; i++)
            if (slices[i] != null)
                sum += Mathf.Max(0f, slices[i].share01);
        if (sum <= 1e-5f)
        {
            float even = 1f / slices.Count;
            for (int i = 0; i < slices.Count; i++)
                if (slices[i] != null)
                    slices[i].share01 = even;
            return;
        }
        for (int i = 0; i < slices.Count; i++)
            if (slices[i] != null)
                slices[i].share01 = Mathf.Max(0f, slices[i].share01) / sum;
    }

    public float Whole01()
    {
        float s = 0f;
        if (slices == null) return 0f;
        for (int i = 0; i < slices.Count; i++)
            if (slices[i] != null)
                s += slices[i].share01;
        return s;
    }

    public ElectorateSlice Sample(int seed)
    {
        Renormalize();
        if (slices == null || slices.Count == 0) return null;
        var rng = new System.Random(seed);
        float pick = (float)rng.NextDouble();
        float acc = 0f;
        for (int i = 0; i < slices.Count; i++)
        {
            var sl = slices[i];
            if (sl == null) continue;
            acc += sl.share01;
            if (pick <= acc) return sl;
        }
        return slices[slices.Count - 1];
    }

    /// <summary>Tilt a yes/no choice. Does not override an actor who already picked.</summary>
    public string TiltYesNo(ElectorateSlice slice, bool actorAlreadyPicked, string actorChoice, int seed)
    {
        if (actorAlreadyPicked && !string.IsNullOrEmpty(actorChoice))
            return actorChoice;
        float tilt = slice != null ? slice.yesTilt01 : 0.5f;
        var rng = new System.Random(seed);
        return rng.NextDouble() < tilt ? "yes" : "no";
    }

    static bool Try(IReadOnlyDictionary<string, float> map, string key, out float value)
    {
        value = 0f;
        return map != null && map.TryGetValue(key, out value);
    }
}
