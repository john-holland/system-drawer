using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class DemographicSpatialAnchor
{
    public Vector3 worldPos;
    public float radiusM = 40f;
    public float unemploymentDelta;
    public float ageChildDelta;
    public float ageAdultDelta;
    public float ageSeniorDelta;
    public float eduNoneDelta;
    public float eduCertDelta;
    public float eduDegreeDelta;
    // Cellular (ignored by civ ResolveAt when unused)
    public float allergyPrevalenceDelta;
    public float glueReadinessDelta;
    public float sensorShareDelta;
    public float adhesiveShareDelta;
    public float immuneShareDelta;
}

[Serializable]
public sealed class DemographicSpatialSampleBound
{
    public string boundId;
    public Vector3 center;
    public Vector3 halfExtents = new Vector3(20f, 5f, 20f);

    public bool Contains(Vector3 p)
    {
        Vector3 d = p - center;
        return Mathf.Abs(d.x) <= halfExtents.x
               && Mathf.Abs(d.y) <= halfExtents.y
               && Mathf.Abs(d.z) <= halfExtents.z;
    }

    public Vector3 SamplePoint(System.Random rng)
    {
        if (rng == null) rng = new System.Random();
        return center + new Vector3(
            ((float)rng.NextDouble() * 2f - 1f) * halfExtents.x,
            ((float)rng.NextDouble() * 2f - 1f) * halfExtents.y,
            ((float)rng.NextDouble() * 2f - 1f) * halfExtents.z);
    }
}

[Serializable]
public sealed class DemographicSpatialGradient
{
    [Range(0f, 1f)] public float strength01 = 0.5f;
    public List<DemographicSpatialAnchor> anchors = new List<DemographicSpatialAnchor>();
    public List<DemographicSpatialSampleBound> sampleBounds = new List<DemographicSpatialSampleBound>();

    public bool IsActive => strength01 > 1e-4f && anchors != null && anchors.Count > 0;

    public Vector3 SamplePosForDemographics(Vector3 spawnPos, int seed)
    {
        if (sampleBounds == null || sampleBounds.Count == 0)
            return spawnPos;
        var containing = new List<DemographicSpatialSampleBound>();
        for (int i = 0; i < sampleBounds.Count; i++)
        {
            var b = sampleBounds[i];
            if (b != null && b.Contains(spawnPos))
                containing.Add(b);
        }
        var pool = containing.Count > 0 ? containing : sampleBounds;
        var usable = new List<DemographicSpatialSampleBound>();
        for (int i = 0; i < pool.Count; i++)
            if (pool[i] != null) usable.Add(pool[i]);
        if (usable.Count == 0) return spawnPos;
        var rng = new System.Random(seed);
        var pick = usable[rng.Next(usable.Count)];
        return pick.SamplePoint(rng);
    }

    public float WeightAt(Vector3 lookupPos)
    {
        if (!IsActive) return 0f;
        float sum = 0f;
        float wSum = 0f;
        for (int i = 0; i < anchors.Count; i++)
        {
            var a = anchors[i];
            if (a == null) continue;
            float r = Mathf.Max(0.01f, a.radiusM);
            float d = Vector3.Distance(lookupPos, a.worldPos);
            float w = StatCurveCatalog.Get("spatial_falloff")(d / r, default);
            sum += w;
            wSum += 1f;
        }
        if (wSum <= 0f) return 0f;
        return Mathf.Clamp01(sum / wSum * strength01);
    }

    public void AccumulateCivDeltas(Vector3 lookupPos, out float unemp, out float child, out float adult,
        out float senior, out float eduNone, out float eduCert, out float eduDeg)
    {
        unemp = child = adult = senior = eduNone = eduCert = eduDeg = 0f;
        if (!IsActive) return;
        float wSum = 0f;
        for (int i = 0; i < anchors.Count; i++)
        {
            var a = anchors[i];
            if (a == null) continue;
            float r = Mathf.Max(0.01f, a.radiusM);
            float d = Vector3.Distance(lookupPos, a.worldPos);
            float w = StatCurveCatalog.Get("spatial_falloff")(d / r, default);
            wSum += w;
            unemp += a.unemploymentDelta * w;
            child += a.ageChildDelta * w;
            adult += a.ageAdultDelta * w;
            senior += a.ageSeniorDelta * w;
            eduNone += a.eduNoneDelta * w;
            eduCert += a.eduCertDelta * w;
            eduDeg += a.eduDegreeDelta * w;
        }
        if (wSum <= 1e-6f) return;
        float s = strength01;
        unemp = unemp / wSum * s;
        child = child / wSum * s;
        adult = adult / wSum * s;
        senior = senior / wSum * s;
        eduNone = eduNone / wSum * s;
        eduCert = eduCert / wSum * s;
        eduDeg = eduDeg / wSum * s;
    }

    public void AccumulateCellularDeltas(Vector3 lookupPos, out float allergy, out float glue,
        out float sensor, out float adhesive, out float immune)
    {
        allergy = glue = sensor = adhesive = immune = 0f;
        if (!IsActive) return;
        float wSum = 0f;
        for (int i = 0; i < anchors.Count; i++)
        {
            var a = anchors[i];
            if (a == null) continue;
            float r = Mathf.Max(0.01f, a.radiusM);
            float d = Vector3.Distance(lookupPos, a.worldPos);
            float w = StatCurveCatalog.Get("spatial_falloff")(d / r, default);
            wSum += w;
            allergy += a.allergyPrevalenceDelta * w;
            glue += a.glueReadinessDelta * w;
            sensor += a.sensorShareDelta * w;
            adhesive += a.adhesiveShareDelta * w;
            immune += a.immuneShareDelta * w;
        }
        if (wSum <= 1e-6f) return;
        float s = strength01;
        allergy = allergy / wSum * s;
        glue = glue / wSum * s;
        sensor = sensor / wSum * s;
        adhesive = adhesive / wSum * s;
        immune = immune / wSum * s;
    }
}
