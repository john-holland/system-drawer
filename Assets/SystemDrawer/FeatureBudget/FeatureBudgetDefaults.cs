using System.Collections.Generic;

public static class FeatureBudgetDefaults
{
    public static readonly float[] GranularitySteps = { 1f, 0.65f, 0.35f, 0f };

    public static List<FeatureBudgetEntry> CreateDefaultEntries()
    {
        return new List<FeatureBudgetEntry>
        {
            Entry(FeatureBudgetIds.Weather, "Weather", 0,
                new[] { "WeatherSystem.", "WeatherPhysicsManifold." },
                new[] { FeatureBudgetRatioFieldIds.WeatherThickness, FeatureBudgetRatioFieldIds.AtmosCloudBase, FeatureBudgetRatioFieldIds.AtmosCloudTop }),
            Entry(FeatureBudgetIds.Planet, "Planet / Horizon", 1,
                new[] { "RebuildAll", "RebakeComposition" },
                new[] { FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, FeatureBudgetRatioFieldIds.HorizonDistanceKm,
                    FeatureBudgetRatioFieldIds.SdfNearFullKm, FeatureBudgetRatioFieldIds.SdfFarFullKm }),
            Entry(FeatureBudgetIds.PlanetSim, "Planet Simulation", 2,
                new[] { "PlateTectonics", "LavaAdvect" },
                new[] { FeatureBudgetRatioFieldIds.LavaThickness, FeatureBudgetRatioFieldIds.MantleThickness }),
            Entry(FeatureBudgetIds.AsteroidBelt, "Asteroid Belt", 3,
                new[] { "AsteroidBelt" },
                new[] { FeatureBudgetRatioFieldIds.HorizonDistanceKm }),
            Entry(FeatureBudgetIds.PlanetStreaming, "Planet Tile Streaming", 4,
                new[] { "PlanetTile", "RequestTile" },
                new[] { FeatureBudgetRatioFieldIds.SdfNearFullKm }),
            Entry(FeatureBudgetIds.Pathing, "Pathing / Travel", 5,
                new[] { "SyncRenderComponents", "TravelAgent", "HierarchicalPath", "BoidsCrowd", "AmbulationPathCache" },
                new[] { FeatureBudgetRatioFieldIds.HorizonDistanceKm }),
            Entry(FeatureBudgetIds.Networking, "Networking", 6,
                new[] { "NetworkLod", "TreeStream" }, null, false),
            Entry(FeatureBudgetIds.Spatial4D, "Spatial 4D", 7,
                new[] { "Spatial4D", "Bedoga" }, null, false),
            Entry(FeatureBudgetIds.Ragdoll, "Ragdoll / Animation", 8,
                new[] { "Ragdoll", "AnimationTree" }, null, false),
            Entry(FeatureBudgetIds.Narrative, "Narrative", 9,
                new[] { "Narrative", "Calendar" }, null, false),
            Entry(FeatureBudgetIds.DreamCycle, "Dream Cycle", 10,
                new[] { "DreamCycle" }, null, false),
            Entry(FeatureBudgetIds.Society, "Society / Political", 11,
                new[] { "PoliticalSim", "Society" }, null, false),
            Entry(FeatureBudgetIds.Usc, "USC Build", 12,
                new[] { "USC", "SemanticCompress" }, null, false),
            Entry(FeatureBudgetIds.CivilSystems, "Civil Systems / Persona Day", 13,
                new[] { "PersonaDay", "CivilVenue", "CivilSystem" }, null, true),
            Entry(FeatureBudgetIds.PixelLight, "PixelLight / Grid Slots", 14,
                new[] { "PixelLight", "PixelLightRig", "PixelLightOptic", "PixelLightGridMount" }, null, true),
            Entry(FeatureBudgetIds.TrainRail, "Train / Rail Vehicle", 15,
                new[] { "TrainVehicle", "TrainCoupling", "Rail" }, null, false),
            Entry(FeatureBudgetIds.CargoLash, "Cargo Lash / Stability", 16,
                new[] { "CargoLash", "CargoStability" }, null, true),
            Entry(FeatureBudgetIds.PlanarSplinePath, "Planar Spline Path", 17,
                new[] { "PlanarSplinePathLocomotion" }, null, true),
            Entry(FeatureBudgetIds.RailTrack, "Rail Track Structure", 18,
                new[] { "RailTrackStructure", "RailTrackFollow" }, null, true),
            Entry(FeatureBudgetIds.TrainDispatch, "Train Dispatch / Station", 19,
                new[] { "TrainStationBio", "TrainDispatch", "TrainEngineer" }, null, false),
            Entry(FeatureBudgetIds.Elevator, "Elevator Vehicle", 20,
                new[] { "ElevatorVehicle", "ElevatorButton", "ElevatorFloor" }, null, true),
            Entry(FeatureBudgetIds.Inspectors, "Inspectors", 21,
                new[] { "InspectorBio", "InspectorKnock", "JusticeDog" }, null, false),
            Entry(FeatureBudgetIds.PetWarden, "Pet Warden / Opinion", 22,
                new[] { "PetWarden", "RagdollOpinion" }, null, false),
            Entry(FeatureBudgetIds.GasStation, "Gas Station / Fuel", 23,
                new[] { "GasStation", "FuelPump", "GasRailRefuel" }, null, false),
            Entry(FeatureBudgetIds.Park, "Park / Walking Paths", 24,
                new[] { "ParkRuntime", "ParkBio", "LotGrass", "ParkPlant" }, null, true),
            Entry(FeatureBudgetIds.SanitationFacility, "Sanitation Facility", 25,
                new[] { "SanitationFacility", "SanitationSort", "PoopQuifer", "RoadCareCrew" }, null, false),
            Entry(FeatureBudgetIds.GarbageTruck, "Garbage Truck / TrashWarden", 26,
                new[] { "GarbageTruck", "TrashWarden", "GarbageBag", "TrashBin" }, null, false),
            Entry(FeatureBudgetIds.SewerGraph, "Sewer Graph / Drainage", 27,
                new[] { "SewerGraph", "SewerBuildingTap", "RoadsideDrain", "DryWell" }, null, true),
            Entry(FeatureBudgetIds.StreetBlocks, "Street Blocks Designer", 28,
                new[] { "StreetBlocks", "HeightMapInterior" }, null, true),
            Entry(FeatureBudgetIds.Factory, "Factory / Line", 29,
                new[] { "Factory", "FactoryLine", "FactoryBio" }, null, false),
            Entry(FeatureBudgetIds.ParkourFall, "Parkour Fall BT", 30,
                new[] { "ParkourFall", "ParkourLand" }, null, true),
            Entry(FeatureBudgetIds.Digging, "Digging / Tunnel SPH", 31,
                new[] { "Digging", "TopologicalDig", "TunnelSupport", "DigScoop" }, null, true),
            Entry(FeatureBudgetIds.VehicleDetect, "Vehicle YOLO Detect", 32,
                new[] { "VehicleDetect", "Yolo26Vehicle", "VehicleTrack" }, null, true),
            Entry(FeatureBudgetIds.PaintInk, "Pen and Ink", 33,
                new[] { "PenInk", "InkDrying", "QuillNib" }, null, true),
            Entry(FeatureBudgetIds.University, "University Campus", 34,
                new[] { "EducationWarden", "UniversityCampus", "CampusPixel" }, null, true),
            Entry(FeatureBudgetIds.LegalCourt, "Legal / Court", 35,
                new[] { "CourtWarden", "CorruptionWarden", "ConstitutionWarden", "RightsWarden", "LawWarden", "GovernmentWarden", "TheocraticWarden", "JusticeWarden", "GenevaConventionWarden", "LawTravelAgent", "ConversationBusTravelAgent", "LegalBuilding" }, null, true),
            Entry(FeatureBudgetIds.Relationship, "Relationship", 36,
                new[] { "LoveWarden", "RomanceWarden", "ConsentWarden", "RelationshipTravelAgent", "TheocraticWarden", "JusticeWarden" }, null, true),
            Entry(FeatureBudgetIds.Voting, "Voting / Polling Place", 37,
                new[] { "VotingPlace", "VoteBehaviorTree", "LaneGrid", "BallotUI", "GameSession" }, null, true),
        };
    }

    static FeatureBudgetEntry Entry(string id, string name, int rank, string[] prefixes, string[] ratioIds, bool aesthetic = true)
    {
        return new FeatureBudgetEntry
        {
            featureId = id,
            displayName = name,
            importanceRank = rank,
            controlMode = FeatureBudgetControlMode.Auto,
            manualEnabled = true,
            perfScopePrefixes = prefixes ?? System.Array.Empty<string>(),
            ratioFieldIds = ratioIds ?? System.Array.Empty<string>(),
            supportsAestheticGranularity = aesthetic
        };
    }

    public static List<FeatureBudgetRatioBinding> CreateDefaultRatioBindings()
    {
        return new List<FeatureBudgetRatioBinding>
        {
            Ratio(FeatureBudgetRatioFieldIds.HorizonFullSimRadiusKm, 0.002f, FeatureBudgetIds.Planet),
            Ratio(FeatureBudgetRatioFieldIds.HorizonDistanceKm, 0.004f, FeatureBudgetIds.Planet),
            Ratio(FeatureBudgetRatioFieldIds.SdfNearFullKm, 0.001f, FeatureBudgetIds.Planet),
            Ratio(FeatureBudgetRatioFieldIds.SdfFarFullKm, 0.004f, FeatureBudgetIds.Planet),
            Ratio(FeatureBudgetRatioFieldIds.WeatherThickness, 0.30f, FeatureBudgetIds.Weather),
            Ratio(FeatureBudgetRatioFieldIds.AtmosCloudBase, 0.10f, FeatureBudgetIds.Weather),
            Ratio(FeatureBudgetRatioFieldIds.AtmosCloudTop, 0.30f, FeatureBudgetIds.Weather),
            Ratio(FeatureBudgetRatioFieldIds.LavaThickness, 0.10f, FeatureBudgetIds.PlanetSim),
            Ratio(FeatureBudgetRatioFieldIds.MantleThickness, 0.60f, FeatureBudgetIds.PlanetSim),
        };
    }

    static FeatureBudgetRatioBinding Ratio(string fieldId, float ratio, string sourceFeatureId)
    {
        return new FeatureBudgetRatioBinding
        {
            fieldId = fieldId,
            ratio = ratio,
            ratioLocked = true,
            budgetGoverned = true,
            sourceFeatureId = sourceFeatureId,
            granularityLevel = 1f
        };
    }
}
