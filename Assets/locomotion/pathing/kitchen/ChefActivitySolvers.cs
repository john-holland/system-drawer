using System;
using UnityEngine;

/// <summary>Shared chef verb solvers (compose; not one mega-class).</summary>
public static class ChefActivitySolvers
{
    public static bool TrySolve(ChefCard card, GameObject actor, float dt, out string status)
    {
        status = "ok";
        if (card == null || actor == null)
        {
            status = "missing";
            return false;
        }
        switch (card.activity)
        {
            case ChefActivity.Filet:
                return FiletSolver.Apply(card, actor, out status);
            case ChefActivity.Pour:
            case ChefActivity.Sprinkle:
            case ChefActivity.Shake:
                return PourAccuracySolver.Apply(card, actor, dt, out status);
            case ChefActivity.Dispense:
                return DispenseSolver.Apply(card, actor, dt, out status);
            case ChefActivity.Cut:
                return CutSolver.Apply(card, actor, out status);
            case ChefActivity.Stir:
                return StirSolver.Apply(card, actor, dt, out status);
            case ChefActivity.Throw:
            case ChefActivity.Drop:
            case ChefActivity.Place:
            case ChefActivity.Hold:
                status = card.activity.ToString().ToLowerInvariant();
                return true;
            case ChefActivity.Sear:
            case ChefActivity.Broil:
            case ChefActivity.Bake:
            case ChefActivity.Boil:
                return HeatCookSolver.Apply(card, actor, dt, out status);
            case ChefActivity.Spread:
                status = "spread";
                return true;
            case ChefActivity.WashHands:
            case ChefActivity.CleanStation:
                KitchenBioRhythmService.Instance?.NotifyCleanAttempt();
                status = "hygiene";
                return true;
            case ChefActivity.SeasonPan:
                if (card is ChefSeasonPanCard season)
                    return ChefSeasonPanSolver.TrySolve(season, dt, out status);
                KitchenBioRhythmService.Instance?.NotifyCleanAttempt(0.06f);
                status = "season_pan";
                return true;
            case ChefActivity.WashDish:
                KitchenBioRhythmService.Instance?.NotifyCleanAttempt(0.05f);
                status = "wash_dish";
                return true;
            default:
                status = "idle";
                return true;
        }
    }
}

public static class FiletSolver
{
    public static bool Apply(ChefCard card, GameObject actor, out string status)
    {
        status = "filet";
        // SDF Max split along spline is scene-authored; mark duty progress
        if (card.dutyChecklist != null && card.dutyChecklist.Count > 0)
            card.dutyChecklist[0] = "stabilize:done";
        return true;
    }
}

public static class PourAccuracySolver
{
    public static bool Apply(ChefCard card, GameObject actor, float dt, out string status)
    {
        float rate = Mathf.Max(0.01f, card.pourRateLitersPerSec);
        float delivered = rate * dt * Mathf.Clamp01(card.accuracy01);
        status = $"pour:{delivered:F3}L";
        DryLiquidPhaseMaterial.Accumulate(card.ingredientOrTool, delivered, card.activity == ChefActivity.Sprinkle);
        return true;
    }
}

public static class DispenseSolver
{
    public static bool Apply(ChefCard card, GameObject actor, float dt, out string status)
    {
        status = "dispense";
        var nozzle = card.ingredientOrTool != null
            ? card.ingredientOrTool.GetComponent<KitchenDispenseNozzle>()
            : null;
        if (nozzle != null)
            nozzle.Dispense(dt * card.pourRateLitersPerSec);
        return true;
    }
}

public static class CutSolver
{
    public static bool Apply(ChefCard card, GameObject actor, out string status)
    {
        status = "cut";
        return card.ingredientOrTool != null || card.stationOrTarget != null;
    }
}

public static class StirSolver
{
    public static bool Apply(ChefCard card, GameObject actor, float dt, out string status)
    {
        status = $"stir:{dt:F2}";
        return true;
    }
}

public static class HeatCookSolver
{
    public static bool Apply(ChefCard card, GameObject actor, float dt, out string status)
    {
        KitchenBioRhythmService.Instance?.NotifyCookHeat(dt * 0.05f);
        PanOilSmokeTracker.NotifyCook(card.stationOrTarget, card.activity, dt);

        if (card.evolutionCards == null || card.evolutionCards.Count == 0)
            card.evolutionCards.Add(ChefMaterialEvolutionCard.ForCook(card.activity));

        for (int i = 0; i < card.evolutionCards.Count; i++)
        {
            var evo = card.evolutionCards[i];
            if (evo == null) continue;
            evo.Advance(dt * 0.08f, smells =>
            {
                KitchenBioRhythmService.Instance?.ApplySmellTint(smells);
            });
        }
        status = card.activity.ToString().ToLowerInvariant();
        return true;
    }
}

/// <summary>Dry→murky→silt phase tracking for sprinkle/pour “dry liquid”.</summary>
public static class DryLiquidPhaseMaterial
{
    public enum Phase { Dry, Murky, Silt, Liquid }

    public static Phase Current = Phase.Dry;
    public static float AccumulatedVolume;

    public static void Accumulate(GameObject target, float liters, bool dryMode)
    {
        AccumulatedVolume += Mathf.Max(0f, liters);
        if (dryMode)
        {
            if (AccumulatedVolume < 0.05f) Current = Phase.Dry;
            else if (AccumulatedVolume < 0.25f) Current = Phase.Murky;
            else Current = Phase.Silt;
        }
        else
            Current = Phase.Liquid;
        if (target != null)
            target.SendMessage("OnDryLiquidPhase", Current, SendMessageOptions.DontRequireReceiver);
    }

    public static void Reset()
    {
        AccumulatedVolume = 0f;
        Current = Phase.Dry;
    }
}

[AddComponentMenu("Locomotion/Kitchen/Dispense Nozzle")]
public sealed class KitchenDispenseNozzle : MonoBehaviour
{
    public float open01 = 1f;
    public float angularDegrees;
    public float totalDispensed;

    public void Dispense(float amount)
    {
        totalDispensed += Mathf.Max(0f, amount) * Mathf.Clamp01(open01);
    }
}

[AddComponentMenu("Locomotion/Kitchen/Pan Oil Smoke Tracker")]
public sealed class PanOilSmokeTracker : MonoBehaviour
{
    public bool smokeEnabled = true;
    [Range(0f, 1f)] public float oil01;
    [Range(0f, 1f)] public float smoke01;

    public static void NotifyCook(GameObject station, ChefActivity mode, float dt)
    {
        if (station == null) return;
        var t = station.GetComponent<PanOilSmokeTracker>() ?? station.GetComponentInChildren<PanOilSmokeTracker>();
        if (t == null) t = station.AddComponent<PanOilSmokeTracker>();
        t.oil01 = Mathf.Clamp01(t.oil01 + dt * 0.02f);
        if (t.smokeEnabled && (mode == ChefActivity.Sear || mode == ChefActivity.Broil))
            t.smoke01 = Mathf.Clamp01(t.smoke01 + dt * 0.05f);
    }
}

/// <summary>Threat resolution via tool-use style water pour / extinguisher / grain.</summary>
public static class ThreatToolResolution
{
    public static bool TryResolve(ThreatCard card, GameObject actor, float pourLitersPerSec, float bucketLiters, bool hasExtinguisher, bool hasGrain)
    {
        if (card == null) return false;
        if (card.preferExtinguisher && hasExtinguisher) return true;
        if (card.preferGrainSmother && hasGrain) return true;
        if (card.requiredWaterPourLitersPerSec > 0f && pourLitersPerSec >= card.requiredWaterPourLitersPerSec)
        {
            if (card.requiredBucketVolumeLiters <= 0f || bucketLiters >= card.requiredBucketVolumeLiters)
                return true;
        }
        return false;
    }
}
