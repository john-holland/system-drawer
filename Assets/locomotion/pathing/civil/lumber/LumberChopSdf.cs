using UnityEngine;

[CreateAssetMenu(fileName = "LumberIkTrainingCatalog", menuName = "Locomotion/Civil/Lumber IK Training Catalog")]
public sealed class LumberIkTrainingCatalog : ScriptableObject
{
    public string[] modeIds =
    {
        "back_cut", "axe_swing", "proxy_instrument", "move_log", "move_log_vehicle"
    };
    public string defaultChopModeId = "axe_swing";
    public string defaultCarryModeId = "move_log";
}

[CreateAssetMenu(fileName = "Chainsaw", menuName = "Locomotion/Civil/Chainsaw")]
public sealed class ChainsawSpec : ScriptableObject
{
    public GarageChainSpec chain;
    public GearboxSpec motorGearbox;
    [Range(0f, 1f)] public float sharpness01 = 0.8f;
    [Range(0f, 1f)] public float hardness01 = 0.7f;
    public string[] failureLanes = { "pinch", "kickback", "dull" };
    public float barLengthM = 0.45f;
}

/// <summary>SDF Max cutter + diggable subtract for limb choosing.</summary>
public static class LumberChopSdf
{
    public static int ChopLimb(DiggableVolume volume, Vector3 worldHit, float amount)
    {
        if (volume == null) return 0;
        volume.materialClass = "wood";
        return volume.ApplyScoop(new DigScoopSph(), worldHit, Mathf.Max(0.02f, amount));
    }
}

[System.Serializable]
public sealed class LumberJackingDebarkCard : GoodSection
{
    public DiggableVolume treeVolume;
    public SdfMax.SdfMaxCompositionAsset barkLayerSdf;

    public LumberJackingDebarkCard()
    {
        isTravelAgentGoal = true;
        physicalPathingTag = "debark";
        sectionName = "lumber_debark";
    }

    public static LumberJackingDebarkCard Generate(DiggableVolume volume)
    {
        return new LumberJackingDebarkCard
        {
            treeVolume = volume,
            sectionName = "lumber_debark",
            description = "Debark lathe / cutter"
        };
    }
}

[System.Serializable]
public sealed class WoodMillPlankCutCard : GoodSection
{
    public int kerfCount;
    public int plankCount;
    public bool sectionAfterCut;

    public WoodMillPlankCutCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "plank_cut";
        sectionName = "wood_mill_plank_cut";
    }

    public static WoodMillPlankCutCard Generate(DispatchRequest request, int kerfCount, bool section)
    {
        int kerfs = MillKerfCuts.OddCutCount(kerfCount);
        var c = new WoodMillPlankCutCard
        {
            kerfCount = kerfs,
            plankCount = MillKerfCuts.PlankCount(kerfs),
            sectionAfterCut = section,
            sectionName = section ? "wood_mill_section" : "wood_mill_plank_cut",
            physicalPathingTag = section ? "plank_section" : "plank_cut",
            description = section
                ? kerfs + " kerfs → " + MillKerfCuts.PlankCount(kerfs) + " sections"
                : kerfs + " PixelLight kerfs → " + MillKerfCuts.PlankCount(kerfs) + " planks"
        };
        return c;
    }
}
