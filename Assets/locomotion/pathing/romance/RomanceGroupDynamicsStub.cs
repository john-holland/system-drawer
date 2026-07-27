using UnityEngine;

/// <summary>Stub group-dynamics resolver for romance scenes (acceptance / crowd / societal).</summary>
public static class RomanceGroupDynamicsStub
{
    public static float Acceptance01(RomanceGroupDynamics dynamics)
    {
        switch (dynamics)
        {
            case RomanceGroupDynamics.CrowdLoveMaking: return 0.85f;
            case RomanceGroupDynamics.DullRampantAcceptance: return 0.7f;
            case RomanceGroupDynamics.CausalAcceptance: return 0.55f;
            case RomanceGroupDynamics.SocietalImpact: return 0.4f;
            default: return 0.5f;
        }
    }

    public static bool AllowsPublicDisplay(RomanceGroupDynamics dynamics, float localNorm01)
    {
        return Acceptance01(dynamics) >= 0.5f || localNorm01 >= 0.65f;
    }
}
