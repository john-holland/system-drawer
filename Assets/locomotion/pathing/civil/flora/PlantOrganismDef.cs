using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlantGrowthForm
{
    Tree = 0,
    Flower = 1,
    Bush = 2,
    Undergrowth = 3,
    Vine = 4,
    Moss = 5,
    Lichen = 6
}

[Serializable]
public sealed class PlantRootDef
{
    public Vector3 localTip;
    [Min(0.001f)] public float radius = 0.05f;
}

[Serializable]
public sealed class PlantIngredient
{
    public string itemId;
    public string partName;
}

[Serializable]
public sealed class PlantBranchDef
{
    public string branchTypeLabel = "leader";
    public float lengthMin = 0.4f;
    public float lengthMax = 1.2f;
    public Quaternion originRotation = Quaternion.identity;
    [Range(0f, 1f)] public float grabberT01 = 1f;
    public float grabberRotationDeg;
    public HarvestIkKind harvest = HarvestIkKind.Pick;
    public HarvestInventorySave harvestSave = new HarvestInventorySave();
    public List<Vector3> curvePoints = new List<Vector3>();
    public PlantPartWindLimit windLimit = new PlantPartWindLimit { impulseCap = 4f, bendCap = 1f };
    public bool broken;
}

[Serializable]
public sealed class PlantLeafDef
{
    public string partName = "leaf";
    public Mesh mesh;
}

[Serializable]
public sealed class PlantNectaryDef
{
    public Vector3 localPlacement;
    [Min(0.001f)] public float radius = 0.01f;
}

[Serializable]
public sealed class PlantFlowerBinding
{
    public string bindingId;
    public string speciesId;
}

/// <summary>Anther pollen: texture, SDF or mesh, and the particle emitter prefab.</summary>
[CreateAssetMenu(fileName = "Pollen", menuName = "Locomotion/Civil/Pollen")]
public sealed class PollenDef : ScriptableObject
{
    public string pollenId = "pollen";
    public Texture2D texture;
    public Mesh mesh;
    public SdfMax.SdfMaxCompositionAsset sdf;
    public GameObject particleEmitterPrefab;
}

/// <summary>
/// Shared species record for trees, flowers, bushes, undergrowth, vines, moss, and lichen.
/// Mesh stages stay on <see cref="LotGrassPlantDef"/> when a park prefab is used.
/// </summary>
[CreateAssetMenu(fileName = "PlantOrganism", menuName = "Locomotion/Civil/Plant Organism")]
public sealed class PlantOrganismDef : ScriptableObject
{
    public string speciesId = "plant";
    public PlantGrowthForm form = PlantGrowthForm.Tree;
    public LotGrassPlantDef meshStages;
    [Range(0f, 1f)] public float waterRequirement01 = 0.4f;
    [Range(0f, 1f)] public float sunRequirement01 = 0.35f;
    public string[] mineralWhitelist = { "loam", "silt" };
    public string[] mineralBlacklist = { "salt", "bedrock" };
    public List<PlantRootDef> roots = new List<PlantRootDef>();
    public List<PollenDef> pollens = new List<PollenDef>();
    public List<PlantIngredient> ingredients = new List<PlantIngredient>();
    public string commodityId;
    public string retinueRoleId;
    public List<PlantBranchDef> branches = new List<PlantBranchDef>();
    public List<PlantLeafDef> leaves = new List<PlantLeafDef>();
    public List<PlantFlowerBinding> flowers = new List<PlantFlowerBinding>();
    public List<PlantNectaryDef> nectaries = new List<PlantNectaryDef>();
}
