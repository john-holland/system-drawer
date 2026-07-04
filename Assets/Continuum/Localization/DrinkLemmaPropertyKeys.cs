/// <summary>Property keys for the built-in drink lemma verb.</summary>
public static class DrinkLemmaPropertyKeys
{
    public const string DrinkAnimationRef = "drink-animation-ref";
    public const string AutoMiddleMouthJaw = "auto-middle-mouth-jaw";
    public const string JawTiltAnimationAuditInsert = "jaw-tilt-animation-audit-insert";
    public const string HoldWithoutReturn = "hold-without-return";
    public const string PutWithoutRelease = "put-without-release";
    public const string NozzleLoopEnabled = "nozzle-loop-enabled";
    public const string LiquidSimulationEnabled = "liquid-simulation-enabled";
    public const string PlaceNozzleOnMouth = "place-nozzle-on-mouth";
    public const string DrinkEfficacy = "drink-efficacy";
    public const string SipCount = "sip-count";
    public const string TotalVolumeLiters = "total-volume-liters";
    public const string PartiallyRaiseAmount = "partially-raise-amount";
    public const string PartialRaiseDefaultWhenStalled = "partial-raise-default-when-stalled";
    public const string TrainForPerfectDrink = "train-for-perfect-drink";
    public const string MaxSpillLitersTolerance = "max-spill-liters-tolerance";
    public const string ClosureMode = "closure-mode";
    public const string MouthVolumeLitersTarget = "mouth-volume-liters-target";
    public const string InfiniteDrain = "infinite-drain";
    public const string InfiniteDrainClosureSeconds = "infinite-drain-closure-seconds";

    public const float DefaultDrinkEfficacy = 0.7f;
    public const int DefaultSipCount = 1;
    public const float DefaultPartialRaiseWhenStalled = 0.65f;
    public const float LitersToUsFlOz = 33.814f;

    public static readonly string[] AllKeys =
    {
        DrinkAnimationRef,
        AutoMiddleMouthJaw,
        JawTiltAnimationAuditInsert,
        HoldWithoutReturn,
        PutWithoutRelease,
        NozzleLoopEnabled,
        LiquidSimulationEnabled,
        PlaceNozzleOnMouth,
        DrinkEfficacy,
        SipCount,
        TotalVolumeLiters,
        PartiallyRaiseAmount,
        PartialRaiseDefaultWhenStalled,
        TrainForPerfectDrink,
        MaxSpillLitersTolerance,
        ClosureMode,
        MouthVolumeLitersTarget,
        InfiniteDrain,
        InfiniteDrainClosureSeconds,
    };
}

public enum DrinkClosureMode
{
    Auto,
    Mouth,
    EmptyVessel,
    Stalled,
    SpillBeat,
    InfiniteDrainBeat,
}

[System.Serializable]
public struct DrinkLemmaProperties
{
    public string drinkAnimationRef;
    public bool autoMiddleMouthJaw;
    public bool jawTiltAnimationAuditInsert;
    public bool holdWithoutReturn;
    public bool putWithoutRelease;
    public bool nozzleLoopEnabled;
    public bool liquidSimulationEnabled;
    public bool placeNozzleOnMouth;
    public float drinkEfficacy;
    public int sipCount;
    public float totalVolumeLiters;
    public float partiallyRaiseAmount;
    public float partialRaiseDefaultWhenStalled;
    public bool trainForPerfectDrink;
    public float maxSpillLitersTolerance;
    public DrinkClosureMode closureMode;
    public float mouthVolumeLitersTarget;
    public bool infiniteDrain;
    public float infiniteDrainClosureSeconds;

    public static DrinkLemmaProperties Defaults => new DrinkLemmaProperties
    {
        autoMiddleMouthJaw = true,
        drinkEfficacy = DrinkLemmaPropertyKeys.DefaultDrinkEfficacy,
        sipCount = DrinkLemmaPropertyKeys.DefaultSipCount,
        liquidSimulationEnabled = true,
        partiallyRaiseAmount = 1f,
        partialRaiseDefaultWhenStalled = DrinkLemmaPropertyKeys.DefaultPartialRaiseWhenStalled,
        maxSpillLitersTolerance = 0.05f,
        closureMode = DrinkClosureMode.Auto,
    };

    public float VolumePerSipLiters =>
        sipCount > 0 && totalVolumeLiters > 0f ? totalVolumeLiters / sipCount : 0f;

    public bool SuppressDispense =>
        partiallyRaiseAmount < 1f - 1e-4f && closureMode == DrinkClosureMode.Stalled;
}
