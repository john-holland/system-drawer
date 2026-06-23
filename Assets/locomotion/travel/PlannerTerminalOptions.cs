using System;
using UnityEngine;

/// <summary>Options for appending a terminal leg after approach planning.</summary>
[Serializable]
public struct PlannerTerminalOptions
{
    public bool enableTerminalLeg;
    public TravelLegMode terminalMode;
    public float terminalSearchRadius;
    public bool autoFromProfile;

    public static PlannerTerminalOptions Disabled => new PlannerTerminalOptions { enableTerminalLeg = false };

    public static PlannerTerminalOptions Auto(float radius = 60f) => new PlannerTerminalOptions
    {
        enableTerminalLeg = true,
        autoFromProfile = true,
        terminalSearchRadius = radius,
    };
}
