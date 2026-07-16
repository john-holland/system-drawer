using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class OrganHealthEntry
{
    public string organId;
    public float rawHealth = OrganCatalog.GreatSpawnRaw;
    public string hostBodyPartPath;

    public float Normalized01(LifeSystemsDifficulty difficulty) =>
        OrganHealthNormalize.Normalize(rawHealth, difficulty);

    public string Label(LifeSystemsDifficulty difficulty) =>
        OrganHealthNormalize.Label(Normalized01(difficulty));

    public bool IsCompromised(LifeSystemsDifficulty difficulty) =>
        Normalized01(difficulty) < OrganHealthNormalize.PoorThreshold;
}

/// <summary>Per-actor organ raw health bag. Spawns Great (raw 1.05).</summary>
[Serializable]
public sealed class OrganHealthState
{
    public List<OrganHealthEntry> entries = new List<OrganHealthEntry>();

    public void EnsureCatalogDefaults()
    {
        if (entries == null)
            entries = new List<OrganHealthEntry>();
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && !string.IsNullOrEmpty(entries[i].organId))
                have.Add(entries[i].organId);
        }
        var organs = OrganCatalog.Organs;
        for (int i = 0; i < organs.Count; i++)
        {
            if (have.Contains(organs[i].id))
                continue;
            entries.Add(new OrganHealthEntry
            {
                organId = organs[i].id,
                rawHealth = OrganCatalog.GreatSpawnRaw
            });
        }
    }

    public bool TryGet(string organId, out OrganHealthEntry entry)
    {
        entry = null;
        if (string.IsNullOrEmpty(organId) || entries == null)
            return false;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null &&
                string.Equals(entries[i].organId, organId, StringComparison.OrdinalIgnoreCase))
            {
                entry = entries[i];
                return true;
            }
        }
        return false;
    }

    public float GetRaw(string organId)
    {
        return TryGet(organId, out var e) ? e.rawHealth : OrganCatalog.GreatSpawnRaw;
    }

    public float GetNormalized(string organId, LifeSystemsDifficulty difficulty)
    {
        return TryGet(organId, out var e)
            ? e.Normalized01(difficulty)
            : OrganHealthNormalize.Normalize(OrganCatalog.GreatSpawnRaw, difficulty);
    }

    public void ApplyRawDelta(string organId, float rawDelta, LifeSystemsDifficulty difficulty, float damageScale = 1f)
    {
        EnsureCatalogDefaults();
        if (!TryGet(organId, out var e) || e == null)
            return;
        float delta = rawDelta;
        if (delta < 0f)
        {
            float scale = damageScale;
            if (difficulty == LifeSystemsDifficulty.Easy)
                scale *= 0.5f;
            delta *= scale;
        }
        e.rawHealth += delta;
    }

    public void SetRaw(string organId, float raw)
    {
        EnsureCatalogDefaults();
        if (!TryGet(organId, out var e) || e == null)
            return;
        e.rawHealth = raw;
    }
}
