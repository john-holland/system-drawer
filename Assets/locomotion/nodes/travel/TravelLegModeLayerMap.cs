using System;
using UnityEngine;

/// <summary>
/// Maps <see cref="TravelLegMode"/> values to <see cref="SystemDrawerAnimator"/> layer indices.
/// </summary>
[Serializable]
public class TravelLegModeLayerMap
{
    [Tooltip("Animator layer index for walk locomotion.")]
    public int walkLayerIndex = 0;

    [Tooltip("Animator layer index for drive / vehicle control.")]
    public int driveLayerIndex = 1;

    [Tooltip("Animator layer index for fly locomotion.")]
    public int flyLayerIndex = 2;

    [Tooltip("Default blend duration when Ctx.estimatedLegTimeSec is zero.")]
    public float defaultBlendDurationSec = 0.35f;

    [Tooltip("Cap blend duration derived from estimated leg time.")]
    public float maxBlendDurationSec = 1.5f;

    public int ResolveLayerIndex(TravelLegMode mode)
    {
        switch (mode)
        {
            case TravelLegMode.Drive:
                return driveLayerIndex;
            case TravelLegMode.Fly:
                return flyLayerIndex;
            default:
                return walkLayerIndex;
        }
    }

    public float ResolveBlendDuration(float estimatedLegTimeSec)
    {
        if (estimatedLegTimeSec > 0.01f)
            return Mathf.Min(estimatedLegTimeSec * 0.15f, maxBlendDurationSec);
        return defaultBlendDurationSec;
    }
}
