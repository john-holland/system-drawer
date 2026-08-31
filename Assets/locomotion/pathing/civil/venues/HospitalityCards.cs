using System.Collections.Generic;
using UnityEngine;

/// <summary>Bouncer detail on JusticeCard (nightclub / bar door).</summary>
[System.Serializable]
public class BouncerCard : JusticeCard
{
    public Transform doorOrQueue;
    [Range(0f, 1f)] public float coverChargeGate01 = 0.2f;

    public BouncerCard()
    {
        justiceAction = JusticeAction.SecureArea;
        violenceThreshold01 = 0.55f;
        sectionName = "bouncer";
        physicalPathingTag = "justice_bouncer";
    }

    public static BouncerCard Generate(GameObject door, RagdollState state = null)
    {
        var c = new BouncerCard
        {
            hazardTarget = door,
            doorOrQueue = door != null ? door.transform : null,
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 90f, maxTorque = 25f, maxVelocityChange = 1.6f }
        };
        return c;
    }
}

/// <summary>Valet / parking attendant Justice detail.</summary>
[System.Serializable]
public class ValetCard : JusticeCard
{
    public ParkingLot lot;
    public Transform dropOff;

    public ValetCard()
    {
        justiceAction = JusticeAction.SecureArea;
        violenceThreshold01 = 0.75f;
        sectionName = "valet";
        physicalPathingTag = "justice_valet";
        defaultFleeUnlessTriggered = true;
    }

    public static ValetCard Generate(ParkingLot lot, RagdollState state = null)
    {
        return new ValetCard
        {
            lot = lot,
            dropOff = lot != null ? lot.arrivalAnchor : null,
            hazardTarget = lot != null ? lot.gameObject : null,
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 60f, maxTorque = 15f, maxVelocityChange = 1.2f }
        };
    }
}

[System.Serializable]
public class NightClubCard : GoodSection
{
    public string duty = "floor";
    public Transform danceFloor;
    public BeatQuantizedActionBinder beatBinder;

    public NightClubCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "nightclub";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "nightclub";
    }

    public static NightClubCard Generate(string duty, Transform danceFloor = null)
    {
        return new NightClubCard
        {
            duty = duty ?? "floor",
            danceFloor = danceFloor,
            sectionName = $"nightclub_{duty}",
            description = duty,
            isCivilGoal = true
        };
    }

    public float StartDelaySec() => beatBinder != null ? beatBinder.QuantizeDelaySec() : 0f;
}

[System.Serializable]
public class BarCard : GoodSection
{
    public string duty = "serve";
    public Transform barSurface;

    public BarCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "bar";
        traversabilityTag = "bar";
    }

    public static BarCard Generate(string duty, Transform bar = null)
    {
        return new BarCard
        {
            duty = duty ?? "serve",
            barSurface = bar,
            sectionName = $"bar_{duty}",
            description = duty,
            isCivilGoal = true
        };
    }
}

[System.Serializable]
public class HotelCard : GoodSection
{
    public string duty = "front_desk";
    public string roomId;
    public KeycardLock roomLock;
    public List<string> dutyChecklist = new List<string>();

    public HotelCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "hotel";
        traversabilityTag = "hotel";
    }

    public static HotelCard Generate(string duty, string roomId = null)
    {
        return new HotelCard
        {
            duty = duty ?? "front_desk",
            roomId = roomId,
            sectionName = $"hotel_{duty}",
            description = duty,
            isCivilGoal = true,
            dutyChecklist = DefaultHotelChecklist(duty)
        };
    }

    static List<string> DefaultHotelChecklist(string duty)
    {
        switch ((duty ?? "").ToLowerInvariant())
        {
            case "checkin": return new List<string> { "greet", "issue_keycard", "escort" };
            case "checkout": return new List<string> { "inspect", "settle", "revoke_keycard" };
            case "wakeup": return new List<string> { "call_room", "knock", "confirm" };
            default: return new List<string> { duty ?? "duty" };
        }
    }
}

[System.Serializable]
public class MaidCard : GoodSection
{
    public string roomId;
    public bool turndown;

    public MaidCard()
    {
        isCivilGoal = true;
        isCivicGoal = true;
        physicalPathingTag = "maid";
        traversabilityTag = "maid";
    }

    public static MaidCard Generate(string roomId, bool turndown = false)
    {
        return new MaidCard
        {
            roomId = roomId,
            turndown = turndown,
            sectionName = turndown ? "maid_turndown" : "maid_clean",
            description = turndown ? "turndown" : "clean_room",
            isCivilGoal = true,
            isCivicGoal = true
        };
    }
}

[System.Serializable]
public class CheckpointCard : GoodSection
{
    public string postId = "gate";
    public bool opsCenterDuty;

    public CheckpointCard()
    {
        isCivilGoal = true;
        isJusticeGoal = true;
        physicalPathingTag = "checkpoint";
        traversabilityTag = "checkpoint";
    }

    public static CheckpointCard Generate(string postId, bool ops = false)
    {
        return new CheckpointCard
        {
            postId = postId ?? "gate",
            opsCenterDuty = ops,
            sectionName = ops ? "checkpoint_ops" : $"checkpoint_{postId}",
            isCivilGoal = true,
            isJusticeGoal = true
        };
    }
}

[System.Serializable]
public class MonarchCard : GoodSection
{
    public string decorum = "audience";
    public Transform workWaypoint;
    public Transform homeWaypoint;
    public List<UtilityCard> utilityCards = new List<UtilityCard>();

    public MonarchCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "monarch";
        traversabilityTag = "monarch";
    }

    public static MonarchCard Generate(string decorum, Transform work = null, Transform home = null)
    {
        return new MonarchCard
        {
            decorum = decorum ?? "audience",
            workWaypoint = work,
            homeWaypoint = home,
            sectionName = $"monarch_{decorum}",
            description = decorum,
            isCivilGoal = true
        };
    }
}

[System.Serializable]
public class SpaCard : GoodSection
{
    public string treatment = "massage";
    public WrestlingCard wrestlingContact;

    public SpaCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "spa";
        traversabilityTag = "spa";
    }

    public static SpaCard Generate(string treatment, WrestlingCard compose = null)
    {
        return new SpaCard
        {
            treatment = treatment ?? "massage",
            wrestlingContact = compose,
            sectionName = $"spa_{treatment}",
            isCivilGoal = true
        };
    }
}

[System.Serializable]
public class BarberCard : GoodSection
{
    public string cutId = "trim";
    public SpaCard spaCompose;
    public WrestlingCard wrestlingCompose;
    public HairdoBlend wantedHairdo;
    public HairdoBlend currentHairdo;
    [Range(0f, 1f)] public float wetMask01;

    public BarberCard()
    {
        isCivilGoal = true;
        physicalPathingTag = "barber";
        traversabilityTag = "barber";
    }

    public static BarberCard Generate(string cutId, HairdoBlend wanted = null, float wet01 = 0f)
    {
        return new BarberCard
        {
            cutId = cutId ?? "trim",
            wantedHairdo = wanted ?? HairdoBlend.CreateDefault(),
            wetMask01 = wet01,
            spaCompose = SpaCard.Generate("shampoo"),
            wrestlingCompose = new WrestlingCard { mode = WrestlingMode.Play, moveKind = WrestlingMoveKind.LockGrapple },
            sectionName = $"barber_{cutId}",
            isCivilGoal = true
        };
    }

    /// <summary>Stub cross-product: current→wanted via blend weight (live SPH mask optional).</summary>
    public float HairdoBlendProgress01() => Mathf.Clamp01(1f - wetMask01 * 0.5f);
}
