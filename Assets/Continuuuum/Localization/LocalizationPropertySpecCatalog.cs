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
        list.AddRange(BuildLifeSystemsPropertyRecords());
        list.AddRange(BuildInventoryWaypointPropertyRecords());
        list.AddRange(BuildNsmPropertyRecords());
        list.AddRange(BuildStuntSafetyPropertyRecords());
        list.AddRange(BuildWrestlingPropertyRecords());
        list.AddRange(BuildKissPropertyRecords());
        list.AddRange(BuildActionInputPropertyRecords());
        list.AddRange(BuildSpatialDescriptionPropertyRecords());
        list.AddRange(BuildChefPropertyRecords());
        list.AddRange(BuildThreatPropertyRecords());
        list.AddRange(BuildTasteNotesPropertyRecords());
        return list.ToArray();
    }

    public static LocalizationPropertySpecRecord[] BuildTasteNotesPropertyRecords() => new[]
    {
        Spec(TasteNotesLemmaPropertyKeys.SpecNotes, "String", "sour,spicy", "Taste notes csv: sour|spicy|sweet|bitter|umami|salty"),
        Spec(TasteNotesLemmaPropertyKeys.SpecIntensity, "Float", "0.5", "Taste intensity 0-1"),
    };

    public static LocalizationPropertySpecRecord[] BuildChefPropertyRecords() => new[]
    {
        Spec(ChefLemmaPropertyKeys.SpecOp, "String", "duty", "chef op: duty|activity|wash|ticket"),
        Spec(ChefLemmaPropertyKeys.SpecActivity, "String", "sear", "ChefActivity: sear|pour|filet|stir|..."),
        Spec(ChefLemmaPropertyKeys.SpecMode, "String", "Line", "ChefDutyMode: Prep|Line|Pass|Expo|Dish|Hygiene"),
        Spec(ChefLemmaPropertyKeys.SpecStation, "String", "", "Station / context id"),
        Spec(ChefLemmaPropertyKeys.SpecItem, "String", "", "Ingredient or tool name"),
        Spec(ChefLemmaPropertyKeys.SpecOrder, "String", "", "Order ticket id"),
    };

    public static LocalizationPropertySpecRecord[] BuildThreatPropertyRecords() => new[]
    {
        Spec(ThreatLemmaPropertyKeys.SpecOp, "String", "raise", "threat op: raise|clear|query|dialog"),
        Spec(ThreatLemmaPropertyKeys.SpecLevel, "String", "localized", "Threat level tag"),
        Spec(ThreatLemmaPropertyKeys.SpecAlert, "String", "on-edge", "Alert: on-edge|all-clear|under-attack|..."),
        Spec(ThreatLemmaPropertyKeys.SpecAgency, "String", "kitchen", "Agency id"),
        Spec(ThreatLemmaPropertyKeys.SpecKind, "String", "generic", "ThreatKind"),
        Spec(ThreatLemmaPropertyKeys.SpecLemma, "String", "on-edge", "Alertness lemma tag"),
    };

    public static LocalizationPropertySpecRecord[] BuildActionInputPropertyRecords() => new[]
    {
        Spec(ActionInputLemmaPropertyKeys.Id, "String", "",
            "{P:action|id=jump} action id (alias: action)"),
        Spec(ActionInputLemmaPropertyKeys.MapsTo, "String", "",
            "Control token: x, Space, KEY_UP, MOUSE_0, X_AXIS (aliases: to, map)"),
        Spec(ActionInputLemmaPropertyKeys.Subscribe, "String", "KEY_DOWN",
            "Edge: KEY_DOWN|KEY_UP|KEY_HELD|AXIS (aliases: edge, on)"),
        Spec(ActionInputLemmaPropertyKeys.AndMapsTo, "String", "",
            "Additional OR-bound control token (alias: also)"),
        Spec(ActionInputLemmaPropertyKeys.Clear, "Bool", "false",
            "Clear existing bindings for this action before apply"),
    };

    public static LocalizationPropertySpecRecord[] BuildKissPropertyRecords() => new[]
    {
        Spec(LoveMakingKissLemmaPropertyKeys.KissAnimation, "String", "",
            "{P:kiss|kiss-animation=key} explicit kiss animation (e.g. slimer-kiss)"),
        Spec(LoveMakingKissLemmaPropertyKeys.KissAnimationIntensity, "Float", "0.35",
            "0–1 kiss intensity (peck→making out); maps to LoveCard.kissAnimationIntensity"),
    };

    public static LocalizationPropertySpecRecord[] BuildWrestlingPropertyRecords() => new[]
    {
        Spec(WrestlingLemmaPropertyKeys.SpecMode, "String", "Play", "Wrestling mode: Play|Subdue|Pin"),
        Spec(WrestlingLemmaPropertyKeys.SpecMove, "String", "", "Wrestling move kind (LockGrapple, Throw, ...)"),
        Spec(WrestlingLemmaPropertyKeys.SpecProfessional, "Bool", "false", "Prefer kayfabe / .pro animation tags"),
    };

    public static LocalizationPropertySpecRecord[] BuildStuntSafetyPropertyRecords() => new[]
    {
        Spec("stunt-max-risk", "Float", "0.3", "Stuntman maxRisk01 planner band"),
        Spec("stunt-min-risk", "Float", "", "Stuntman minRisk01 planner band"),
        Spec("safely-risk-min", "Float", "0.1", "{P:safely|riskMin} band"),
        Spec("safely-safety-min", "Float", "0.9", "{P:safely|safetyMin} band"),
        Spec("safely-safety-max", "Float", "0.9", "{P:safely|safetyMax} ⇒ min risk complementary"),
        Spec("stunt-anim-group", "String", "", "Parkour / rope animation group tag"),
    };

    public static LocalizationPropertySpecRecord[] BuildNsmPropertyRecords() => new[]
    {
        Spec(NsmLemmaPropertyKeys.SpecPrime, "Bool", "false", "Entry is an NSM semantic prime"),
        Spec(NsmLemmaPropertyKeys.SpecGroup, "String", "", "NSM prime group (substantive, time, logical, ...)"),
        Spec(NsmLemmaPropertyKeys.SpecDefinition, "String", "", "Gloss / ostensive note"),
        Spec(NsmLemmaPropertyKeys.SpecLogicalForm, "Json", "{}", "Math/predicate AST JSON"),
        Spec(NsmLemmaPropertyKeys.SpecCausalityRole, "String", "none", "none|causal|conditional|negation|temporal|modal"),
        Spec(NsmLemmaPropertyKeys.SpecTemporalRole, "String", "none", "none|when|now|before|after|duration|moment|place_time"),
        Spec(NsmLemmaPropertyKeys.SpecFuzzyHedge, "String", "", "Hedge id / phrase key"),
        Spec(NsmLemmaPropertyKeys.SpecFuzzyCurve, "Json", "", "Override membership curve params"),
        Spec(NsmLemmaPropertyKeys.SpecCausalityTree, "String", "", "Causality tree / composition note"),
    };

    public static LocalizationPropertySpecRecord[] BuildSpatialDescriptionPropertyRecords() => new[]
    {
        Spec("spatial-description", "String", "", "Description / place key for SG paint and filters"),
        Spec("spatial-skin-key", "String", "", "Stylesheet / skin key override"),
        Spec("spatial-adj-paint", "String", "", "Adjective term for ShaderGrammarIndex paint"),
    };

    public static LocalizationPropertySpecRecord[] BuildInventoryWaypointPropertyRecords() => new[]
    {
        Spec("inv-op", "String", "have", "inventory op: have|give|take|transfer|assert|putaway"),
        Spec("inv-item", "String", "", "Loadout item name"),
        Spec("inv-from", "String", "", "Source actor id"),
        Spec("inv-to", "String", "", "Target actor id"),
        Spec("inv-context", "String", "", "Put-away context GameObject name / path"),
        Spec("wp-name", "String", "A", "Waypoint name / id"),
        Spec("wp-x", "Float", "0", "Waypoint X"),
        Spec("wp-y", "Float", "0", "Waypoint Y"),
        Spec("wp-z", "Float", "0", "Waypoint Z"),
        Spec("wp-formation", "String", "triangle", "Formation id for leg"),
    };

    public static LocalizationPropertySpecRecord[] BuildLifeSystemsPropertyRecords() => new[]
    {
        Spec(LifeSystemsLemmaPropertyKeys.SpecOp, "String", "query", "life op: set|adjust|query|buff|illness|organ"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecChannel, "String", "", "Channel id (depression, immune, heart_rate, ...)"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecValue, "Float", "0", "Absolute 0-1 (or clinical mapped) set value"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecDelta, "Float", "0", "Channel or organ raw delta"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecDuration, "Float", "0", "Effect duration seconds (0=until cleared)"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecQuery, "String", "mood", "Query target: mood|organ|channel id"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecOrganId, "String", "heart", "Organ id for organ op/query"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecLifeForce, "Float", "0", "Life force delta for buff"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecBioRhythm, "Float", "0", "Bio rhythm amplitude delta"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecLabel, "String", "", "Effect label"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecDifficulty, "String", "normal", "easy|normal"),
        Spec(LifeSystemsLemmaPropertyKeys.SpecRaw, "Bool", "false", "Prefer raw organ values in queries"),
    };

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
