using System;
using System.Collections.Generic;
using UnityEngine;

public enum CityPlaceableKind
{
    Building = 0,
    Intersection = 1,
    SchoolBusStop = 2,
    Custom = 3
}

[Serializable]
public sealed class CityPlaceableCandidate
{
    public string id = "candidate";
    public CityPlaceableKind placeableKind = CityPlaceableKind.Building;
    [Tooltip("Primary type key; shared shells often use shared_building or skyscraper.")]
    public string typeKey = "Generic";
    public GameObject prefab;
    public string buildingRagdollTypeHint;
    public UnityEngine.Object sg3dPromptComposition;
    public bool sg3dSharedBuildingCompatible;
    public bool sharedBuildingCompatible;
    public List<string> allowedTenantTypeKeys = new List<string>();
    public BuildingRequirementSpec buildingConfig;
    public int footprintCellsX = 1;
    public int footprintCellsZ = 1;
    public int footprintCellsY = 1;
    [Range(0f, 1f)] public float scoreBias01;
    public FloorPlanIndexMap defaultFloorPlanIndexMap;

    public bool AllowsTenant(string tenantTypeKey)
    {
        if (allowedTenantTypeKeys == null || allowedTenantTypeKeys.Count == 0) return true;
        if (string.IsNullOrEmpty(tenantTypeKey)) return true;
        for (int i = 0; i < allowedTenantTypeKeys.Count; i++)
            if (string.Equals(allowedTenantTypeKeys[i], tenantTypeKey, StringComparison.OrdinalIgnoreCase))
                return true;
        return false;
    }

    /// <summary>True when this shell covers the needed footprint (candidate >= need on each axis).</summary>
    public bool Fits(int needW, int needD, int needH) =>
        footprintCellsX >= needW && footprintCellsZ >= needD && footprintCellsY >= needH;
}

[CreateAssetMenu(fileName = "CityPlaceableCatalog", menuName = "Locomotion/Civil/City Placeable Catalog")]
public sealed class CityPlaceableCatalog : ScriptableObject
{
    public List<CityPlaceableCandidate> candidates = new List<CityPlaceableCandidate>();

    public CityPlaceableCandidate FindById(string id)
    {
        if (string.IsNullOrEmpty(id) || candidates == null) return null;
        for (int i = 0; i < candidates.Count; i++)
            if (candidates[i] != null && candidates[i].id == id)
                return candidates[i];
        return null;
    }

    public List<CityPlaceableCandidate> FindMatching(CityPlaceableKind kind, string typeKey, bool sharedShell)
    {
        var list = new List<CityPlaceableCandidate>();
        if (candidates == null) return list;
        for (int i = 0; i < candidates.Count; i++)
        {
            var c = candidates[i];
            if (c == null || c.placeableKind != kind) continue;
            if (sharedShell)
            {
                if (c.sharedBuildingCompatible)
                    list.Add(c);
                continue;
            }
            if (string.IsNullOrEmpty(typeKey) ||
                string.Equals(c.typeKey, typeKey, StringComparison.OrdinalIgnoreCase))
                list.Add(c);
        }
        return list;
    }
}
