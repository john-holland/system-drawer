/// <summary>Lemma keys for train cars, limbs, containment, lash stability, fold failure.</summary>
public static class TrainCarLemmaPropertyKeys
{
    public const string ContainedVehicle = "train_car.contained_vehicle";
    public const string LimbState = "train_car.limb_state";
    public const string LashStable01 = "train_car.lash_stable01";
    public const string ImpossibleKeepStable = "train_car.impossible_keep_stable";
    public const string FoldFailed = "train_fold_failed";
    public const string ConsistId = "train_car.consist_id";
    public const string BayId = "train_car.bay_id";
    public const string LimbRole = "train_car.limb_role";
    public const string StabilityMode = "train_car.stability_mode";

    public static bool IsTrainCarLemma(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        string k = key.ToLowerInvariant().Replace('-', '.');
        return k == ContainedVehicle || k == LimbState || k == LashStable01
               || k == ImpossibleKeepStable || k == FoldFailed
               || k == ConsistId || k == BayId || k == LimbRole || k == StabilityMode
               || k.StartsWith("train_car.");
    }
}

public static class TrainCarNarrativeActionIds
{
    public const string UnfoldLimb = "train_unfold_limb";
    public const string RefoldLimb = "train_refold_limb";
    public const string UnloadBay = "train_unload_bay";
    public const string ParkVehicle = "train_park_vehicle";
    public const string FoldFailed = "train_fold_failed";
    public const string Couple = "train_couple";
    public const string Decouple = "train_decouple";
    public const string SwapCar = "train_swap_car";
    public const string SiloLoad = "train_silo_load";
    public const string DepotReplace = "train_depot_replace";
}
