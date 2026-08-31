using System.Collections.Generic;
using UnityEngine;

public enum UtilityCardKind
{
    Install = 0,
    Start = 1,
    ShutDown = 2,
    FilterChange = 3,
    GunkFlush = 4,
    RecoupSpin = 5,
    BreakerReset = 6,
    PlugIn = 7,
    Unplug = 8,
    SumpPrime = 9,
    SumpClear = 10,
    StreetBuildingWaterBreaker = 11
}

[System.Serializable]
public class UtilityCard : CivilCard
{
    public UtilityCardKind kind = UtilityCardKind.Start;
    public UtilityRoomBootstrap room;
    public string ikGoalId;

    public UtilityCard()
    {
        isCivilGoal = true;
        civicDuty = CivilianDutyKind.WorkShift;
        physicalPathingTag = "utility";
    }

    public void Apply()
    {
        if (room == null) return;
        room.Ensure();
        switch (kind)
        {
            case UtilityCardKind.Install:
                room.RequestInstallOpenCloseBt();
                break;
            case UtilityCardKind.Start:
                room.furnace?.SetRunning(true);
                room.heater?.SetRunning(true);
                room.hvac?.SetRunning(true);
                room.recoup?.SetSpinning(true);
                room.shutoff?.SetOpen(true);
                room.panel?.SetFeed(true);
                break;
            case UtilityCardKind.ShutDown:
                room.furnace?.SetRunning(false);
                room.heater?.SetRunning(false);
                room.hvac?.SetRunning(false);
                room.recoup?.SetSpinning(false);
                break;
            case UtilityCardKind.FilterChange:
                room.filter?.ChangeFilter();
                break;
            case UtilityCardKind.GunkFlush:
                room.jacobs?.Flush();
                break;
            case UtilityCardKind.RecoupSpin:
                room.recoup?.SetSpinning(true);
                break;
            case UtilityCardKind.BreakerReset:
                room.panel?.ResetBreakers();
                break;
            case UtilityCardKind.PlugIn:
                if (room.plugs != null && room.plugs.Length > 0)
                    room.plugs[0].PlugIn();
                ikGoalId = UtilityIkTrainingCatalog.PlugIn;
                break;
            case UtilityCardKind.Unplug:
                if (room.plugs != null && room.plugs.Length > 0)
                    room.plugs[0].Unplug();
                ikGoalId = UtilityIkTrainingCatalog.PlugOut;
                break;
            case UtilityCardKind.SumpPrime:
                room.sump?.Prime();
                break;
            case UtilityCardKind.SumpClear:
                room.sump?.Clear();
                break;
            case UtilityCardKind.StreetBuildingWaterBreaker:
                room.tap?.TripStreetBuildingWaterBreaker();
                ikGoalId = UtilityIkTrainingCatalog.BreakerFlipOff;
                break;
        }
    }

    public static UtilityCard Generate(UtilityCardKind kind, UtilityRoomBootstrap room = null)
    {
        return new UtilityCard
        {
            kind = kind,
            room = room,
            sectionName = $"utility_{kind}",
            description = kind.ToString(),
            isCivilGoal = true,
            dutyChecklist = new List<string> { kind.ToString().ToLowerInvariant() }
        };
    }
}

public static class UtilityCardCatalog
{
    public static List<UtilityCard> DefaultCards(UtilityRoomBootstrap room)
    {
        return new List<UtilityCard>
        {
            UtilityCard.Generate(UtilityCardKind.Start, room),
            UtilityCard.Generate(UtilityCardKind.ShutDown, room),
            UtilityCard.Generate(UtilityCardKind.FilterChange, room),
            UtilityCard.Generate(UtilityCardKind.GunkFlush, room),
            UtilityCard.Generate(UtilityCardKind.RecoupSpin, room),
            UtilityCard.Generate(UtilityCardKind.BreakerReset, room),
            UtilityCard.Generate(UtilityCardKind.PlugIn, room),
            UtilityCard.Generate(UtilityCardKind.Unplug, room),
            UtilityCard.Generate(UtilityCardKind.SumpPrime, room),
            UtilityCard.Generate(UtilityCardKind.SumpClear, room),
            UtilityCard.Generate(UtilityCardKind.StreetBuildingWaterBreaker, room)
        };
    }
}
