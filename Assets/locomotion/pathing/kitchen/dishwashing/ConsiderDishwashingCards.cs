using System.Collections.Generic;
using UnityEngine;

/// <summary>Prefer Dish duty near pit; legal Hanoi moves maximizing Dry output.</summary>
public static class ConsiderDishwashingCards
{
    public static List<DishwashingCard> GeneratePreferredMoves(DishWashingStation station, int maxCards = 6)
    {
        var cards = new List<DishwashingCard>();
        if (station == null) return cards;
        station.EnsureZones();
        bool compost = station.config != null && station.config.enableCompostZone;
        var finish = station.config != null ? station.config.finishPreference : DishFinishPreference.Either;

        TryAdd(station, cards, DishZoneKind.Dirty, DishZoneKind.Sink, finish, compost);
        if (finish == DishFinishPreference.DryingRack || finish == DishFinishPreference.Either)
            TryAdd(station, cards, DishZoneKind.Sink, DishZoneKind.Dry, finish, compost);
        if (finish == DishFinishPreference.Dishwasher || finish == DishFinishPreference.Either)
        {
            TryAdd(station, cards, DishZoneKind.Sink, DishZoneKind.Dishwasher, finish, compost);
            TryAdd(station, cards, DishZoneKind.Dishwasher, DishZoneKind.Dry, finish, compost);
        }
        if (compost)
            TryAdd(station, cards, DishZoneKind.Dirty, DishZoneKind.Compost, finish, compost);

        if (cards.Count > maxCards)
            cards.RemoveRange(maxCards, cards.Count - maxCards);
        return cards;
    }

    static void TryAdd(
        DishWashingStation station,
        List<DishwashingCard> cards,
        DishZoneKind from,
        DishZoneKind to,
        DishFinishPreference finish,
        bool compost)
    {
        if (!DishWashingStation.IsLegalMove(from, to, compost)) return;
        if (!station.TryPeekTop(from, out string dishId)) return;
        var card = DishwashingCard.Generate(dishId, from, to, finish);
        if (station.config != null)
        {
            card.scrubSeconds = station.config.defaultScrubSeconds;
            card.rinseLiters = station.config.rinseLiters;
            card.scrubMode = station.config.scrubMode;
        }
        cards.Add(card);
    }
}
