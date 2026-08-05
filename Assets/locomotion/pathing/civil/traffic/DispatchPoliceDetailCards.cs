using System.Collections.Generic;
using UnityEngine;

/// <summary>Police traffic detail card driven by <see cref="TrafficDetailLadderAsset"/>.</summary>
[System.Serializable]
public class DispatchPoliceDetailCard : DispatchCard
{
    public TrafficDetailLadderAsset ladderAsset;
    public int stepIndex;
    public GameObject intersection;
    public PoliceCarVehicleRagdoll cruiser;
    public Vector3 worldTarget;

    public DispatchPoliceDetailCard()
    {
        sectionName = "dispatch_police_detail";
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "police_traffic_detail";
    }

    public TrafficDetailLadderStep CurrentStep =>
        ladderAsset != null && ladderAsset.steps != null && stepIndex >= 0 && stepIndex < ladderAsset.steps.Count
            ? ladderAsset.steps[stepIndex]
            : null;

    public static DispatchPoliceDetailCard GenerateFromLadder(TrafficDetailLadderAsset asset, Vector3 worldTarget, int stepIndex = 0)
    {
        return new DispatchPoliceDetailCard
        {
            ladderAsset = asset,
            stepIndex = Mathf.Max(0, stepIndex),
            worldTarget = worldTarget,
            request = new DispatchRequest
            {
                kind = "traffic_detail",
                worldTarget = worldTarget,
                notes = "traffic_detail"
            },
            sectionName = "dispatch_police_detail"
        };
    }

    public bool TryAdvanceStep()
    {
        if (ladderAsset == null || ladderAsset.steps == null) return false;
        if (stepIndex + 1 >= ladderAsset.steps.Count) return false;
        stepIndex++;
        return true;
    }

    /// <summary>Expand current ladder step into concrete duty cards.</summary>
    public List<GoodSection> ExpandStepCards()
    {
        var list = new List<GoodSection>();
        var step = CurrentStep;
        if (step == null) return list;
        switch (step.emitCard)
        {
            case TrafficDetailEmitCard.CopLights:
                list.Add(CopLightsCard.Generate(cruiser, true));
                break;
            case TrafficDetailEmitCard.OccupyIntersection:
                list.Add(CopCard.Generate("occupy_intersection", worldTarget));
                break;
            case TrafficDetailEmitCard.PullOver:
                list.Add(CopPullOverCard.Generate(worldTarget, cruiser));
                break;
            case TrafficDetailEmitCard.TrafficJustice:
                list.Add(TrafficJusticeCard.Generate(intersection));
                break;
            case TrafficDetailEmitCard.CopDetail:
                list.Add(CopDetailCard.GenerateProtect(intersection));
                break;
            case TrafficDetailEmitCard.Confirm:
                list.Add(DispatchConfirmCard.Generate(request));
                break;
        }
        return list;
    }
}
