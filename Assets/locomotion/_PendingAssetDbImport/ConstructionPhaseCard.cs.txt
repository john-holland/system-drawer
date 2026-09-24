using UnityEngine;

/// <summary>RTS construction card: path to incomplete SG instance, then harden ghost at progress 1.</summary>
[System.Serializable]
public class ConstructionPhaseCard : TravelAgentCard
{
    public string sgInstanceId;
    public HouseConstructionLayerKind layer = HouseConstructionLayerKind.Foundation;
    public int frameIndex;
    [Range(0f, 1f)] public float progress01;
    public string drivewayLotId;
    public string garageLotId;

    public ConstructionPhaseCard()
    {
        sectionName = "construction_phase";
        isTravelAgentGoal = true;
        physicalPathingTag = "construction";
    }

    public static ConstructionPhaseCard GenerateDefault(Vector3 site)
    {
        return new ConstructionPhaseCard
        {
            sectionName = "construction_phase",
            goalWorld = site,
            preferFlee = false
        };
    }

    public bool IsComplete => progress01 >= 1f - 1e-4f;

    public void AddProgress(float delta)
    {
        progress01 = Mathf.Clamp01(progress01 + Mathf.Max(0f, delta));
    }
}
