using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene example of a treegen config. Editor gizmos draw spheres, arrows, and the grabber arc.</summary>
[AddComponentMenu("Locomotion/Civil/Tree Gen Example Plant")]
public sealed class TreeGenExamplePlant : MonoBehaviour
{
    public TreeGenConfig config;
    public float trunkHeight = 1.2f;
    public List<BranchPathBake> bakes = new List<BranchPathBake>();
    public List<PlantBranchDef> branches = new List<PlantBranchDef>();

    public Vector3 Crown => transform.position + Vector3.up * trunkHeight;

    public void Refresh()
    {
        bakes = new List<BranchPathBake>();
        branches = new List<PlantBranchDef>();
        if (config == null) return;
        config.BakeAll();
        if (config.branches == null) return;
        for (int i = 0; i < config.branches.Count; i++)
        {
            branches.Add(TreeGenConfig.Copy(config.branches[i]));
            bakes.Add(i < config.bakes.Count ? config.bakes[i] : BranchPathBake.Bake(config.branches[i], config.seed + i));
        }
    }
}
