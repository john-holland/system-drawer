using System;
using System.Collections.Generic;
using Locomotion.Rig;
using UnityEngine;

[Serializable]
public sealed class DiggingTopologyNode
{
    public string nodeId = Guid.NewGuid().ToString("N");
    public Vector3 approachAnchorWorld;
    public Vector3 contactWorld;
    public bool stopAmbulation = true;
    public string boneTraitId = "hand_r";
}

[CreateAssetMenu(fileName = "DiggingTopology", menuName = "Locomotion/Digging Topology")]
public sealed class DiggingTopologyAsset : ScriptableObject
{
    public List<DiggingTopologyNode> nodes = new List<DiggingTopologyNode>();
    public bool defaultStopAmbulation = true;
}

/// <summary>Compiles digging topology into approach stop ids like OpenCloseTopologyCompiler.</summary>
public static class TopologicalDigSolver
{
    public sealed class CompileResult
    {
        public List<string> stepIds = new List<string>();
        public GameObject tool;
        public Transform scoopSurface;
        public BoneMap boneMap;
    }

    public static CompileResult Compile(DiggingTopologyAsset asset, GameObject tool = null, BoneMap map = null)
    {
        var result = new CompileResult { tool = tool, boneMap = map };
        if (map != null && map.TryGet("hand_r", out Transform hand))
            result.scoopSurface = hand;
        else if (tool != null)
            result.scoopSurface = tool.transform;

        if (asset?.nodes == null) return result;
        for (int i = 0; i < asset.nodes.Count; i++)
        {
            var n = asset.nodes[i];
            if (n == null) continue;
            string id = string.IsNullOrEmpty(n.nodeId) ? $"dig_{i}" : n.nodeId;
            result.stepIds.Add(id + (n.stopAmbulation ? "_stop" : "_ambulate"));
        }
        return result;
    }
}
