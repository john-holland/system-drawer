using System;
using System.Collections.Generic;
using UnityEngine;

public enum CellularRoleKind
{
    Stem = 0,
    Immune = 1,
    Barrier = 2,
    Sensor = 3,
    Adhesive = 4,
    Metabolite = 5,
    Pathogen = 6,
    Custom = 7
}

[Serializable]
public sealed class CellularPeckingEntry
{
    public string cellKey;
    public CellularRoleKind role = CellularRoleKind.Sensor;
    public int peckingOrder = 100;
    public string speciesId;
    public string cultureId;
    public string hostActorId;
    public string partId;
    [Range(0f, 1f)] public float allergyPropensity01;
    [Range(0f, 1f)] public float adhesionReadiness01;
    [Range(0f, 1f)] public float load01;
}

[Serializable]
public sealed class CellularDemographics
{
    public string scopeId = "host";
    public int colonyPopulation = 100;
    [Range(0f, 1f)] public float stem01 = 0.1f;
    [Range(0f, 1f)] public float immune01 = 0.25f;
    [Range(0f, 1f)] public float barrier01 = 0.15f;
    [Range(0f, 1f)] public float sensor01 = 0.2f;
    [Range(0f, 1f)] public float adhesive01 = 0.15f;
    [Range(0f, 1f)] public float metabolite01 = 0.1f;
    [Range(0f, 1f)] public float pathogen01 = 0.05f;
    [Range(0f, 1f)] public float allergyPrevalence01 = 0.12f;
    [Range(0f, 1f)] public float glueReadiness01 = 0.2f;
    [Range(0f, 1f)] public float slack01 = 0.08f;
    public DemographicSpatialGradient spatialGradient;

    public static CellularDemographics DefaultTissue()
    {
        return new CellularDemographics();
    }

    public CellularDemographics ResolveAt(Vector3 lookupPos)
    {
        var copy = Clone();
        if (spatialGradient == null || !spatialGradient.IsActive)
            return copy;
        spatialGradient.AccumulateCellularDeltas(lookupPos,
            out float allergy, out float glue, out float sensor, out float adhesive, out float immune);
        copy.allergyPrevalence01 = Mathf.Clamp01(allergyPrevalence01 + allergy);
        copy.glueReadiness01 = Mathf.Clamp01(glueReadiness01 + glue);
        copy.sensor01 = Mathf.Max(0f, sensor01 + sensor);
        copy.adhesive01 = Mathf.Max(0f, adhesive01 + adhesive);
        copy.immune01 = Mathf.Max(0f, immune01 + immune);
        RenormRoles(copy);
        return copy;
    }

    public bool TryAccept(CellularPeckingEntry candidate, IList<CellularPeckingEntry> existing)
    {
        if (candidate == null) return false;
        float share = RoleShare(candidate.role);
        int next = (existing?.Count ?? 0) + 1;
        int match = 1;
        if (existing != null)
            for (int i = 0; i < existing.Count; i++)
                if (existing[i] != null && existing[i].role == candidate.role)
                    match++;
        float allowed = Mathf.Clamp01(share) + slack01;
        return match <= Mathf.Max(1, Mathf.CeilToInt(next * allowed));
    }

    public CellularPeckingEntry SampleMember(
        string cellKey, IList<CellularPeckingEntry> existing, int seed, Vector3? spawnPos = null)
    {
        CellularDemographics local = this;
        if (spawnPos.HasValue && spatialGradient != null && spatialGradient.IsActive)
        {
            Vector3 samplePos = spatialGradient.SamplePosForDemographics(spawnPos.Value, seed);
            local = ResolveAt(samplePos);
        }
        var rng = new System.Random(seed);
        for (int attempt = 0; attempt < 24; attempt++)
        {
            var e = new CellularPeckingEntry
            {
                cellKey = string.IsNullOrEmpty(cellKey) ? $"cell_{seed}_{attempt}" : cellKey,
                role = local.PickRole(rng),
                peckingOrder = 50 + rng.Next(0, 100),
                allergyPropensity01 = local.allergyPrevalence01 * (0.5f + (float)rng.NextDouble() * 0.5f),
                adhesionReadiness01 = local.glueReadiness01 * (0.5f + (float)rng.NextDouble() * 0.5f),
                load01 = 0.05f + (float)rng.NextDouble() * 0.2f
            };
            if (local.TryAccept(e, existing))
                return e;
        }
        return null;
    }

    float RoleShare(CellularRoleKind role)
    {
        switch (role)
        {
            case CellularRoleKind.Stem: return stem01;
            case CellularRoleKind.Immune: return immune01;
            case CellularRoleKind.Barrier: return barrier01;
            case CellularRoleKind.Sensor: return sensor01;
            case CellularRoleKind.Adhesive: return adhesive01;
            case CellularRoleKind.Metabolite: return metabolite01;
            case CellularRoleKind.Pathogen: return pathogen01;
            default: return 0.05f;
        }
    }

    CellularRoleKind PickRole(System.Random rng)
    {
        float r = (float)rng.NextDouble();
        float c = stem01;
        if (r < c) return CellularRoleKind.Stem;
        c += immune01; if (r < c) return CellularRoleKind.Immune;
        c += barrier01; if (r < c) return CellularRoleKind.Barrier;
        c += sensor01; if (r < c) return CellularRoleKind.Sensor;
        c += adhesive01; if (r < c) return CellularRoleKind.Adhesive;
        c += metabolite01; if (r < c) return CellularRoleKind.Metabolite;
        c += pathogen01; if (r < c) return CellularRoleKind.Pathogen;
        return CellularRoleKind.Custom;
    }

    CellularDemographics Clone()
    {
        return new CellularDemographics
        {
            scopeId = scopeId,
            colonyPopulation = colonyPopulation,
            stem01 = stem01,
            immune01 = immune01,
            barrier01 = barrier01,
            sensor01 = sensor01,
            adhesive01 = adhesive01,
            metabolite01 = metabolite01,
            pathogen01 = pathogen01,
            allergyPrevalence01 = allergyPrevalence01,
            glueReadiness01 = glueReadiness01,
            slack01 = slack01,
            spatialGradient = spatialGradient
        };
    }

    static void RenormRoles(CellularDemographics d)
    {
        float s = d.stem01 + d.immune01 + d.barrier01 + d.sensor01 + d.adhesive01 + d.metabolite01 + d.pathogen01;
        if (s <= 1e-6f)
        {
            d.stem01 = d.immune01 = d.barrier01 = d.sensor01 = d.adhesive01 = d.metabolite01 = d.pathogen01 = 1f / 7f;
            return;
        }
        d.stem01 /= s; d.immune01 /= s; d.barrier01 /= s; d.sensor01 /= s;
        d.adhesive01 /= s; d.metabolite01 /= s; d.pathogen01 /= s;
    }
}

[Serializable]
public sealed class CellularRetinue
{
    public string hostRetinueId;
    public string cultureId;
    public string partId;
    public List<CellularPeckingEntry> members = new List<CellularPeckingEntry>();
    public CellularDemographics demographics = new CellularDemographics();

    public void SortByPecking()
    {
        if (members == null) return;
        members.Sort((a, b) =>
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            return a.peckingOrder.CompareTo(b.peckingOrder);
        });
    }

    public CellularPeckingEntry SampleAndAdd(string cellKey, int seed, Vector3? spawnPos = null)
    {
        if (members == null) members = new List<CellularPeckingEntry>();
        if (demographics == null) demographics = CellularDemographics.DefaultTissue();
        var e = demographics.SampleMember(cellKey, members, seed, spawnPos);
        if (e == null) return null;
        e.cultureId = cultureId;
        e.partId = partId;
        members.Add(e);
        SortByPecking();
        return e;
    }
}
