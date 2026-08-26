using UnityEngine;
using Locomotion.Rendering;

/// <summary>Drives TransparentOccluder dither from ApertureCrowdSampler occupancy so dense crowds fade behind buildings.</summary>
[RequireComponent(typeof(TransparentOccluder))]
[AddComponentMenu("Locomotion/Rendering/Crowd Transparent Occluder")]
public sealed class CrowdTransparentOccluder : MonoBehaviour
{
    public PathingAperture aperture;
    public TransparentOccluder occluder;
    [Range(0f, 1f)] public float occupancy01;

    void Awake()
    {
        if (occluder == null)
            occluder = GetComponent<TransparentOccluder>();
        if (occluder != null)
            occluder.crowdDither = true;
    }

    void LateUpdate()
    {
        occupancy01 = ApertureCrowdSampler.GetOccupancy01(aperture);
        occluder?.ApplyCrowdOccupancy(occupancy01);
    }
}
