using UnityEngine;

/// <summary>
/// Player-driven VehicleRagdoll skips TravelAgent speed/hold unless brakes or self-driving.
/// Does not set ignoreTrafficAvoidance (avoid-cop still applies).
/// </summary>
public static class PlayerVehicleTravelSlowOverride
{
    public static bool ShouldApplyTravelSlow(VehicleRagdoll vehicle)
    {
        if (vehicle == null) return true;
        var buffer = vehicle.GetComponentInChildren<RagdollPlayerInputBuffer>()
                     ?? vehicle.GetComponent<RagdollPlayerInputBuffer>();
        if (buffer == null || buffer.options == null || !buffer.options.overrideTravelAgentSlow)
            return true;
        if (buffer.State.selfDriving)
            return true;
        return buffer.State.brake01 > 0.01f;
    }

    public static bool ShouldApplyTravelSlow(TravelAgent agent)
    {
        if (agent == null) return true;
        var vehicle = agent.GetComponent<VehicleRagdoll>()
                      ?? agent.GetComponentInParent<VehicleRagdoll>()
                      ?? agent.GetComponentInChildren<VehicleRagdoll>();
        return ShouldApplyTravelSlow(vehicle);
    }
}
