using UnityEngine;

/// <summary>Fiber crops reuse TreeGrowthTravelAgent + LotGrassPlantDef (flax / hemp / cotton).</summary>
[AddComponentMenu("Locomotion/Travel/Fiber Farm Travel Agent")]
public sealed class FiberFarmTravelAgent : TreeGrowthTravelAgent
{
    public string speciesId = "flax";

    void Reset()
    {
        mineralWhitelist = new[] { "loam", "silt" };
        mineralBlacklist = new[] { "salt", "bedrock" };
    }

    public bool IsFiberSpecies(LotGrassPlantDef def)
    {
        string id = def != null ? def.speciesId : speciesId;
        if (string.IsNullOrEmpty(id)) return false;
        id = id.ToLowerInvariant();
        for (int i = 0; i < ClothingCommodities.FiberSpecies.Length; i++)
            if (id == ClothingCommodities.FiberSpecies[i])
                return true;
        return false;
    }
}
