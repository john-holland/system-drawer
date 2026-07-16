using System.Collections.Generic;
using UnityEngine;
using SpatialVolumes;

/// <summary>
/// Character seating stress / tip-risk from CoG vs support polygon and optional SDF / convex-tree leaves.
/// Used for Physics IK training fitness (not planetary volcano stress).
/// </summary>
public sealed class SeatedStressManifoldEstimator
{
    public SitSurfaceContact surface;
    public ISpatialVolumeQuery volumeQuery;
    public float narrativeTime;

    public float LastTipRisk01 { get; private set; }
    public float LastCogErrorMeters { get; private set; }
    public Vector3 LastRestoreDir { get; private set; }
    public int LastLeafCount { get; private set; }

    public float Evaluate(GameObject actorRoot)
    {
        LastTipRisk01 = 1f;
        LastCogErrorMeters = 0f;
        LastRestoreDir = Vector3.zero;
        LastLeafCount = 0;
        if (surface == null || actorRoot == null)
            return 1f;

        Vector3 cog = ActorPhysicalCentroid.GetWorldCenterOfMass(actorRoot);
        bool inside = surface.TryProjectCog(cog, out Vector3 projected, out float tip);
        LastTipRisk01 = tip;
        LastCogErrorMeters = Vector3.Distance(projected, surface.WorldPlanePoint);
        LastRestoreDir = Vector3.ProjectOnPlane(surface.WorldPlanePoint - projected, surface.WorldPlaneNormal);
        if (LastRestoreDir.sqrMagnitude > 1e-6f)
            LastRestoreDir.Normalize();

        if (volumeQuery != null)
        {
            Bounds b = new Bounds(surface.WorldPlanePoint, Vector3.one * 1.5f);
            var leaves = new List<SpatialVolumeLeaf>(32);
            if (volumeQuery.SearchLeaves(b, narrativeTime, leaves))
            {
                LastLeafCount = leaves.Count;
                // Bias tip risk by nearest leaf sample if outside support volume.
                if (volumeQuery.TrySample(projected, narrativeTime, out float field, out bool volInside))
                {
                    if (!volInside)
                        LastTipRisk01 = Mathf.Clamp01(LastTipRisk01 + 0.15f + Mathf.Abs(field) * 0.05f);
                    else if (!inside)
                        LastTipRisk01 = Mathf.Clamp01(LastTipRisk01 + 0.1f);
                }
            }
        }

        return LastTipRisk01;
    }

    /// <summary>
    /// Training fitness in 0–1 (higher better): low tip risk, low CoG error, optional plant/sway bonuses.
    /// </summary>
    public float TrainingFitness(
        GameObject actorRoot,
        bool freeHangBraceSuccess = true,
        bool standOnPlantStable = true,
        bool feetClearCasters = true,
        bool schoochLiftHold = true)
    {
        float tip = Evaluate(actorRoot);
        float tipScore = 1f - Mathf.Clamp01(tip);
        float cogScore = 1f - Mathf.Clamp01(LastCogErrorMeters / 0.5f);
        float bonus = 1f;
        if (!freeHangBraceSuccess) bonus *= 0.7f;
        if (!standOnPlantStable) bonus *= 0.75f;
        if (!feetClearCasters) bonus *= 0.8f;
        if (!schoochLiftHold) bonus *= 0.8f;
        return Mathf.Clamp01((tipScore * 0.55f + cogScore * 0.45f) * bonus);
    }
}
