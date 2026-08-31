using UnityEngine;

/// <summary>Street → building water shutoff and service-panel feed at the lot tap cell.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/House Utility Tap")]
public sealed class HouseUtilityTap : MonoBehaviour
{
    public BuildingWaterShutoff shutoff;
    public CircuitBreakerPanel panel;
    public WaterNode waterNode;
    public SewerNode sewerNode;
    public Vector3 tapWorld;

    public void TripStreetBuildingWaterBreaker()
    {
        if (shutoff != null)
            shutoff.SetOpen(false);
        if (panel != null)
            panel.SetFeed(false);
    }

    public void RestoreStreetBuildingWaterBreaker()
    {
        if (shutoff != null)
            shutoff.SetOpen(true);
        if (panel != null)
            panel.SetFeed(true);
    }
}
