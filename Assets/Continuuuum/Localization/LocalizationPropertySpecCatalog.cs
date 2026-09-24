using System;
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
        list.AddRange(BuildChatPropertyRecords());
        list.AddRange(BuildSpatialDescriptionPropertyRecords());
        list.AddRange(BuildChefPropertyRecords());
        list.AddRange(BuildHousingPropertyRecords());
        list.AddRange(BuildThreatPropertyRecords());
        list.AddRange(BuildStreetLightPropertyRecords());
        list.AddRange(BuildTasteNotesPropertyRecords());
        list.AddRange(BuildTrainCarPropertyRecords());
        list.AddRange(BuildPenInkPropertyRecords());
        list.AddRange(BuildUniversityPropertyRecords());
        list.AddRange(BuildScribePropertyRecords());
        list.AddRange(BuildRelationshipPropertyRecords());
        list.AddRange(BuildLegalPropertyRecords());
        list.AddRange(BuildVotePropertyRecords());
        list.AddRange(BuildGameSessionPropertyRecords());
        list.AddRange(BuildUtilityPropertyRecords());
        list.AddRange(BuildFrameShellInclusionPropertyRecords());
        list.AddRange(BuildSewingPropertyRecords());
        return list.ToArray();
    }

    public static LocalizationPropertySpecRecord[] BuildFrameShellInclusionPropertyRecords() => new[]
    {
        Spec(FrameShellInclusionLemmaPropertyKeys.Inclusion, "String", "shell",
            "{P:sewing-machine|inclusion=shell} Bounds4 inclusion: frame|shell"),
        Spec(FrameShellInclusionLemmaPropertyKeys.FrameInclusion, "String", "frame",
            "Frame member-tube inclusion"),
        Spec(FrameShellInclusionLemmaPropertyKeys.ShellInclusion, "String", "shell",
            "Shell filled-cavity inclusion"),
        Spec(FrameShellInclusionLemmaPropertyKeys.FrameId, "String", "",
            "PixelLight / door frame id (alias: frameId)"),
        Spec(FrameShellInclusionLemmaPropertyKeys.DoorId, "String", "",
            "Hinged door id (alias: doorId)"),
        Spec(FrameShellInclusionLemmaPropertyKeys.HingeLabel, "String", "left",
            "Hinge side: left|right|front|rear|bottom (alias: hingeLabel)"),
        Spec(FrameShellInclusionLemmaPropertyKeys.Hollow, "String", "",
            "Hollow subtract slot id"),
        Spec(FrameShellInclusionLemmaPropertyKeys.HollowSubtract, "String", "",
            "PixelLightGridSlotKind.HollowSubtract"),
        Spec(FrameShellInclusionLemmaPropertyKeys.SlotKind, "String", "light",
            "light|hollow-subtract|door"),
        Spec(FrameShellInclusionLemmaPropertyKeys.ZIndex, "Integer", "0",
            "Subtract / stack order (alias: zIndex)"),
        Spec(FrameShellInclusionLemmaPropertyKeys.HollowRadius, "Float", "0.02",
            "Hollow / door opening radius meters"),
    };

    public static LocalizationPropertySpecRecord[] BuildSewingPropertyRecords() => new[]
    {
        Spec(SewingLemmaPropertyKeys.SewingMachine, "String", "", "Sewing machine Frame/Shell spec"),
        Spec(SewingLemmaPropertyKeys.Serger, "String", "", "Serger Frame/Shell spec"),
        Spec(SewingLemmaPropertyKeys.Lathe, "String", "", "Lathe Frame/Shell spec"),
        Spec(SewingLemmaPropertyKeys.StitchProgram, "String", "lockstitch", "lockstitch|overlock stitch program"),
        Spec(SewingLemmaPropertyKeys.Lockstitch, "String", "", "Sewing lockstitch program"),
        Spec(SewingLemmaPropertyKeys.Overlock, "String", "", "Serger overlock program"),
        Spec(SewingLemmaPropertyKeys.Hem, "String", "", "ClothSplineKind.Hem path"),
        Spec(SewingLemmaPropertyKeys.NeedleThroat, "String", "", "Needle throat hollow"),
        Spec(SewingLemmaPropertyKeys.BobbinRace, "String", "", "Bobbin race hollow"),
        Spec(SewingLemmaPropertyKeys.DoorBobbin, "String", "", "Bobbin door id"),
        Spec(SewingLemmaPropertyKeys.DoorLooper, "String", "", "Serger looper door id"),
        Spec(SewingLemmaPropertyKeys.DoorHeadstock, "String", "", "Lathe headstock door id"),
        Spec(SewingLemmaPropertyKeys.SpindleBore, "String", "", "Lathe spindle bore hollow"),
    };

    public static LocalizationPropertySpecRecord[] BuildUtilityPropertyRecords() => new[]
    {
        Spec(UtilityLemmaPropertyKeys.Furnace, "String", "", "Basement furnace"),
        Spec(UtilityLemmaPropertyKeys.WaterHeater, "String", "", "Tank water heater"),
        Spec(UtilityLemmaPropertyKeys.WaterMain, "String", "", "City water main"),
        Spec(UtilityLemmaPropertyKeys.Shutoff, "String", "", "Building water shutoff"),
        Spec(UtilityLemmaPropertyKeys.WaterFilter, "String", "", "Utility filter bank"),
        Spec(UtilityLemmaPropertyKeys.Hvac, "String", "", "HVAC plant"),
        Spec(UtilityLemmaPropertyKeys.Utility, "String", "", "Utility room / comfort"),
        Spec(UtilityLemmaPropertyKeys.CircuitBreaker, "String", "", "100 A service panel"),
        Spec(UtilityLemmaPropertyKeys.WallPlug, "String", "", "Wall receptacle"),
        Spec(UtilityLemmaPropertyKeys.JacobsLadder, "String", "", "Recoup-axis gunk freeer"),
        Spec(UtilityLemmaPropertyKeys.Recoup, "String", "", "Recoup wheel energy"),
        Spec(UtilityLemmaPropertyKeys.Imitirrrr, "String", "", "Submerged silicon recoup wheel"),
        Spec(UtilityLemmaPropertyKeys.Flood, "String", "", "Basement standing flood"),
        Spec(UtilityLemmaPropertyKeys.Gunk, "String", "", "Jacobs-ladder trap fill"),
        Spec(UtilityLemmaPropertyKeys.SumpPump, "String", "", "Basement pit pump"),
        Spec(UtilityLemmaPropertyKeys.Drain, "String", "", "Sump / SPH drain"),
    };

    public static LocalizationPropertySpecRecord[] BuildVotePropertyRecords() => new[]
    {
        Spec(VoteLemmaPropertyKeys.Vote, "String", "", "Vote run / ballot cast"),
        Spec(VoteLemmaPropertyKeys.Ballot, "String", "", "Ballot spec id"),
        Spec(VoteLemmaPropertyKeys.Recount, "String", "", "Recount run id"),
        Spec(VoteLemmaPropertyKeys.Tally, "Integer", "0", "Tally hash / count"),
        Spec(VoteLemmaPropertyKeys.Queue, "String", "", "Polling queue / feeder lane"),
        Spec(VoteLemmaPropertyKeys.Queued, "String", "", "Queued-by-address policy"),
        Spec(VoteLemmaPropertyKeys.Address, "String", "", "Street / civic address"),
        Spec(VoteLemmaPropertyKeys.HomeAddress, "String", "", "homeAddress property on the voter"),
        Spec(VoteLemmaPropertyKeys.Randomly, "String", "", "Random feeder when no home address"),
        Spec(VoteLemmaPropertyKeys.Happily, "String", "", "Adverb that can take postfix if-so"),
        Spec(VoteLemmaPropertyKeys.IfSo, "String", "", "Postfix anaphor; if is also prefix / infix / circumfix"),
        Spec(VoteLemmaPropertyKeys.Property, "String", "", "Named civic property key"),
    };

    public static LocalizationPropertySpecRecord[] BuildGameSessionPropertyRecords() => new[]
    {
        Spec(GameSessionLemmaPropertyKeys.GameSession, "String", "", "GameSession id inside lobby"),
        Spec(GameSessionLemmaPropertyKeys.Saving, "String", "", "Saving a game session"),
        Spec(GameSessionLemmaPropertyKeys.Loading, "String", "", "Loading a game session"),
        Spec(GameSessionLemmaPropertyKeys.LocalSave, "String", "", "Save to local client"),
        Spec(GameSessionLemmaPropertyKeys.SaveServerToLocal, "String", "", "Copy server structure to local"),
        Spec(GameSessionLemmaPropertyKeys.LocalServer, "String", "", "Local server structure"),
    };

    public static LocalizationPropertySpecRecord[] BuildLegalPropertyRecords() => new[]
    {
        Spec(LegalLemmaPropertyKeys.Court, "String", "american", "Court kind / courthouse id"),
        Spec(LegalLemmaPropertyKeys.Constitution, "Float", "1", "Constitution allow 0-1"),
        Spec(LegalLemmaPropertyKeys.Scripture, "String", "", "Active scripture refs"),
        Spec(LegalLemmaPropertyKeys.Chamber, "String", "house", "Legislative chamber"),
        Spec(LegalLemmaPropertyKeys.Rights, "Float", "1", "Rights allow 0-1"),
        Spec(LegalLemmaPropertyKeys.Law, "String", "", "Law card / statute id"),
        Spec(LegalLemmaPropertyKeys.Junta, "Float", "0", "Junta suspend constitution 0-1"),
        Spec(LegalLemmaPropertyKeys.GenevaConventions, "String", "", "Geneva Convention Warden"),
        Spec(LegalLemmaPropertyKeys.Torture, "Bool", "false", "ThreatWarden isTorture"),
        Spec(LegalLemmaPropertyKeys.RespectsGenevaConventions, "Bool", "true", "Junta / prison respects Geneva Conventions"),
        Spec(LegalLemmaPropertyKeys.Announce, "String", "", "Announce a civic / legal event"),
        Spec(LegalLemmaPropertyKeys.Returned, "String", "", "A right or article returned"),
        Spec(LegalLemmaPropertyKeys.RightsReturned, "Bool", "false", "Rights restored after limit or removal"),
        Spec(LegalLemmaPropertyKeys.AnnounceRightsReturned, "String", "", "ConstitutionWarden AnnounceRightsReturned event"),
    };

    public static LocalizationPropertySpecRecord[] BuildRelationshipPropertyRecords() => new[]
    {
        Spec(RelationshipLemmaPropertyKeys.Stage, "String", "notion", "Relationship stage / RomanceSeverity"),
        Spec(RelationshipLemmaPropertyKeys.Consent, "Float", "1", "Consent allow 0-1"),
        Spec(RelationshipLemmaPropertyKeys.Doctrine, "Float", "1", "Theocratic doctrine allow 0-1"),
        Spec(RelationshipLemmaPropertyKeys.Subjects, "String", "", "Relationship subject ids"),
        Spec(RelationshipLemmaPropertyKeys.Affection, "Float", "0.5", "Love/romance affection 0-1"),
        Spec(RelationshipLemmaPropertyKeys.Romance, "String", "", "Romance profile / route id"),
    };

    public static LocalizationPropertySpecRecord[] BuildScribePropertyRecords() => new[]
    {
        Spec(ScribeLemmaPropertyKeys.ScribeSet, "String", "", "Scribe document config / set id"),
        Spec(ScribeLemmaPropertyKeys.Page, "Integer", "0", "Scribe page index"),
        Spec(ScribeLemmaPropertyKeys.Anchor, "String", "", "Page anchor key"),
        Spec(ScribeLemmaPropertyKeys.Format, "String", "plain", "plain|markdown|odt|docx|pdf|lemma"),
        Spec(ScribeLemmaPropertyKeys.PeckingOrder, "Integer", "20", "Scribe pecking order"),
    };

    public static LocalizationPropertySpecRecord[] BuildUniversityPropertyRecords() => new[]
    {
        Spec(UniversityLemmaPropertyKeys.Campus, "String", "", "University campus id"),
        Spec(UniversityLemmaPropertyKeys.Curriculum, "String", "", "Curriculum asset / course catalog id"),
        Spec(UniversityLemmaPropertyKeys.CourseLoad, "String", "", "Shared course load id (teacher + assistant)"),
        Spec(UniversityLemmaPropertyKeys.AgeBracket, "String", "undergrad", "lower-school|upper-school|undergrad|graduate"),
        Spec(UniversityLemmaPropertyKeys.Dorm, "String", "", "Room-and-board dorm id"),
        Spec(UniversityLemmaPropertyKeys.Enroll, "Bool", "true", "Enroll eligible student"),
    };

    public static LocalizationPropertySpecRecord[] BuildPenInkPropertyRecords() => new[]
    {
        Spec(PenInkLemmaPropertyKeys.Paintlike, "Bool", "false", "Paintlike ink keeps stacked films instead of single-layer mix"),
        Spec(PenInkLemmaPropertyKeys.Dilution, "Float", "0.75", "Ink dilution into the top wet layer 0-1"),
        Spec(PenInkLemmaPropertyKeys.SingleLayerMix, "Bool", "true", "Lerp incoming pigment into the top wet layer"),
        Spec(PenInkLemmaPropertyKeys.MaxBendDeg, "Float", "10", "Quill nib max page bend in degrees"),
        Spec(PenInkLemmaPropertyKeys.SeeThroughSec, "Float", "30", "See-through dry window in seconds"),
        Spec(PenInkLemmaPropertyKeys.Aperture, "Float", "0.0008", "Nib / nozzle aperture radius meters"),
        Spec(PenInkLemmaPropertyKeys.CapOpen, "Bool", "true", "Pen cap open"),
    };

    public static LocalizationPropertySpecRecord[] BuildTrainCarPropertyRecords() => new[]
    {
        Spec("train_car.contained_vehicle", "String", "", "Vehicle id parked in train car bay"),
        Spec("train_car.limb_state", "String", "Folded", "Limb state: Folded|Unfolding|Unfolded|Refolding|Failed"),
        Spec("train_car.lash_stable01", "Float", "1", "Live cargo/limb lash stability 0-1"),
        Spec("train_car.impossible_keep_stable", "Bool", "false", "Impossible physics keep vehicle/limb section stable"),
        Spec("train_fold_failed", "Bool", "false", "Fold/unfold topology failure leaf"),
        Spec("train_car.consist_id", "String", "", "Consist / snake formation id"),
        Spec("train_car.bay_id", "String", "deck", "Containment bay id"),
        Spec("train_car.limb_role", "String", "Crane", "Limb role: Crane|DigArm|Loader|Generic"),
        Spec("train_car.stability_mode", "String", "Nominal", "Nominal|SoftLash|ImpossibleKeepStable"),
    };

    public static LocalizationPropertySpecRecord[] BuildStreetLightPropertyRecords() => new[]
    {
        Spec(StreetLightLemmaPropertyKeys.SpecChangedTo, "String", "red", "changed-to color: red|green|yellow|amber"),
        Spec(StreetLightLemmaPropertyKeys.SpecRed, "Bool", "true", "Predicate/control red signal"),
        Spec(StreetLightLemmaPropertyKeys.SpecGreen, "Bool", "true", "Predicate/control green signal"),
        Spec(StreetLightLemmaPropertyKeys.SpecYellow, "Bool", "true", "Predicate/control yellow/amber signal"),
    };

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

    public static LocalizationPropertySpecRecord[] BuildHousingPropertyRecords() => new[]
    {
        Spec(HousingLemmaPropertyKeys.SpecSize, "String", "good_size", "quaint|good_size|mc_mansion|mansion|cabin|cottage|townhome"),
        Spec(HousingLemmaPropertyKeys.SpecStyle, "String", "", "Freeform architecture style tag"),
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

    public static LocalizationPropertySpecRecord[] BuildChatPropertyRecords() => new[]
    {
        Spec(ChatLemmaPropertyKeys.Op, "String", "open",
            "{P:chat|op=open} open|close|toggle (aliases: op, action). Placeholders open-chat/close-chat/dismiss infer op."),
        Spec(ChatLemmaPropertyKeys.ProductId, "String", "",
            "Saurce product id for lexicon/history (alias: product)"),
        Spec(ChatLemmaPropertyKeys.SessionId, "String", "",
            "Chat session id (alias: session)"),
        Spec(ChatLemmaPropertyKeys.ComposeMode, "String", "preview",
            "preview|sendButton"),
        Spec(ChatLemmaPropertyKeys.Surface, "String", "unity-mp-text",
            "Structural chat surface id; Continuuuum tools stay unrated (alias: surface)"),
        Spec(ChatLemmaPropertyKeys.AutoCloseOnExit, "Bool", "false",
            "Close chat when leaving the bound stop / scene"),
        Spec(ChatLemmaPropertyKeys.RequireEntitlement, "Bool", "true",
            "Block open unless chat entitlement is granted"),
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

// <auto-merged-lemma-keys>
// Merged because Unity AssetDatabase omitted these scripts after pull.

// ---- from Assets/Continuuuum/Localization/ChatLemmaPropertyKeys.cs ----
/// <summary>Property keys for {P:chat|op=open} / {P:open-chat} / {P:close-chat} spans.</summary>
public static class ChatLemmaPropertyKeys
{
    public const string Op = "chat-op";
    public const string AliasOp = "op";
    public const string AliasAction = "action";
    public const string ProductId = "product-id";
    public const string AliasProduct = "product";
    public const string SessionId = "session-id";
    public const string AliasSession = "session";
    public const string ComposeMode = "compose-mode";
    public const string Surface = "chat-surface";
    public const string AliasSurface = "surface";
    public const string AutoCloseOnExit = "auto-close-on-exit";
    public const string RequireEntitlement = "require-entitlement";

    public static readonly string[] LemmaPlaceholders =
    {
        "chat", "open-chat", "close-chat", "dismiss"
    };

    public static readonly string[] AllKeys =
    {
        Op, ProductId, SessionId, ComposeMode, Surface, AutoCloseOnExit, RequireEntitlement
    };
}

public enum ChatLemmaOp
{
    Open,
    Close,
    Toggle
}

[Serializable]
public struct ChatLemmaProperties
{
    public ChatLemmaOp op;
    public string productId;
    public string sessionId;
    public string composeMode;
    public string surface;
    public bool autoCloseOnExit;
    public bool requireEntitlement;
    public string lemmaHint;

    public static ChatLemmaProperties Defaults => new ChatLemmaProperties
    {
        op = ChatLemmaOp.Open,
        productId = "",
        sessionId = "",
        composeMode = "",
        surface = "unity-mp-text",
        autoCloseOnExit = false,
        requireEntitlement = true,
        lemmaHint = "chat"
    };

    public static bool IsChatLemma(string placeholderName)
    {
        if (string.IsNullOrEmpty(placeholderName))
            return false;
        string n = NormalizeName(placeholderName);
        for (int i = 0; i < ChatLemmaPropertyKeys.LemmaPlaceholders.Length; i++)
        {
            if (n == ChatLemmaPropertyKeys.LemmaPlaceholders[i])
                return true;
        }
        return false;
    }

    public static ChatLemmaProperties ResolveFromParams(
        Dictionary<string, string> parameters,
        string placeholderName = "chat")
    {
        var p = Defaults;
        p.lemmaHint = placeholderName ?? "chat";
        p.op = InferOp(placeholderName);
        if (parameters == null)
            return p;

        if (Try(parameters, ChatLemmaPropertyKeys.Op, out var op) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasOp, out op) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasAction, out op))
            p.op = ParseOp(op, p.op);

        if (Try(parameters, ChatLemmaPropertyKeys.ProductId, out var pid) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasProduct, out pid))
            p.productId = pid;

        if (Try(parameters, ChatLemmaPropertyKeys.SessionId, out var sid) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasSession, out sid))
            p.sessionId = sid;

        if (Try(parameters, ChatLemmaPropertyKeys.ComposeMode, out var mode))
            p.composeMode = mode;

        if (Try(parameters, ChatLemmaPropertyKeys.Surface, out var surface) ||
            Try(parameters, ChatLemmaPropertyKeys.AliasSurface, out surface))
            p.surface = surface;

        if (Try(parameters, ChatLemmaPropertyKeys.AutoCloseOnExit, out var autoClose))
            p.autoCloseOnExit = ParseBool(autoClose);

        if (Try(parameters, ChatLemmaPropertyKeys.RequireEntitlement, out var req))
            p.requireEntitlement = ParseBool(req);

        return p;
    }

    public static ChatLemmaOp InferOp(string placeholderName)
    {
        string n = NormalizeName(placeholderName);
        if (n == "close-chat" || n == "dismiss")
            return ChatLemmaOp.Close;
        if (n == "open-chat")
            return ChatLemmaOp.Open;
        return ChatLemmaOp.Open;
    }

    // todo: review: add send, and refine open to use join as a separate lemma
    public static ChatLemmaOp ParseOp(string raw, ChatLemmaOp fallback = ChatLemmaOp.Open)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;
        string n = NormalizeName(raw);
        switch (n)
        {
            case "close":
            case "dismiss":
            case "leave":
            case "hang-up":
                return ChatLemmaOp.Close;
            case "toggle":
            case "flip":
                return ChatLemmaOp.Toggle;
            case "open":
            case "join":
            case "show":
                return ChatLemmaOp.Open;
            default:
                return fallback;
        }
    }

    static string NormalizeName(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";
        return raw.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
    }

    static bool ParseBool(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return false;
        return raw == "1" ||
               string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(raw, "yes", StringComparison.OrdinalIgnoreCase);
    }

    static bool Try(Dictionary<string, string> p, string key, out string v)
    {
        v = null;
        foreach (var kv in p)
        {
            if (string.Equals(kv.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                v = kv.Value;
                return !string.IsNullOrEmpty(v);
            }
        }
        return false;
    }
}


// ---- from Assets/Continuuuum/Localization/ChefLemmaPropertyKeys.cs ----
/// <summary>Property keys for {P:chef|...} / {P:cook|...} lemma painting.</summary>
public static class ChefLemmaPropertyKeys
{
    public const string PlaceholderName = "chef";
    public const string CookPlaceholderName = "cook";
    public const string Op = "op";
    public const string Activity = "activity";
    public const string Mode = "mode";
    public const string Station = "station";
    public const string Item = "item";
    public const string Order = "order";

    public const string SpecOp = "chef-op";
    public const string SpecActivity = "chef-activity";
    public const string SpecMode = "chef-mode";
    public const string SpecStation = "chef-station";
    public const string SpecItem = "chef-item";
    public const string SpecOrder = "chef-order";

    public static readonly string[] AllKeys = { Op, Activity, Mode, Station, Item, Order };
}

public enum ChefLemmaOp
{
    None,
    Duty,
    Activity,
    Wash,
    Ticket
}

[Serializable]
public struct ChefLemmaProperties
{
    public ChefLemmaOp op;
    public string activity;
    public string mode;
    public string station;
    public string item;
    public string orderId;

    public static ChefLemmaProperties ResolveFromParams(System.Collections.Generic.IReadOnlyDictionary<string, string> p)
    {
        var props = new ChefLemmaProperties();
        if (p == null) return props;
        if (p.TryGetValue(ChefLemmaPropertyKeys.Op, out var op))
        {
            if (string.Equals(op, "duty", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Duty;
            else if (string.Equals(op, "activity", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Activity;
            else if (string.Equals(op, "wash", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Wash;
            else if (string.Equals(op, "ticket", StringComparison.OrdinalIgnoreCase)) props.op = ChefLemmaOp.Ticket;
        }
        p.TryGetValue(ChefLemmaPropertyKeys.Activity, out props.activity);
        p.TryGetValue(ChefLemmaPropertyKeys.Mode, out props.mode);
        p.TryGetValue(ChefLemmaPropertyKeys.Station, out props.station);
        p.TryGetValue(ChefLemmaPropertyKeys.Item, out props.item);
        p.TryGetValue(ChefLemmaPropertyKeys.Order, out props.orderId);
        return props;
    }
}


// ---- from Assets/Continuuuum/Localization/FrameShellInclusionLemmaPropertyKeys.cs ----
/// <summary>
/// Canonical lemmas for Bounds4 Frame/Shell inclusion and PixelLight hollow/door slot identity.
/// Slot catalog ids use underscores; <see cref="ToSlotId"/> / <see cref="FromSlotId"/> convert.
/// </summary>
public static class FrameShellInclusionLemmaPropertyKeys
{
    public const string Frame = "frame";
    public const string Shell = "shell";
    public const string Inclusion = "inclusion";
    public const string FrameInclusion = "frame-inclusion";
    public const string ShellInclusion = "shell-inclusion";
    public const string Hollow = "hollow";
    public const string HollowSubtract = "hollow-subtract";
    public const string FrameId = "frame-id";
    public const string DoorId = "door-id";
    public const string HingeLabel = "hinge-label";
    public const string SlotKind = "slot-kind";
    public const string ZIndex = "z-index";
    public const string HollowRadius = "hollow-radius";

    public const string HingeLeft = "left";
    public const string HingeRight = "right";
    public const string HingeFront = "front";
    public const string HingeRear = "rear";
    public const string HingeBottom = "bottom";

    public static readonly string[] LemmaPlaceholders =
    {
        Frame, Shell, Inclusion, FrameInclusion, ShellInclusion, Hollow, HollowSubtract,
        FrameId, DoorId, HingeLabel, SlotKind, ZIndex, HollowRadius
    };

    public static string ToSlotId(string lemma)
    {
        if (string.IsNullOrEmpty(lemma)) return lemma;
        return lemma.Replace('-', '_');
    }

    public static string FromSlotId(string slotId)
    {
        if (string.IsNullOrEmpty(slotId)) return slotId;
        return slotId.Replace('_', '-');
    }

    public static bool IsFrameInclusion(string lemma)
    {
        string t = BuiltInSynonyms.CanonicalizeToken(lemma ?? "");
        return t == Frame || t == FrameInclusion;
    }

    public static bool IsShellInclusion(string lemma)
    {
        string t = BuiltInSynonyms.CanonicalizeToken(lemma ?? "");
        return t == Shell || t == ShellInclusion || t == Inclusion;
    }
}


// ---- from Assets/Continuuuum/Localization/GameSessionLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for GameSession save/load and local server structure.</summary>
public static class GameSessionLemmaPropertyKeys
{
    public const string GameSession = "game-session";
    public const string Saving = "saving";
    public const string Loading = "loading";
    public const string LocalSave = "local-save";
    public const string SaveServerToLocal = "save-server-to-local";
    public const string LocalServer = "local-server";

    public static readonly string[] LemmaPlaceholders =
    {
        "game-session", "saving", "loading", "local-save", "save-server-to-local", "local-server"
    };
}


// ---- from Assets/Continuuuum/Localization/HousingLemmaPropertyKeys.cs ----
/// <summary>Property keys for {P:house|...} architecture / size lemma painting.</summary>
public static class HousingLemmaPropertyKeys
{
    public const string PlaceholderName = "house";
    public const string Size = "size";
    public const string Style = "style";
    public const string SpecSize = "house-size";
    public const string SpecStyle = "house-style";

    public static readonly string[] SizeTokens =
    {
        "quaint", "good_size", "mc_mansion", "mansion", "cabin", "cottage", "townhome"
    };

    public static readonly string[] AllKeys = { Size, Style };
}


// ---- from Assets/Continuuuum/Localization/LegalLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for court, constitution, scripture, and chambers.</summary>
public static class LegalLemmaPropertyKeys
{
    public const string Court = "court";
    public const string Constitution = "constitution";
    public const string Scripture = "scripture";
    public const string Chamber = "chamber";
    public const string Rights = "rights";
    public const string Law = "law";
    public const string Junta = "junta";
    public const string GenevaConventions = "geneva-conventions";
    public const string Torture = "torture";
    public const string RespectsGenevaConventions = "respects-geneva-conventions";
    public const string Announce = "announce";
    public const string Returned = "returned";
    public const string RightsReturned = "rights-returned";
    public const string AnnounceRightsReturned = "announce-rights-returned";

    public static readonly string[] LemmaPlaceholders =
    {
        "court", "constitution", "scripture", "chamber", "rights", "law", "junta",
        "geneva-conventions", "torture", "respects-geneva-conventions",
        "announce", "returned", "rights-returned", "announce-rights-returned"
    };
}


// ---- from Assets/Continuuuum/Localization/PenInkLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for pen, quill, nib, ink, and cap open/close.</summary>
public static class PenInkLemmaPropertyKeys
{
    public const string Pen = "pen";
    public const string Quill = "quill";
    public const string Nib = "nib";
    public const string Ink = "ink";
    public const string Write = "write";
    public const string Dip = "dip";
    public const string Cap = "cap";
    public const string Open = "open";
    public const string Close = "close";
    public const string Wet = "wet";
    public const string Dry = "dry";
    public const string Paint = "paint";
    public const string Towel = "towel";
    public const string Whiteboard = "whiteboard";

    public static readonly string[] LemmaPlaceholders =
    {
        "pen", "quill", "nib", "ink", "write", "dip", "cap",
        "open", "close", "wet", "dry", "paint", "towel", "whiteboard"
    };

    public const string Paintlike = "paintlike";
    public const string Dilution = "dilution";
    public const string SingleLayerMix = "single-layer-mix";
    public const string MaxBendDeg = "max-bend-deg";
    public const string SeeThroughSec = "see-through-sec";
    public const string Aperture = "aperture";
    public const string CapOpen = "cap-open";
}


// ---- from Assets/Continuuuum/Localization/RelationshipLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for relationship stage / consent / doctrine / subjects.</summary>
public static class RelationshipLemmaPropertyKeys
{
    public const string Stage = "stage";
    public const string Consent = "consent";
    public const string Doctrine = "doctrine";
    public const string Subjects = "subjects";
    public const string Affection = "affection";
    public const string Romance = "romance";

    public static readonly string[] LemmaPlaceholders =
    {
        "stage", "consent", "doctrine", "subjects", "affection", "romance"
    };
}


// ---- from Assets/Continuuuum/Localization/RoadLaneLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for road lanes, sidewalks, wires, signs, emergency bars.</summary>
public static class RoadLaneLemmaPropertyKeys
{
    public const string RoadLane = "road_lane";
    public const string Sidewalk = "sidewalk";
    public const string Crosswalk = "crosswalk";
    public const string Curb = "curb";
    public const string GrassStrip = "grass_strip";
    public const string PhonePole = "phone_pole";
    public const string StreetWire = "street_wire";
    public const string WireEnd = "wire_end";
    public const string HangingShoes = "hanging_shoes";
    public const string WalkButton = "walk_button";
    public const string Intersection = "intersection";
    public const string RoadSign = "road_sign";
    public const string JerseyBarrier = "jersey_barrier";
    public const string GuardRail = "guard_rail";
    public const string EmergencyBar = "emergency_bar";
    public const string StreetLuminaire = "street_luminaire";

    public static readonly string[] LemmaPlaceholders =
    {
        "road-lane", "sidewalk", "crosswalk", "curb", "grass-strip",
        "phone-pole", "street-wire", "wire-end", "hanging-shoes", "walk-button",
        "intersection", "road-sign", "jersey-barrier", "guard-rail",
        "emergency-bar", "street-luminaire", "street-light", "traffic-signal"
    };

    public const string LaneIndex = "lane-index";
    public const string Grid = "grid";
    public const string FollowTime = "follow-time";
    public const string Disabled = "disabled";
    public const string Open = "open";
    public const string Padded = "padded";
    public const string Matting = "matting";
    public const string WalkAcross = "walk-across";
    public const string Hold = "hold";
    public const string Width = "width";
    public const string Dapple = "dapple";
    public const string Span = "span";
    public const string Occupied = "occupied";
    public const string PoleId = "pole-id";
    public const string WireId = "wire-id";
    public const string Tension = "tension";
    public const string Down = "down";
    public const string Kind = "kind";
    public const string Stuck = "stuck";
    public const string Draped = "draped";
    public const string KnotLength = "knot-length";
    public const string Snapped = "snapped";
    public const string Pressed = "pressed";
    public const string Held = "held";
    public const string Approach = "approach";
    public const string Yield = "yield";
    public const string StopPotential = "stop-potential";
    public const string Read = "read";
    public const string BendWithRoad = "bend-with-road";
    public const string LaneDisabled = "lane-disabled";
    public const string On = "on";
    public const string Blink = "blink";
    public const string Color = "color";
    public const string Hear = "hear";
    public const string See = "see";
    public const string Tracked = "tracked";
}


// ---- from Assets/Continuuuum/Localization/ScribeLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for scribe documents / pages / anchors.</summary>
public static class ScribeLemmaPropertyKeys
{
    public const string ScribeSet = "scribe-set";
    public const string Page = "page";
    public const string Anchor = "anchor";
    public const string Format = "format";
    public const string PeckingOrder = "pecking-order";

    public static readonly string[] LemmaPlaceholders =
    {
        "scribe-set", "page", "anchor", "format", "pecking-order"
    };
}


// ---- from Assets/Continuuuum/Localization/SewingLemmaPropertyKeys.cs ----
/// <summary>
/// Lemma keys for sewing machine, serger, and lathe Frame/Shell PixelLight machines.
/// Catalog slot ids are underscore forms of these hyphenated lemmas.
/// </summary>
public static class SewingLemmaPropertyKeys
{
    public const string SewingMachine = "sewing-machine";
    public const string Serger = "serger";
    public const string Lathe = "lathe";
    public const string Stitch = "stitch";
    public const string StitchProgram = "stitch-program";
    public const string Lockstitch = "lockstitch";
    public const string Overlock = "overlock";
    public const string Hem = "hem";
    public const string Seam = "seam";
    public const string Needle = "needle";
    public const string Bobbin = "bobbin";
    public const string Looper = "looper";
    public const string Presser = "presser";
    public const string Hook = "hook";
    public const string NeedleThroat = "needle-throat";
    public const string BobbinRace = "bobbin-race";
    public const string ThreadPath = "thread-path";
    public const string LooperRace = "looper-race";
    public const string DoorBobbin = "door-bobbin";
    public const string DoorBed = "door-bed";
    public const string DoorLooper = "door-looper";
    public const string SewingShell = "sewing-shell";
    public const string SewingFrame = "sewing-frame";
    public const string SergerShell = "serger-shell";
    public const string LooperUpper = "looper-upper";
    public const string LooperLower = "looper-lower";
    public const string Differential = "differential";
    public const string SpindleBore = "spindle-bore";
    public const string TailstockQuill = "tailstock-quill";
    public const string ChipChute = "chip-chute";
    public const string DoorHeadstock = "door-headstock";
    public const string DoorGearbox = "door-gearbox";
    public const string DoorChipPan = "door-chip-pan";
    public const string LatheFrameBed = "lathe-frame-bed";
    public const string LatheShellCover = "lathe-shell-cover";
    public const string Headstock = "headstock";
    public const string Tailstock = "tailstock";
    public const string Sew = "sew";
    public const string Serge = "serge";

    public static readonly string[] LemmaPlaceholders =
    {
        SewingMachine, Serger, Lathe, Stitch, StitchProgram, Lockstitch, Overlock,
        Hem, Seam, Needle, Bobbin, Looper, Presser, Hook,
        NeedleThroat, BobbinRace, ThreadPath, LooperRace,
        DoorBobbin, DoorBed, DoorLooper,
        SewingShell, SewingFrame, SergerShell,
        LooperUpper, LooperLower, Differential,
        SpindleBore, TailstockQuill, ChipChute,
        DoorHeadstock, DoorGearbox, DoorChipPan,
        LatheFrameBed, LatheShellCover, Headstock, Tailstock,
        Sew, Serge
    };
}


// ---- from Assets/Continuuuum/Localization/StreetLightLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for describing / controlling street &amp; traffic lights.</summary>
public static class StreetLightLemmaPropertyKeys
{
    public const string PlaceholderName = "street_light";
    public const string TrafficSignal = "traffic_signal";

    public const string ChangedTo = "changed-to";
    public const string Red = "red";
    public const string Green = "green";
    public const string Yellow = "yellow";
    public const string Amber = "amber";

    public const string SpecChangedTo = "street-light-changed-to";
    public const string SpecRed = "street-light-red";
    public const string SpecGreen = "street-light-green";
    public const string SpecYellow = "street-light-yellow";

    public static readonly string[] AllKeys =
    {
        ChangedTo, Red, Green, Yellow, Amber
    };
}

public enum StreetLightLemmaOp
{
    None,
    ChangedTo,
    SetRed,
    SetGreen,
    SetYellow
}

[Serializable]
public struct StreetLightLemmaProperties
{
    public StreetLightLemmaOp op;
    public string color;

    public static StreetLightLemmaProperties Defaults => new StreetLightLemmaProperties
    {
        op = StreetLightLemmaOp.None,
        color = "red"
    };
}


// ---- from Assets/Continuuuum/Localization/TasteNotesLemmaPropertyKeys.cs ----
/// <summary>Property keys for {P:taste|notes=sour,spicy|intensity=0.5} lemma painting.</summary>
public static class TasteNotesLemmaPropertyKeys
{
    public const string PlaceholderName = "taste";
    public const string Notes = "notes";
    public const string Intensity = "intensity";

    public const string SpecNotes = "taste-notes";
    public const string SpecIntensity = "taste-intensity";

    public static readonly string[] AllKeys = { Notes, Intensity };
}

[Serializable]
public struct TasteNotesLemmaProperties
{
    public string notesCsv;
    public float intensity01;

    public static TasteNotesLemmaProperties Defaults => new TasteNotesLemmaProperties
    {
        notesCsv = "",
        intensity01 = 0.5f
    };
}


// ---- from Assets/Continuuuum/Localization/ThreatLemmaPropertyKeys.cs ----
/// <summary>Property keys for {P:threat|...} alertness lemmas.</summary>
public static class ThreatLemmaPropertyKeys
{
    public const string PlaceholderName = "threat";
    public const string Op = "op";
    public const string Level = "level";
    public const string Alert = "alert";
    public const string Agency = "agency";
    public const string Kind = "kind";
    public const string Lemma = "lemma";

    public const string SpecOp = "threat-op";
    public const string SpecLevel = "threat-level";
    public const string SpecAlert = "threat-alert";
    public const string SpecAgency = "threat-agency";
    public const string SpecKind = "threat-kind";
    public const string SpecLemma = "threat-lemma";

    public static readonly string[] LemmaTags =
    {
        "on-edge", "all-clear", "under-attack", "potential-intruders", "advisory", "elevated"
    };

    public static readonly string[] AllKeys = { Op, Level, Alert, Agency, Kind, Lemma };
}

public enum ThreatLemmaOp
{
    None,
    Raise,
    Clear,
    Query,
    Dialog
}

[Serializable]
public struct ThreatLemmaProperties
{
    public ThreatLemmaOp op;
    public string level;
    public string alert;
    public string agency;
    public string kind;
    public string lemma;

    public static ThreatLemmaProperties ResolveFromParams(System.Collections.Generic.IReadOnlyDictionary<string, string> p)
    {
        var props = new ThreatLemmaProperties();
        if (p == null) return props;
        if (p.TryGetValue(ThreatLemmaPropertyKeys.Op, out var op))
        {
            if (string.Equals(op, "raise", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Raise;
            else if (string.Equals(op, "clear", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Clear;
            else if (string.Equals(op, "query", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Query;
            else if (string.Equals(op, "dialog", StringComparison.OrdinalIgnoreCase)) props.op = ThreatLemmaOp.Dialog;
        }
        p.TryGetValue(ThreatLemmaPropertyKeys.Level, out props.level);
        p.TryGetValue(ThreatLemmaPropertyKeys.Alert, out props.alert);
        p.TryGetValue(ThreatLemmaPropertyKeys.Agency, out props.agency);
        p.TryGetValue(ThreatLemmaPropertyKeys.Kind, out props.kind);
        p.TryGetValue(ThreatLemmaPropertyKeys.Lemma, out props.lemma);
        return props;
    }
}


// ---- from Assets/Continuuuum/Localization/UniversityLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for university / boarding campus.</summary>
public static class UniversityLemmaPropertyKeys
{
    public const string Campus = "campus";
    public const string Curriculum = "curriculum";
    public const string Headmaster = "headmaster";
    public const string Dean = "dean";
    public const string Dorm = "dorm";
    public const string CourseLoad = "course-load";
    public const string AgeBracket = "age-bracket";
    public const string Teacher = "teacher";
    public const string Assistant = "assistant";
    public const string Enroll = "enroll";

    public static readonly string[] LemmaPlaceholders =
    {
        "campus", "curriculum", "headmaster", "dean", "dorm",
        "course-load", "age-bracket", "teacher", "assistant", "enroll"
    };
}


// ---- from Assets/Continuuuum/Localization/UtilityLemmaPropertyKeys.cs ----
/// <summary>Canonical lemma keys for basement utility / water / flood vocabulary.</summary>
public static class UtilityLemmaPropertyKeys
{
    public const string Furnace = "furnace";
    public const string WaterHeater = "water-heater";
    public const string WaterMain = "water-main";
    public const string Shutoff = "shutoff";
    public const string WaterFilter = "water-filter";
    public const string Hvac = "hvac";
    public const string Utility = "utility";
    public const string CircuitBreaker = "circuit-breaker";
    public const string WallPlug = "wall-plug";
    public const string JacobsLadder = "jacobs-ladder";
    public const string Recoup = "recoup";
    public const string Imitirrrr = "imitirrrr";
    public const string ImitirrrrId = "imitirrrr__";
    public const string Flood = "flood";
    public const string Gunk = "gunk";
    public const string SumpPump = "sump-pump";
    public const string Drain = "drain";

    public static readonly string[] LemmaPlaceholders =
    {
        Furnace, WaterHeater, WaterMain, Shutoff, WaterFilter, Hvac, Utility,
        CircuitBreaker, WallPlug, JacobsLadder, Recoup, Imitirrrr, Flood, Gunk,
        SumpPump, Drain
    };
}


// ---- from Assets/Continuuuum/Localization/VoteLemmaPropertyKeys.cs ----
/// <summary>Lemma keys for ballots, tallies, recounts, and queue-by-address in-paint.</summary>
public static class VoteLemmaPropertyKeys
{
    public const string Vote = "vote";
    public const string Ballot = "ballot";
    public const string Recount = "recount";
    public const string Tally = "tally";
    public const string Queue = "queue";
    public const string Queued = "queued";
    public const string Address = "address";
    public const string HomeAddress = "home-address";
    public const string Randomly = "randomly";
    public const string Happily = "happily";
    public const string IfSo = "if-so";
    public const string Property = "property";

    /// <summary>Default developer in-paint on the local voting-place SG node. <c>if</c> is prefix / infix / postfix / circumfix; anaphor <c>if so</c> after an adverb postfixes.</summary>
    public const string DefaultInpaintPrompt = "queued by address, or randomly, if so";

    public static readonly string[] LemmaPlaceholders =
    {
        "vote", "ballot", "recount", "tally",
        "queue", "queued", "address", "home-address", "randomly", "happily", "if-so", "property"
    };
}
