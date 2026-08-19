using System;
using Locomotion.Rig;
using UnityEngine;

/// <summary>Digging card: tool + BoneMap trait + optional stopAmbulation.</summary>
[Serializable]
public class DiggingCard : GoodSection
{
    public GameObject tool;
    public string boneTraitId = "hand_r";
    public BoneMap boneMap;
    public bool stopAmbulation = true;
    public Vector3 contactWorld;
    public Transform scoopSurface;

    public DiggingCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "digging";
        traversabilityTag = "digging";
        sectionName = "digging";
    }

    public static DiggingCard Generate(GameObject tool, BoneMap map, bool stopAmbulation = true)
    {
        Transform scoop = tool != null ? tool.transform : null;
        if (map != null && map.TryGet("hand_r", out Transform hand))
            scoop = hand;
        return new DiggingCard
        {
            tool = tool,
            boneMap = map,
            scoopSurface = scoop,
            stopAmbulation = stopAmbulation,
            sectionName = stopAmbulation ? "digging_stop" : "digging_ambulate",
            isCivilGoal = true
        };
    }

    public Transform ResolveScoop()
    {
        if (scoopSurface != null) return scoopSurface;
        if (boneMap != null && boneMap.TryGet(boneTraitId, out Transform t))
            return t;
        return tool != null ? tool.transform : null;
    }
}
