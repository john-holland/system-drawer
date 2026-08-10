using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TrainDispatchCard : TravelAgentCard
{
    public DispatchRequest request;
    public TrainVehicleRagdoll train;

    public TrainDispatchCard()
    {
        isTravelAgentGoal = true;
        isCivilGoal = true;
        physicalPathingTag = "train";
        traversabilityTag = "rail";
    }

    protected static void Fill(TrainDispatchCard c, DispatchRequest request, string name)
    {
        c.request = request;
        c.sectionName = name;
        c.description = request != null ? request.kind : name;
        c.goalWorld = request != null ? request.worldTarget : Vector3.zero;
        c.limits = new SectionLimits { maxForce = 90f, maxTorque = 24f, maxVelocityChange = 2f };
    }

    protected static T Make<T>(DispatchRequest request, string name) where T : TrainDispatchCard, new()
    {
        var c = new T();
        Fill(c, request, name);
        return c;
    }
}

[Serializable]
public class TrainEngineerCard : TrainDispatchCard
{
    public TAVehicleRouteCard route;
    public TAVehicleRecallCard recall;
    public TAVehicleParkHaltCard parkHalt;
    public TAVehicleReleasePassengersCard releasePassengers;
    public TAVehicleSpeakCard speak;
    public TAVehicleMusicCard music;
    public TAVehicleSoundDesignCard soundDesign;
    public TAVehicleParkCard park;
    public TAVehicleBayParkCard bayPark;
    public TAVehicleBayRepairCard bayRepair;
    public TAVehicleFuelCard fuel;

    public static TrainEngineerCard Generate(DispatchRequest request, TrainVehicleRagdoll train = null)
    {
        var c = Make<TrainEngineerCard>(request, "train_engineer");
        c.train = train;
        c.route = TAVehicleRouteCard.Generate(request);
        c.recall = TAVehicleRecallCard.Generate(request);
        c.parkHalt = TAVehicleParkHaltCard.Generate(request);
        c.releasePassengers = TAVehicleReleasePassengersCard.Generate(request, TAPassengerReleaseMode.NextStop);
        c.speak = TAVehicleSpeakCard.Generate(request);
        c.music = TAVehicleMusicCard.Generate(request);
        c.soundDesign = TAVehicleSoundDesignCard.Generate(request);
        c.park = TAVehicleParkCard.Generate(request);
        c.bayPark = TAVehicleBayParkCard.Generate(request);
        c.bayRepair = TAVehicleBayRepairCard.Generate(request);
        c.fuel = TAVehicleFuelCard.GenerateForTrain(request, train);
        return c;
    }

    public void ApplyFuel() => fuel?.ApplyFuel();

    public void ApplyMusic()
    {
        if (train == null || music == null) return;
        train.SetCabinMusic(music.trackId, music.play);
    }
}

[Serializable]
public class TrainDispatchTurnstyleRequestCard : TrainDispatchCard
{
    public static TrainDispatchTurnstyleRequestCard Generate(DispatchRequest request) =>
        Make<TrainDispatchTurnstyleRequestCard>(request, "train_dispatch_turnstile");
}

[Serializable]
public class TrainEngineerTurnstyleCard : TrainDispatchCard
{
    public static TrainEngineerTurnstyleCard Generate(DispatchRequest request) =>
        Make<TrainEngineerTurnstyleCard>(request, "train_engineer_turnstile");

    public void Apply()
    {
        train?.SendMessage("OnNarrativeSchedulerAction", TrainDispatchNarrativeIds.Turnstile,
            SendMessageOptions.DontRequireReceiver);
    }
}

[Serializable]
public class TrainEngineerStartCard : TrainDispatchCard
{
    public static TrainEngineerStartCard Generate(DispatchRequest request, TrainVehicleRagdoll train = null)
    {
        var c = Make<TrainEngineerStartCard>(request, "train_engineer_start");
        c.train = train;
        return c;
    }

    public void Apply() => train?.SetEngineRunning(true);
}

[Serializable]
public class TrainEngineerStopCard : TrainDispatchCard
{
    public static TrainEngineerStopCard Generate(DispatchRequest request, TrainVehicleRagdoll train = null)
    {
        var c = Make<TrainEngineerStopCard>(request, "train_engineer_stop");
        c.train = train;
        return c;
    }

    public void Apply() => train?.SetEngineRunning(false);
}

[Serializable]
public class TrainDispatchStartCard : TrainDispatchCard
{
    public static TrainDispatchStartCard Generate(DispatchRequest request) =>
        Make<TrainDispatchStartCard>(request, "train_dispatch_start");
}

[Serializable]
public class TrainDispatchStopCard : TrainDispatchCard
{
    public static TrainDispatchStopCard Generate(DispatchRequest request) =>
        Make<TrainDispatchStopCard>(request, "train_dispatch_stop");
}

[Serializable]
public class TrainDispatchSpeedAdjustCard : TrainDispatchCard
{
    public float targetSpeedMs = 20f;

    public static TrainDispatchSpeedAdjustCard Generate(DispatchRequest request, float speedMs = 20f)
    {
        var c = Make<TrainDispatchSpeedAdjustCard>(request, "train_dispatch_speed");
        c.targetSpeedMs = speedMs;
        return c;
    }
}

[Serializable]
public class TrainEngineerSpeedAdjustCard : TrainDispatchCard
{
    public float targetSpeedMs = 20f;

    public static TrainEngineerSpeedAdjustCard Generate(DispatchRequest request, TrainVehicleRagdoll train = null, float speedMs = 20f)
    {
        var c = Make<TrainEngineerSpeedAdjustCard>(request, "train_engineer_speed");
        c.train = train;
        c.targetSpeedMs = speedMs;
        return c;
    }

    public void Apply()
    {
        if (train == null) return;
        train.currentSpeedMs = Mathf.Clamp(targetSpeedMs, 0f, train.speedLimitMs);
        train.SendMessage("OnNarrativeSchedulerAction", TrainDispatchNarrativeIds.SpeedAdjust,
            SendMessageOptions.DontRequireReceiver);
    }
}

[Serializable]
public class TrainEngineerTrafficeStopCard : TrainDispatchCard
{
    public static TrainEngineerTrafficeStopCard Generate(DispatchRequest request) =>
        Make<TrainEngineerTrafficeStopCard>(request, "train_engineer_traffic_stop");
}

[Serializable]
public class TrainEngineerPlowCard : TrainDispatchCard
{
    public static TrainEngineerPlowCard Generate(DispatchRequest request, TrainVehicleRagdoll train = null)
    {
        var c = Make<TrainEngineerPlowCard>(request, "train_engineer_plow");
        c.train = train;
        return c;
    }

    public void Apply()
    {
        train?.TryUnfoldLimb("plow");
        train?.SendMessage("OnNarrativeSchedulerAction", TrainDispatchNarrativeIds.Plow,
            SendMessageOptions.DontRequireReceiver);
    }
}

[Serializable]
public class TrainEngineerJusticeCard : TrainDispatchCard
{
    public bool lockCabin = true;
    public CombatCard combat;
    public List<string> dialogSuggestions = new List<string>();

    public static TrainEngineerJusticeCard Generate(DispatchRequest request, TrainVehicleRagdoll train = null)
    {
        var c = Make<TrainEngineerJusticeCard>(request, "train_engineer_justice");
        c.train = train;
        c.justice = JusticeCard.Generate(JusticeAction.SecureArea, null);
        c.combat = new CombatCard { sectionName = "train_cabin_combat" };
        c.dialogSuggestions.Add("Cabin is being secured.");
        return c;
    }

    public void Apply() => train?.SetCabinLocked(lockCabin);
}

[Serializable]
public class TSATrainEngineerAttendant : TrainDispatchCard
{
    public List<string> dialogSuggestions = new List<string>();

    public static TSATrainEngineerAttendant Generate(DispatchRequest request)
    {
        var c = Make<TSATrainEngineerAttendant>(request, "tsa_train_attendant");
        c.dialogSuggestions.Add("Tickets please.");
        return c;
    }
}

[Serializable]
public class TrainEngineerFollowTrainCard : TrainDispatchCard
{
    public string leadConsistId;

    public static TrainEngineerFollowTrainCard Generate(DispatchRequest request, string leadId = null)
    {
        var c = Make<TrainEngineerFollowTrainCard>(request, "train_engineer_follow");
        c.leadConsistId = leadId ?? request?.notes;
        return c;
    }

    public void Apply()
    {
        train?.SendMessage("OnNarrativeSchedulerAction", TrainDispatchNarrativeIds.FollowTrain,
            SendMessageOptions.DontRequireReceiver);
    }
}

[Serializable]
public class TrainDispatchFollowTrainRequestCard : TrainDispatchCard
{
    public static TrainDispatchFollowTrainRequestCard Generate(DispatchRequest request) =>
        Make<TrainDispatchFollowTrainRequestCard>(request, "train_dispatch_follow");
}

[Serializable]
public class TrainYardBackupForwardCard : TrainDispatchCard
{
    public static TrainYardBackupForwardCard Generate(DispatchRequest request) =>
        Make<TrainYardBackupForwardCard>(request, "train_yard_backup_forward");
}

[Serializable]
public class TrainYardTowPushCard : TrainDispatchCard
{
    public static TrainYardTowPushCard Generate(DispatchRequest request) =>
        Make<TrainYardTowPushCard>(request, "train_yard_tow_push");
}
