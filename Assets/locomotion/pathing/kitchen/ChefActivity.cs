/// <summary>Chef duty / cooking activity kinds for ChefCard.</summary>
public enum ChefActivity
{
    Idle,
    Filet,
    Spread,
    Dispense,
    Sprinkle,
    Pour,
    Shake,
    Drop,
    Cut,
    Place,
    Throw,
    Stir,
    Hold,
    Sear,
    Broil,
    Bake,
    Boil,
    Plating,
    WashHands,
    CleanStation,
    SeasonPan,
    WashDish
}

/// <summary>High-level kitchen duty mode (station context).</summary>
public enum ChefDutyMode
{
    Prep,
    Line,
    Pass,
    Expo,
    Dish,
    Hygiene
}
