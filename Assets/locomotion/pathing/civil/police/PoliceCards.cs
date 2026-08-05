using UnityEngine;

[System.Serializable]
public class PoliceCard : GoodSection
{
    public string duty = "desk";

    public PoliceCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "police";
        traversabilityTag = "police";
    }

    public static PoliceCard Generate(string duty)
    {
        return new PoliceCard
        {
            duty = duty ?? "desk",
            sectionName = $"police_{duty}",
            description = duty,
            isCivilGoal = true,
            limits = new SectionLimits { maxForce = 90f, maxTorque = 25f, maxVelocityChange = 1.8f }
        };
    }
}

[System.Serializable]
public class CopCard : GoodSection
{
    public string duty = "patrol";
    public Vector3 goalWorld;

    public CopCard()
    {
        isCivilGoal = true;
        isTravelAgentGoal = true;
        physicalPathingTag = "cop";
        traversabilityTag = "cop";
    }

    public static CopCard Generate(string duty, Vector3 goal)
    {
        return new CopCard
        {
            duty = duty ?? "patrol",
            goalWorld = goal,
            sectionName = $"cop_{duty}",
            description = duty,
            isCivilGoal = true,
            isTravelAgentGoal = true
        };
    }
}

/// <summary>Protect/escort — close follow, low posturing Justice detail.</summary>
[System.Serializable]
public class CopDetailCard : JusticeCard
{
    public GameObject protectTarget;
    public bool closeFollow = true;
    [Range(0f, 1f)] public float posturing01 = 0.25f;

    public CopDetailCard()
    {
        justiceAction = JusticeAction.SecureArea;
        violenceThreshold01 = 0.75f;
        sectionName = "cop_detail";
        physicalPathingTag = "justice_cop_detail";
        defaultFleeUnlessTriggered = false;
    }

    public static CopDetailCard GenerateProtect(GameObject target)
    {
        return new CopDetailCard
        {
            protectTarget = target,
            hazardTarget = target,
            closeFollow = true,
            posturing01 = 0.25f,
            sectionName = "cop_detail_protect"
        };
    }

    public static CopDetailCard GenerateEscort(GameObject target)
    {
        return new CopDetailCard
        {
            protectTarget = target,
            hazardTarget = target,
            closeFollow = true,
            posturing01 = 0.2f,
            sectionName = "cop_detail_escort"
        };
    }
}

/// <summary>Traffic Justice detail.</summary>
[System.Serializable]
public class TrafficJusticeCard : JusticeCard
{
    public TrafficJusticeCard()
    {
        justiceAction = JusticeAction.SecureArea;
        violenceThreshold01 = 0.7f;
        sectionName = "justice_traffic";
        physicalPathingTag = "justice_traffic";
    }

    public static TrafficJusticeCard Generate(GameObject vehicleOrLane)
    {
        return new TrafficJusticeCard
        {
            hazardTarget = vehicleOrLane,
            sectionName = "justice_traffic"
        };
    }
}

[System.Serializable]
public class CopPullOverCard : CopCard
{
    public PoliceCarVehicleRagdoll cruiser;
    [Range(0f, 1f)] public float posturing01 = 0.85f;
    [Range(0f, 1f)] public float backupThreshold01 = 0.65f;
    [Range(0f, 1f)] public float weaponFireThreshold01 = 0.9f;
    public bool lightsOn = true;

    public static CopPullOverCard Generate(Vector3 goal, PoliceCarVehicleRagdoll cruiser = null)
    {
        return new CopPullOverCard
        {
            duty = "pullover",
            goalWorld = goal,
            cruiser = cruiser,
            sectionName = "cop_pullover",
            isTravelAgentGoal = true,
            isCivilGoal = true
        };
    }

    public bool ShouldCallBackup(float threat01) => threat01 >= backupThreshold01;
    public bool ShouldFireWeapon(float threat01) => threat01 >= weaponFireThreshold01;
}

[System.Serializable]
public class CopLightsCard : CopCard
{
    public PoliceCarVehicleRagdoll cruiser;
    public bool lightsOn = true;
    public string narrativeActionId = "police_lights";

    public static CopLightsCard Generate(PoliceCarVehicleRagdoll cruiser, bool on)
    {
        return new CopLightsCard
        {
            duty = on ? "lights_on" : "lights_off",
            cruiser = cruiser,
            lightsOn = on,
            sectionName = on ? "cop_lights_on" : "cop_lights_off"
        };
    }

    public void Apply()
    {
        cruiser?.SetLights(lightsOn);
        if (cruiser != null)
            cruiser.SendMessage("OnNarrativeSchedulerAction", narrativeActionId,
                SendMessageOptions.DontRequireReceiver);
    }
}

[System.Serializable]
public class CopRequestWeaponCard : CopCard
{
    public PoliceCarVehicleRagdoll cruiser;
    public string telecomCode;
    public bool fulfilled;

    public static CopRequestWeaponCard Generate(PoliceCarVehicleRagdoll cruiser)
    {
        return new CopRequestWeaponCard
        {
            duty = "request_weapon",
            cruiser = cruiser,
            sectionName = "cop_request_weapon"
        };
    }

    public bool TryFulfill()
    {
        if (cruiser == null) return false;
        fulfilled = cruiser.TryOpenWeaponsChest(out telecomCode);
        return fulfilled;
    }
}

[System.Serializable]
public class PoliceInterrogateCard : PoliceCard
{
    public PoliceInterrogationRoom room;
    public GameObject subject;
    public bool useIkStub = true;
    public bool bindDialog = true;
    public bool bindMusic = true;

    public static PoliceInterrogateCard Generate(PoliceInterrogationRoom room, GameObject subject)
    {
        return new PoliceInterrogateCard
        {
            duty = "interrogate",
            room = room,
            subject = subject,
            bindDialog = room == null || room.enableDialog,
            bindMusic = room == null || room.enableMusic,
            sectionName = "police_interrogate"
        };
    }
}

[System.Serializable]
public class PoliceJailCivilianCard : PoliceCard
{
    public GameObject civilian;
    public WrestlingCard wrestlingCompose;
    public PoliceCuffItem cuffs;

    public static PoliceJailCivilianCard Generate(GameObject civilian, PoliceCuffItem cuffs = null)
    {
        return new PoliceJailCivilianCard
        {
            duty = "jail",
            civilian = civilian,
            cuffs = cuffs,
            wrestlingCompose = new WrestlingCard
            {
                mode = WrestlingMode.Play,
                moveKind = WrestlingMoveKind.LockGrapple
            },
            sectionName = "police_jail_civilian",
            isCivilGoal = true
        };
    }
}
