using System.Collections.Generic;
using UnityEngine;

/// <summary>After meal/service: seed dirty dishes, emit season-pan + dishwashing cards.</summary>
public static class KitchenTearDownBranch
{
    public sealed class TearDownResult
    {
        public readonly List<ChefSeasonPanCard> seasonPanCards = new List<ChefSeasonPanCard>();
        public readonly List<DishwashingCard> dishwashingCards = new List<DishwashingCard>();
        public int dirtySeeded;
    }

    public static TearDownResult Run(
        KitchenTearDownSettings settings,
        DishWashingStation dishStation,
        GameObject actorOrVenue,
        IList<GameObject> soiledPans = null)
    {
        settings = settings ?? new KitchenTearDownSettings();
        var result = new TearDownResult();
        if (!settings.enableTearDown) return result;

        if (settings.emitSeasonPanCards)
        {
            if (soiledPans != null)
            {
                for (int i = 0; i < soiledPans.Count; i++)
                {
                    if (soiledPans[i] == null) continue;
                    var card = ChefSeasonPanCard.Generate(settings.seasonPanMode, soiledPans[i], settings.oilWipeAmount01);
                    ChefSeasonPanSolver.TrySolve(card, 0.1f, out _);
                    result.seasonPanCards.Add(card);
                }
            }
            else if (actorOrVenue != null)
            {
                var tracker = actorOrVenue.GetComponentInChildren<PanOilSmokeTracker>();
                if (tracker != null)
                {
                    var card = ChefSeasonPanCard.Generate(settings.seasonPanMode, tracker.gameObject, settings.oilWipeAmount01);
                    ChefSeasonPanSolver.TrySolve(card, 0.1f, out _);
                    result.seasonPanCards.Add(card);
                }
            }
        }

        if (settings.seedDirtyDishes && dishStation != null)
        {
            result.dirtySeeded = dishStation.SeedDirtyFromService(Mathf.Max(1, dishStation.pendingDirtySeed));
        }

        if (settings.emitDishwashingCards && dishStation != null)
        {
            var cards = ConsiderDishwashingCards.GeneratePreferredMoves(dishStation, maxCards: 8);
            result.dishwashingCards.AddRange(cards);
        }

        KitchenBioRhythmService.Instance?.NotifyCleanAttempt(0.05f);
        return result;
    }
}
