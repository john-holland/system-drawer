using System.Collections.Generic;
using UnityEngine;

/// <summary>Treegen record: organism, mesh stages, and the branches the editor writes back.</summary>
[CreateAssetMenu(fileName = "TreeGenConfig", menuName = "Locomotion/Civil/Tree Gen Config")]
public sealed class TreeGenConfig : ScriptableObject
{
    public PlantOrganismDef organism;
    public LotGrassPlantDef meshStages;
    public int seed = 1;
    public List<PlantBranchDef> branches = new List<PlantBranchDef>();
    public List<BranchPathBake> bakes = new List<BranchPathBake>();

    public void PullFromOrganism()
    {
        branches = new List<PlantBranchDef>();
        if (organism?.branches == null) return;
        for (int i = 0; i < organism.branches.Count; i++)
            branches.Add(Copy(organism.branches[i]));
    }

    public void WriteToOrganism()
    {
        if (organism == null) return;
        organism.branches = new List<PlantBranchDef>();
        if (branches == null) return;
        for (int i = 0; i < branches.Count; i++)
            organism.branches.Add(Copy(branches[i]));
    }

    public void BakeAll()
    {
        if (bakes == null)
            bakes = new List<BranchPathBake>();
        bakes.Clear();
        if (branches == null) return;
        for (int i = 0; i < branches.Count; i++)
            bakes.Add(BranchPathBake.Bake(branches[i], seed + i));
    }

    public static PlantBranchDef Copy(PlantBranchDef src)
    {
        if (src == null) return new PlantBranchDef();
        var copy = new PlantBranchDef
        {
            branchTypeLabel = src.branchTypeLabel,
            lengthMin = src.lengthMin,
            lengthMax = src.lengthMax,
            originRotation = src.originRotation,
            grabberT01 = src.grabberT01,
            grabberRotationDeg = src.grabberRotationDeg,
            harvest = src.harvest,
            harvestSave = HarvestInventorySave.Copy(src.harvestSave),
            curvePoints = new List<Vector3>()
        };
        if (src.curvePoints != null)
        {
            for (int i = 0; i < src.curvePoints.Count; i++)
                copy.curvePoints.Add(src.curvePoints[i]);
        }
        return copy;
    }

    public static PlantBranchDef DefaultLeader()
    {
        return new PlantBranchDef
        {
            branchTypeLabel = "leader",
            lengthMin = 0.6f,
            lengthMax = 1.4f,
            curvePoints = new List<Vector3> { Vector3.zero, new Vector3(0.2f, 0.15f, 1f) }
        };
    }
}
