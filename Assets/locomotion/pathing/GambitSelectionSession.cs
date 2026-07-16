using System.Collections.Generic;
using UnityEngine;

/// <summary>Runtime state for slow-time gambit aperture selection.</summary>
public sealed class GambitSelectionSession : MonoBehaviour
{
    public PathingApertureRegistry registry;
    public SlowTimeController slowTime;
    public AngularTargetSelectMode selectMode;
    public GambitSteeringEnforcer steeringEnforcer;
    public TravelAgent travelAgent;
    public GambitInputTriggerBuffer inputBuffer;

    public readonly List<PathingAperture> candidates = new List<PathingAperture>();
    public PathingAperture hoveredAperture;
    public PathingAperture selectedAperture;
    [Range(0f, 1f)] public float enforcement01 = 1f;
    public bool slowTimeActive;
    public bool requirePlayerConfirm = true;

    public void Begin(PathingApertureMode modeFilter, string tagFilter, float timeScaleCoefficient, float enforcement)
    {
        enforcement01 = Mathf.Clamp01(enforcement);
        if (steeringEnforcer != null)
            steeringEnforcer.enforcement01 = enforcement01;
        candidates.Clear();
        hoveredAperture = null;
        selectedAperture = null;
        if (registry != null)
            candidates.AddRange(registry.Query(modeFilter, tagFilter));
        if (selectMode != null)
            selectMode.SetCandidates(candidates);
        if (slowTime != null)
        {
            slowTime.Enter(timeScaleCoefficient);
            slowTimeActive = true;
        }
        if (inputBuffer != null)
            inputBuffer.Clear();
    }

    public void SetHovered(PathingAperture aperture)
    {
        hoveredAperture = aperture;
        if (selectMode != null)
            selectMode.SetHovered(aperture);
    }

    public bool TryConfirmHovered()
    {
        if (hoveredAperture == null) return false;
        selectedAperture = hoveredAperture;
        if (selectMode != null)
            selectMode.SetSelected(selectedAperture);
        return true;
    }

    public void Cancel()
    {
        selectedAperture = null;
        hoveredAperture = null;
        if (selectMode != null)
            selectMode.ClearSelection();
        EndSlowTime();
    }

    public bool CommitToTravelAgent()
    {
        if (selectedAperture == null || travelAgent == null)
            return false;
        travelAgent.previewGoalWorld = selectedAperture.ApproachPointWorld;
        travelAgent.RebuildCachedPlan();
        if (steeringEnforcer != null)
            steeringEnforcer.enforcement01 = enforcement01;
        EndSlowTime();
        return true;
    }

    public void EndSlowTime()
    {
        if (slowTime != null && slowTimeActive)
            slowTime.Exit();
        slowTimeActive = false;
    }
}
