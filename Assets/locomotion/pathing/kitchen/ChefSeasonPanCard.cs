using System.Collections.Generic;
using UnityEngine;

/// <summary>Pan tear-down / seasoning card: InOven, OnStove, or WipeOilAfterClean.</summary>
[System.Serializable]
public class ChefSeasonPanCard : ChefCard
{
    public ChefSeasonPanMode seasonMode = ChefSeasonPanMode.WipeOilAfterClean;
    [Range(0f, 1f)] public float oilAmount01 = 0.35f;
    public GameObject pan;
    public Transform hangReturnPose;

    public ChefSeasonPanCard()
    {
        dutyMode = ChefDutyMode.Hygiene;
        activity = ChefActivity.SeasonPan;
        isChefGoal = true;
        physicalPathingTag = "chef_season_pan";
    }

    public static ChefSeasonPanCard Generate(ChefSeasonPanMode mode, GameObject pan, float oil01 = 0.35f)
    {
        var card = new ChefSeasonPanCard
        {
            seasonMode = mode,
            pan = pan,
            stationOrTarget = pan,
            oilAmount01 = oil01,
            activity = ChefActivity.SeasonPan,
            dutyMode = ChefDutyMode.Hygiene,
            sectionName = $"season_pan_{mode}",
            description = $"Season pan {mode}",
            dutyChecklist = DefaultSeasonChecklist(mode)
        };
        return card;
    }

    public static List<string> DefaultSeasonChecklist(ChefSeasonPanMode mode)
    {
        switch (mode)
        {
            case ChefSeasonPanMode.InOven:
                return new List<string> { "scrape", "clean", "season", "park_oven" };
            case ChefSeasonPanMode.OnStove:
                return new List<string> { "scrape", "clean", "season", "park_stove" };
            default:
                return new List<string> { "scrape", "clean", "wipe_oil", "hang" };
        }
    }
}

/// <summary>Applies ChefSeasonPanCard modes and resets PanOilSmokeTracker.</summary>
public static class ChefSeasonPanSolver
{
    public static bool TrySolve(ChefSeasonPanCard card, float dt, out string status)
    {
        status = "ok";
        if (card == null)
        {
            status = "no_card";
            return false;
        }
        GameObject pan = card.pan != null ? card.pan : card.stationOrTarget;
        if (pan == null)
        {
            status = "no_pan";
            return false;
        }

        var tracker = pan.GetComponent<PanOilSmokeTracker>() ?? pan.GetComponentInChildren<PanOilSmokeTracker>();
        if (tracker == null) tracker = pan.AddComponent<PanOilSmokeTracker>();

        switch (card.seasonMode)
        {
            case ChefSeasonPanMode.InOven:
                tracker.smoke01 = Mathf.MoveTowards(tracker.smoke01, 0f, dt * 0.5f);
                tracker.oil01 = Mathf.Clamp01(card.oilAmount01);
                status = "park_oven";
                break;
            case ChefSeasonPanMode.OnStove:
                tracker.smoke01 = Mathf.MoveTowards(tracker.smoke01, 0f, dt * 0.4f);
                tracker.oil01 = Mathf.Clamp01(card.oilAmount01 * 0.8f);
                status = "park_stove";
                break;
            case ChefSeasonPanMode.WipeOilAfterClean:
                KitchenBioRhythmService.Instance?.NotifyCleanAttempt(0.08f);
                tracker.smoke01 = 0f;
                tracker.oil01 = Mathf.Clamp01(card.oilAmount01);
                status = "wipe_oil";
                break;
        }
        return true;
    }
}
