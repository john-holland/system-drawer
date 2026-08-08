using UnityEngine;

/// <summary>Lemma keys for boarding, runway description/alignment, baggage, and animal-cage peer.</summary>
public static class AirportLemmaPropertyKeys
{
    public const string BoardingPartyPrefix = "boarding-party-number-";
    public const string RunwayAlign = "runway_align";
    public const string RunwayDescribe = "runway_describe";
    public const string RunwayLargeParallel = "runway_large_parallel";
    public const string RunwaySmallSingle = "runway_small_single";
    public const string BaggageLoad = "baggage_load";
    public const string BaggageUnload = "baggage_unload";
    public const string CagePeer = "cage_peer";
    public const string AnimalInCage = "animal_in_cage";

    public static string BoardingParty(int number) => BoardingPartyPrefix + Mathf.Max(0, number);

    public static bool IsAirportLemma(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string k = key.ToLowerInvariant();
        return k.StartsWith(BoardingPartyPrefix)
               || k == RunwayAlign || k == RunwayDescribe
               || k == RunwayLargeParallel || k == RunwaySmallSingle
               || k == BaggageLoad || k == BaggageUnload
               || k == CagePeer || k == AnimalInCage;
    }
}
