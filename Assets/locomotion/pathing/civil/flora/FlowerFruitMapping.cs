using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class FlowerFruitOverride
{
    public string speciesId;
    public string fruitId;
}

/// <summary>Species id maps to itself unless an override is set. Cherry and pear presets are explicit.</summary>
[CreateAssetMenu(fileName = "FlowerFruitMapping", menuName = "Locomotion/Civil/Flower Fruit Mapping")]
public sealed class FlowerFruitMapping : ScriptableObject
{
    public List<FlowerFruitOverride> overrides = new List<FlowerFruitOverride>();

    public string Resolve(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId)) return "";
        if (overrides != null)
        {
            for (int i = 0; i < overrides.Count; i++)
            {
                var row = overrides[i];
                if (row == null) continue;
                if (!string.Equals(row.speciesId, speciesId, StringComparison.OrdinalIgnoreCase))
                    continue;
                return string.IsNullOrEmpty(row.fruitId) ? speciesId : row.fruitId;
            }
        }
        return speciesId;
    }

    public void SetOverride(string speciesId, string fruitId)
    {
        if (overrides == null)
            overrides = new List<FlowerFruitOverride>();
        for (int i = 0; i < overrides.Count; i++)
        {
            if (overrides[i] != null &&
                string.Equals(overrides[i].speciesId, speciesId, StringComparison.OrdinalIgnoreCase))
            {
                overrides[i].fruitId = fruitId;
                return;
            }
        }
        overrides.Add(new FlowerFruitOverride { speciesId = speciesId, fruitId = fruitId });
    }

    public void AddCherryPearDefaults()
    {
        SetOverride("cherry_blossom", "cherry");
        SetOverride("pear_blossom", "pear");
    }
}
