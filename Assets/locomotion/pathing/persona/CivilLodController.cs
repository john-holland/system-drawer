using UnityEngine;

/// <summary>Combines FeatureBudget + speed log falloff + venue priority into CivilLodTier.</summary>
public sealed class CivilLodController
{
    public CivilSpeedLodPolicy speedPolicy = new CivilSpeedLodPolicy();
    public int maxFullSimVenues = 4;
    public int maxWokenActors = 24;

    public float LastCombinedScale { get; private set; } = 1f;
    public float LastSpeedScale { get; private set; } = 1f;
    public float LastBudgetScale { get; private set; } = 1f;

    public float ComputeCombinedScale(float playerSpeedMps)
    {
        LastSpeedScale = speedPolicy != null ? speedPolicy.ComputeLodScale(playerSpeedMps) : 1f;
        LastBudgetScale = 1f;
        if (FeatureBudget.IsAvailable)
        {
            float civil = FeatureBudget.GetGranularity(FeatureBudgetIds.CivilSystems);
            float society = FeatureBudget.GetGranularity(FeatureBudgetIds.Society);
            float pathing = FeatureBudget.GetGranularity(FeatureBudgetIds.Pathing);
            float narrative = FeatureBudget.GetGranularity(FeatureBudgetIds.Narrative);
            // Budget Auto wins as floor: take min of civil + secondary gates
            LastBudgetScale = Mathf.Min(civil, Mathf.Min(society, Mathf.Min(pathing, narrative)));
            if (LastBudgetScale <= 0f && !FeatureBudget.IsFeatureActive(FeatureBudgetIds.CivilSystems))
                LastBudgetScale = 0f;
        }
        LastCombinedScale = Mathf.Min(LastSpeedScale, LastBudgetScale);
        float floor = speedPolicy != null ? speedPolicy.lodFloor : 0.15f;
        if (LastBudgetScale <= 0f)
            LastCombinedScale = 0f;
        else
            LastCombinedScale = Mathf.Max(LastCombinedScale, Mathf.Min(floor, LastBudgetScale));
        return LastCombinedScale;
    }

    public CivilLodTier ResolveTier(float combinedScale, int venuePriorityIndex, int fullSimSlotsUsed)
    {
        if (combinedScale <= 0.01f)
            return CivilLodTier.Culled;
        if (venuePriorityIndex >= 0 && fullSimSlotsUsed >= maxFullSimVenues)
        {
            if (combinedScale >= 0.35f)
                return CivilLodTier.Proxy;
            if (combinedScale >= 0.15f)
                return CivilLodTier.Ghost;
            return CivilLodTier.Culled;
        }
        if (combinedScale >= 0.65f)
            return CivilLodTier.FullSim;
        if (combinedScale >= 0.35f)
            return CivilLodTier.Proxy;
        if (combinedScale >= 0.15f)
            return CivilLodTier.Ghost;
        return CivilLodTier.Culled;
    }
}
