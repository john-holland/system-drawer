using Planetary.Composition;
using UnityEngine;

namespace Planetary
{
    public static class PlanetaryFeatureBudget
    {
        public static LodTier SelectLodWithBudget(
            PlanetaryHorizonLodController controller,
            float surfaceDistanceKm,
            float altitudeMSL,
            float cloudBaseM,
            float cloudTopM,
            float fallbackFullSimKm,
            float fallbackHorizonKm)
        {
            if (controller == null)
                return LodTier.FullSim;

            float fullSimKm = FeatureBudget.IsAvailable
                ? FeatureBudgetGranularityBridge.ResolveHorizonFullSimKm(FeatureBudget.Ratios, fallbackFullSimKm)
                : fallbackFullSimKm;
            float horizonKm = FeatureBudget.IsAvailable
                ? FeatureBudgetGranularityBridge.ResolveHorizonDistanceKm(FeatureBudget.Ratios, fallbackHorizonKm)
                : fallbackHorizonKm;

            var baseTier = controller.SelectLod(surfaceDistanceKm, altitudeMSL, cloudBaseM, cloudTopM,
                fullSimKm, horizonKm);

            if (!FeatureBudget.IsFeatureActive(FeatureBudgetIds.Planet))
                return LodTier.SpaceImpostor;

            float g = FeatureBudget.GetGranularity(FeatureBudgetIds.Planet);
            int bumped = FeatureBudgetGranularityBridge.ApplyLodTierBump((int)baseTier, g);
            return (LodTier)Mathf.Clamp(bumped, 0, (int)LodTier.SpaceImpostor);
        }

        public static float EffectiveSdfNearKm(float fallbackKm)
        {
            return FeatureBudgetGranularityBridge.ResolveSdfNearFullKm(FeatureBudget.Ratios, fallbackKm);
        }

        public static float EffectiveSdfFarKm(float fallbackKm)
        {
            return FeatureBudgetGranularityBridge.ResolveSdfFarFullKm(FeatureBudget.Ratios, fallbackKm);
        }
    }
}
