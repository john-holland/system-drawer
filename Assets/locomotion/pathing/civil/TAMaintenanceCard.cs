using UnityEngine;

/// <summary>TravelAgent-compatible vehicle/bay maintenance card.</summary>
[System.Serializable]
public class TAMaintenanceCard : TravelAgentCard
{
    public VehicleRagdoll vehicle;
    public string duty = "repair";
    [Range(0f, 1f)] public float integrityDelta01 = 0.25f;
    public CivicCard bayCivicRepair;

    public TAMaintenanceCard()
    {
        isTravelAgentGoal = true;
        isCivicGoal = true;
        physicalPathingTag = "ta_maintenance";
        traversabilityTag = "maintenance";
        preferFlee = false;
    }

    public static TAMaintenanceCard GenerateBayService(VehicleRagdoll vehicle)
    {
        return Make(vehicle, "bay_service", 0.15f);
    }

    public static TAMaintenanceCard GenerateInspect(VehicleRagdoll vehicle)
    {
        return Make(vehicle, "inspect", 0f);
    }

    public static TAMaintenanceCard GenerateRepair(VehicleRagdoll vehicle)
    {
        return Make(vehicle, "repair", 0.35f);
    }

    static TAMaintenanceCard Make(VehicleRagdoll vehicle, string duty, float delta)
    {
        var c = new TAMaintenanceCard
        {
            vehicle = vehicle,
            duty = duty,
            integrityDelta01 = delta,
            sectionName = $"ta_maint_{duty}",
            description = duty,
            goalTarget = vehicle != null ? vehicle.gameObject : null,
            goalWorld = vehicle != null ? vehicle.transform.position : Vector3.zero,
            isTravelAgentGoal = true,
            isCivicGoal = true,
            justice = JusticeCard.Generate(JusticeAction.SecureArea, vehicle != null ? vehicle.gameObject : null),
            limits = new SectionLimits { maxForce = 80f, maxTorque = 20f, maxVelocityChange = 1.5f }
        };
        return c;
    }

    /// <summary>Apply integrity repair to the vehicle (and optional bay civic).</summary>
    public void ApplyMaintenance()
    {
        if (vehicle != null && integrityDelta01 > 0f)
            vehicle.integrity01 = Mathf.Clamp01(vehicle.integrity01 + integrityDelta01);
        bayCivicRepair?.MeetsCivicRequirements(null);
    }
}
