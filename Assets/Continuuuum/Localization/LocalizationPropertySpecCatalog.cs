using System.Collections.Generic;
using UnityEngine;

public static class LocalizationPropertyKeys
{
    public const string NonIkAnimation = "non-ik-animation";
}

[CreateAssetMenu(fileName = "LocalizationPropertySpec", menuName = "Continuuuum/Localization Property Spec")]
public sealed class LocalizationPropertySpecAsset : ScriptableObject
{
    public string key = "non-ik-animation";
    public string valueType = "Bool";
    public string[] allowedValues = { "true", "false" };
    public string defaultValue = "false";
    [TextArea] public string description = "When true, ragdoll playback uses kinematic Non-IK sampling instead of physics cards.";

    public LocalizationPropertySpecRecord ToRecord() => new LocalizationPropertySpecRecord
    {
        key = key,
        valueType = valueType,
        allowedValuesJson = allowedValues != null ? string.Join(",", allowedValues) : "",
        defaultValue = defaultValue,
        description = description
    };
}

[CreateAssetMenu(fileName = "LocalizationPropertySpecCatalog", menuName = "Continuuuum/Localization Property Spec Catalog")]
public sealed class LocalizationPropertySpecCatalog : ScriptableObject
{
    public List<LocalizationPropertySpecAsset> specs = new List<LocalizationPropertySpecAsset>();

    public bool TryGet(string key, out LocalizationPropertySpecAsset spec)
    {
        spec = null;
        if (string.IsNullOrEmpty(key) || specs == null)
            return false;
        foreach (var s in specs)
        {
            if (s != null && string.Equals(s.key, key, System.StringComparison.OrdinalIgnoreCase))
            {
                spec = s;
                return true;
            }
        }
        return false;
    }

    public static LocalizationPropertySpecRecord[] BuildDefaultRecords()
    {
        var list = new List<LocalizationPropertySpecRecord>
        {
            new LocalizationPropertySpecRecord
            {
                key = LocalizationPropertyKeys.NonIkAnimation,
                valueType = "Bool",
                allowedValuesJson = "[\"true\",\"false\"]",
                defaultValue = "false",
                description = "When true, ragdoll playback uses kinematic Non-IK sampling instead of physics cards."
            },
        };
        list.AddRange(BuildDrinkPropertyRecords());
        list.AddRange(BuildOpenClosePropertyRecords());
        return list.ToArray();
    }

    public static LocalizationPropertySpecRecord[] BuildOpenClosePropertyRecords() => new[]
    {
        Spec(OpenCloseLemmaPropertyKeys.OpenAngleDeg, "Float", "90", "Target hinge open angle in degrees"),
        Spec(OpenCloseLemmaPropertyKeys.DriveMode, "String", "hybrid", "Physics, animation, or hybrid drive"),
        Spec(OpenCloseLemmaPropertyKeys.ArrivalBlendCoefficient, "Float", "0", "0=stop-first, 1=reach-and-retry open"),
        Spec(OpenCloseLemmaPropertyKeys.ReachRadiusMeters, "Float", "0.6", "Handle reach radius for open attempts"),
        Spec(OpenCloseLemmaPropertyKeys.RequireFacingTarget, "Bool", "true", "Require facing target before open when blend < 1"),
        Spec(OpenCloseLemmaPropertyKeys.AutoCloseBt, "String", "on-stop-exit", "Auto-close BT compile mode"),
        Spec(OpenCloseLemmaPropertyKeys.AutoCloseOnExit, "Bool", "false", "Runtime close when leaving stop"),
        Spec(OpenCloseLemmaPropertyKeys.CompileCloseAmbulation, "Bool", "false", "Ambulate back before auto-close"),
        Spec(OpenCloseLemmaPropertyKeys.LinearOnly, "Bool", "false", "Ignore disabled topology branches"),
        Spec(OpenCloseLemmaPropertyKeys.QuestHintKind, "String", "none", "Quest hint on beat"),
        Spec(OpenCloseLemmaPropertyKeys.QuestObjectiveId, "String", "", "Quest objective id"),
        Spec(OpenCloseLemmaPropertyKeys.OpenAnimationRef, "String", "", "Open animation reference"),
        Spec(OpenCloseLemmaPropertyKeys.CloseAnimationRef, "String", "", "Close animation reference"),
        Spec(OpenCloseLemmaPropertyKeys.ClosureMode, "String", "auto", "Open/close beat closure mode"),
    };

    public static LocalizationPropertySpecRecord[] BuildDrinkPropertyRecords() => new[]
    {
        Spec(DrinkLemmaPropertyKeys.DrinkAnimationRef, "String", "", "Asset path or id for DrinkAnimationReference"),
        Spec(DrinkLemmaPropertyKeys.AutoMiddleMouthJaw, "Bool", "true", "Auto-align nozzle to middle mouth / jaw opening"),
        Spec(DrinkLemmaPropertyKeys.JawTiltAnimationAuditInsert, "Bool", "false", "Enable jaw-tilt keyframe audit and optional insertion"),
        Spec(DrinkLemmaPropertyKeys.HoldWithoutReturn, "Bool", "false", "Hold BT: skip return-to-rest cards"),
        Spec(DrinkLemmaPropertyKeys.PutWithoutRelease, "Bool", "false", "Put BT: skip release after placement"),
        Spec(DrinkLemmaPropertyKeys.NozzleLoopEnabled, "Bool", "false", "Optional continuous nozzle pour loop clip"),
        Spec(DrinkLemmaPropertyKeys.LiquidSimulationEnabled, "Bool", "true", "Enable local liquid sim on vessel"),
        Spec(DrinkLemmaPropertyKeys.PlaceNozzleOnMouth, "Bool", "false", "IK/orient: place nozzle on mouth"),
        Spec(DrinkLemmaPropertyKeys.DrinkEfficacy, "Float", "0.7", "Fraction of flow reaching mouth vs spill (0-1)"),
        Spec(DrinkLemmaPropertyKeys.SipCount, "Int", "1", "Number of sips to imbibe over"),
        Spec(DrinkLemmaPropertyKeys.TotalVolumeLiters, "Float", "0", "Target/stored volume in liters"),
        Spec(DrinkLemmaPropertyKeys.PartiallyRaiseAmount, "Float", "1", "Fraction of raise toward mouth/spout"),
        Spec(DrinkLemmaPropertyKeys.PartialRaiseDefaultWhenStalled, "Float", "0.65", "Partial raise when stalled"),
        Spec(DrinkLemmaPropertyKeys.TrainForPerfectDrink, "Bool", "false", "Zero spill training mode"),
        Spec(DrinkLemmaPropertyKeys.MaxSpillLitersTolerance, "Float", "0.05", "Spill cap when perfect drink"),
        Spec(DrinkLemmaPropertyKeys.ClosureMode, "String", "auto", "Beat closure mode"),
        Spec(DrinkLemmaPropertyKeys.MouthVolumeLitersTarget, "Float", "0", "Mouth volume closure target"),
        Spec(DrinkLemmaPropertyKeys.InfiniteDrain, "Bool", "false", "Never deplete vessel volume"),
        Spec(DrinkLemmaPropertyKeys.InfiniteDrainClosureSeconds, "Float", "0", "Infinite drain beat duration"),
    };

    static LocalizationPropertySpecRecord Spec(string key, string valueType, string defaultValue, string description) =>
        new LocalizationPropertySpecRecord
        {
            key = key,
            valueType = valueType,
            allowedValuesJson = valueType == "Bool" ? "[\"true\",\"false\"]" : null,
            defaultValue = defaultValue,
            description = description,
        };

    public static LocalizationPropertySpecCatalog CreateDefaultAsset()
    {
        var catalog = CreateInstance<LocalizationPropertySpecCatalog>();
        var spec = CreateInstance<LocalizationPropertySpecAsset>();
        catalog.specs.Add(spec);
        return catalog;
    }
}
