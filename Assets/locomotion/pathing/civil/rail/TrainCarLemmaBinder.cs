using UnityEngine;

/// <summary>Applies lemma tokens to train car stability / fold predicates for player lashing decisions.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Car Lemma Binder")]
public sealed class TrainCarLemmaBinder : MonoBehaviour
{
    public TrainCarVehicleRagdoll car;

    void Awake()
    {
        if (car == null)
            car = GetComponent<TrainCarVehicleRagdoll>();
    }

    public void ApplyToken(string token)
    {
        if (car == null || string.IsNullOrEmpty(token)) return;
        var t = token.ToLowerInvariant().Replace('-', '_').Replace('.', '_');
        if (t.Contains("impossible") || t.Contains("unbreakable") || t.Contains("keep_stable"))
        {
            car.defaultStabilityMode = CargoStabilityMode.ImpossibleKeepStable;
            if (car.lashRuntime != null)
            {
                car.lashRuntime.mode = CargoStabilityMode.ImpossibleKeepStable;
                car.lashRuntime.ApplyProfile(car.lashRuntime.profile, CargoStabilityMode.ImpossibleKeepStable);
            }
            for (int i = 0; i < car.containmentBays.Count; i++)
                if (car.containmentBays[i] != null)
                    car.containmentBays[i].stabilityMode = CargoStabilityMode.ImpossibleKeepStable;
            for (int i = 0; i < car.limbs.Count; i++)
                if (car.limbs[i] != null)
                    car.limbs[i].stabilityMode = CargoStabilityMode.ImpossibleKeepStable;
        }
        else if (t.Contains("soft_lash"))
        {
            car.defaultStabilityMode = CargoStabilityMode.SoftLash;
        }
        else if (t.Contains("nominal"))
        {
            car.defaultStabilityMode = CargoStabilityMode.Nominal;
        }
    }

    public float QueryLashStable01() => car != null ? car.LastLashStable01 : 1f;

    public bool QueryImpossibleKeepStable() =>
        car != null && car.defaultStabilityMode == CargoStabilityMode.ImpossibleKeepStable;

    public bool QueryFoldFailed() => car != null && car.LastFoldFailed;
}
