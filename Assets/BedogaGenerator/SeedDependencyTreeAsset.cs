using System;
using UnityEngine;

public enum SeedDeriveFn
{
    Master,
    HashCombine,
    AspectIndex,
    DayCollapse,
    SleepPhase,
    DreamLstm
}

[Serializable]
public class SeedDependencyNode
{
    public string id;
    public string parentId;
    public SeedDeriveFn deriveFn = SeedDeriveFn.HashCombine;
    public int aspectIndex;
    public int salt;
}

[CreateAssetMenu(fileName = "SeedDependencyTree", menuName = "Bedoga/Seed Dependency Tree")]
public class SeedDependencyTreeAsset : ScriptableObject
{
    public int masterSeed = 42;
    public SeedDependencyNode[] nodes = Array.Empty<SeedDependencyNode>();

    public int DeriveSeed(string nodeId, int dayCollapseSeed = 0, int sleepSeed = 0)
    {
        if (nodes == null || nodes.Length == 0)
            return masterSeed;
        for (int i = 0; i < nodes.Length; i++)
        {
            if (nodes[i].id == nodeId)
                return DeriveNode(nodes[i], dayCollapseSeed, sleepSeed);
        }
        return masterSeed;
    }

    int DeriveNode(SeedDependencyNode node, int dayCollapseSeed, int sleepSeed)
    {
        int parent = masterSeed;
        if (!string.IsNullOrEmpty(node.parentId))
            parent = DeriveSeed(node.parentId, dayCollapseSeed, sleepSeed);
        switch (node.deriveFn)
        {
            case SeedDeriveFn.Master:
                return masterSeed;
            case SeedDeriveFn.AspectIndex:
                return HashCombine(parent, node.aspectIndex + node.salt);
            case SeedDeriveFn.DayCollapse:
                return HashCombine(parent, dayCollapseSeed);
            case SeedDeriveFn.SleepPhase:
                return HashCombine(parent, sleepSeed);
            case SeedDeriveFn.DreamLstm:
                return HashCombine(parent, dayCollapseSeed ^ sleepSeed);
            default:
                return HashCombine(parent, node.salt);
        }
    }

    public static int HashCombine(int a, int b)
    {
        unchecked
        {
            return (a * 397) ^ b;
        }
    }
}
