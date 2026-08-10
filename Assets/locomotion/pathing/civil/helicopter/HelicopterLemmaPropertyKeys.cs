/// <summary>Lemma keys for helicopter / magneto / GPS portal / road-lot heli pads.</summary>
public static class HelicopterLemmaPropertyKeys
{
    public const string MagnetoCollective = "magneto_collective";
    public const string MagnetoCyclic = "magneto_cyclic";
    public const string TailRudder = "tail_rudder";
    public const string Acceleration = "heli_acceleration";
    public const string AirBrake = "heli_air_brake";
    public const string GpsHud = "gps_hud";
    public const string GpsHudBaked = "gps_hud_baked";
    public const string GpsHudRealtime = "gps_hud_realtime";
    public const string PortalBounds2 = "portal_bounds2";
    public const string Flap = "magneto_flap";
    public const string Winglet = "magneto_winglet";
    public const string LandingGear = "heli_landing_gear";
    public const string Door = "heli_door";
    public const string RoadLotPad = "road_lot_pad";

    public static bool IsHelicopterLemma(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string k = key.ToLowerInvariant();
        return k == MagnetoCollective || k == MagnetoCyclic || k == TailRudder
               || k == Acceleration || k == AirBrake
               || k == GpsHud || k == GpsHudBaked || k == GpsHudRealtime || k == PortalBounds2
               || k == Flap || k == Winglet || k == LandingGear || k == Door || k == RoadLotPad;
    }
}

public static class HelicopterNarrativeActionIds
{
    public const string Takeoff = "heli_takeoff";
    public const string Landing = "heli_landing";
    public const string GearUp = "heli_gear_up";
    public const string GearDown = "heli_gear_down";
    public const string GpsOpen = "heli_gps_open";
    public const string GpsClose = "heli_gps_close";
    public const string PortalBounds = "heli_portal_bounds2";
    public const string MagnetoShed = "heli_magneto_efficacy";
    public const string CutGrass = "lot_grass_cut";
}
