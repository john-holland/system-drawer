using UnityEngine;

/// <summary>Lemma keys for boarding, runway, baggage, airplane designer systems.</summary>
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

    public const string Checklist = "airplane_checklist";
    public const string LandingGear = "landing_gear";
    public const string Nose = "concorde_nose";
    public const string WebtopVideo = "webtop_video";
    public const string SeatbackWebtop = "seatback_webtop";
    public const string BathroomOccupied = "bathroom_occupied";
    public const string WeaponBay = "weapon_bay";
    public const string BatteryPower = "battery_power";
    public const string SeatOutlet = "seat_outlet";
    public const string SeatAux = "seat_aux";
    public const string PaSpeakers = "pa_speakers";
    public const string CabinMusicSource = "cabin_music_source";
    public const string TakeoffOverride = "takeoff_gear_override";
    public const string LandingOverride = "landing_gear_override";
    public const string DisasterDivertNearestAtc = "disaster_divert_nearest_atc";
    public const string AtcDefaultDestination = "atc_default_destination";
    public const string AtcDispatcherDialogue = "atc_dispatcher_dialogue";
    public const string LandingQueue = "landing_queue";
    public const string Refuel = "refuel";

    public static string BoardingParty(int number) => BoardingPartyPrefix + Mathf.Max(0, number);

    public static bool IsAirportLemma(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string k = key.ToLowerInvariant();
        return k.StartsWith(BoardingPartyPrefix)
               || k == RunwayAlign || k == RunwayDescribe
               || k == RunwayLargeParallel || k == RunwaySmallSingle
               || k == BaggageLoad || k == BaggageUnload
               || k == CagePeer || k == AnimalInCage
               || k == Checklist || k == LandingGear || k == Nose
               || k == WebtopVideo || k == SeatbackWebtop || k == BathroomOccupied
               || k == WeaponBay || k == BatteryPower || k == SeatOutlet || k == SeatAux
               || k == PaSpeakers || k == CabinMusicSource
               || k == TakeoffOverride || k == LandingOverride
               || k == DisasterDivertNearestAtc || k == AtcDefaultDestination
               || k == AtcDispatcherDialogue || k == LandingQueue || k == Refuel;
    }
}
