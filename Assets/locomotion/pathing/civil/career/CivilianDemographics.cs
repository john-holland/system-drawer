using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>City-scoped quotas so unemployed paper dolls stay inside statistical limits.</summary>
[Serializable]
public sealed class CivilianDemographics
{
    public int cityPopulation = 100;
    [Range(0f, 1f)] public float unemploymentRate01 = 0.08f;
    [Range(0f, 1f)] public float ageChild01 = 0.22f;
    [Range(0f, 1f)] public float ageAdult01 = 0.62f;
    [Range(0f, 1f)] public float ageSenior01 = 0.16f;
    [Range(0f, 1f)] public float eduNone01 = 0.45f;
    [Range(0f, 1f)] public float eduCert01 = 0.35f;
    [Range(0f, 1f)] public float eduDegree01 = 0.2f;
    [Range(0f, 1f)] public float slack01 = 0.08f;

    public int UnemployedQuota =>
        Mathf.Max(0, Mathf.CeilToInt(Mathf.Max(0, cityPopulation) * Mathf.Clamp01(unemploymentRate01)));

    public static CivilianDemographics FromSocietyFeatures(
        IReadOnlyDictionary<string, float> societyFeatures,
        int population = 100)
    {
        var d = new CivilianDemographics { cityPopulation = Mathf.Max(1, population) };
        if (TryFeature(societyFeatures, "unemploymentRate", out float u) ||
            TryFeature(societyFeatures, "unemployment_rate", out u))
            d.unemploymentRate01 = Mathf.Clamp01(u);
        else if (TryFeature(societyFeatures, "welfareBenefits", out float w) ||
                 TryFeature(societyFeatures, "welfare_benefits", out w))
            d.unemploymentRate01 = Mathf.Clamp01(1f - w);
        return d;
    }

    public bool TryAcceptUnemployed(CivilianPaperDoll candidate, IList<CivilianPaperDoll> existing)
    {
        if (candidate == null || candidate.employment != CivilianEmploymentStatus.Unemployed)
            return false;
        int unemployed = CountUnemployed(existing);
        if (unemployed + 1 > UnemployedQuota)
            return false;
        if (!BandOk(candidate.ageBand, existing, AgeShare))
            return false;
        if (!BandOk(candidate.education, existing, EduShare))
            return false;
        return true;
    }

    public CivilianPaperDoll SampleUnemployed(string personaKey, IList<CivilianPaperDoll> existing, int seed = 0)
    {
        var rng = new System.Random(seed);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            var doll = ScriptableObject.CreateInstance<CivilianPaperDoll>();
            doll.personaKey = string.IsNullOrEmpty(personaKey) ? "civilian" : personaKey;
            doll.employment = CivilianEmploymentStatus.Unemployed;
            doll.ageBand = PickAge(rng);
            doll.education = PickEdu(rng);
            if (TryAcceptUnemployed(doll, existing))
                return doll;
            UnityEngine.Object.DestroyImmediate(doll);
        }
        return null;
    }

    float AgeShare(CivilianAgeBand b)
    {
        switch (b)
        {
            case CivilianAgeBand.Child0To17: return ageChild01;
            case CivilianAgeBand.Senior65Plus: return ageSenior01;
            default: return ageAdult01;
        }
    }

    float EduShare(CivilianEducationAttainment e)
    {
        switch (e)
        {
            case CivilianEducationAttainment.Certification: return eduCert01;
            case CivilianEducationAttainment.Degree: return eduDegree01;
            default: return eduNone01;
        }
    }

    bool BandOk<T>(T band, IList<CivilianPaperDoll> existing, Func<T, float> share)
    {
        int unemployed = CountUnemployed(existing);
        int next = unemployed + 1;
        int match = 0;
        if (existing != null)
        {
            for (int i = 0; i < existing.Count; i++)
            {
                var d = existing[i];
                if (d == null || d.employment != CivilianEmploymentStatus.Unemployed) continue;
                if (EqualityComparer<T>.Default.Equals(GetBand(d, band), band))
                    match++;
            }
        }
        match++;
        float target = Mathf.Clamp01(share(band));
        float allowed = target + slack01;
        return match <= Mathf.Max(1, Mathf.CeilToInt(next * allowed));
    }

    static T GetBand<T>(CivilianPaperDoll d, T probe)
    {
        if (probe is CivilianAgeBand)
            return (T)(object)d.ageBand;
        return (T)(object)d.education;
    }

    static int CountUnemployed(IList<CivilianPaperDoll> existing)
    {
        if (existing == null) return 0;
        int n = 0;
        for (int i = 0; i < existing.Count; i++)
            if (existing[i] != null && existing[i].employment == CivilianEmploymentStatus.Unemployed)
                n++;
        return n;
    }

    CivilianAgeBand PickAge(System.Random rng)
    {
        float r = (float)rng.NextDouble();
        if (r < ageChild01) return CivilianAgeBand.Child0To17;
        if (r < ageChild01 + ageAdult01) return CivilianAgeBand.Adult18To64;
        return CivilianAgeBand.Senior65Plus;
    }

    CivilianEducationAttainment PickEdu(System.Random rng)
    {
        float r = (float)rng.NextDouble();
        if (r < eduNone01) return CivilianEducationAttainment.None;
        if (r < eduNone01 + eduCert01) return CivilianEducationAttainment.Certification;
        return CivilianEducationAttainment.Degree;
    }

    static bool TryFeature(IReadOnlyDictionary<string, float> map, string key, out float value)
    {
        value = 0f;
        if (map == null || string.IsNullOrEmpty(key)) return false;
        return map.TryGetValue(key, out value);
    }
}
