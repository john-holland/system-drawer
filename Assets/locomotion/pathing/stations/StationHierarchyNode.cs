using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hierarchical station placard in scene — binds SG4D causality leaf + company/gov building/vehicle.
/// </summary>
[AddComponentMenu("Locomotion/Stations/Station Hierarchy Node")]
public sealed class StationHierarchyNode : MonoBehaviour
{
    public string stableId;
    public string displayName;
    public StationKind kind = StationKind.Generic;
    public string parentStableId;
    public string causalityLeafId;
    public string levelId = "default";
    public string cityId = "demo-city";
    public StationConfig config = new StationConfig();

    void Awake()
    {
        if (string.IsNullOrEmpty(stableId))
            stableId = gameObject.name;
        if (string.IsNullOrEmpty(displayName))
            displayName = gameObject.name;
        if (config == null)
            config = new StationConfig();
        StationRegistry.Instance?.Register(this);
        TryBridge();
    }

    void OnDestroy()
    {
        StationRegistry.Instance?.Unregister(this);
    }

    public void TryBridge()
    {
        if (kind == StationKind.Cooking)
        {
            if (config.kitchenVenue == null)
                config.kitchenVenue = GetComponent<RestaurantVenueRuntime>()
                                     ?? GetComponentInChildren<RestaurantVenueRuntime>();
            BridgeToCivilVenue();
        }
        else if (kind == StationKind.Computer)
        {
            if (config.computerStation == null)
                config.computerStation = GetComponent<ComputerPeripheryStation>()
                                        ?? GetComponentInChildren<ComputerPeripheryStation>();
        }
        else if (kind == StationKind.Train)
        {
            if (config.trainStation == null)
                config.trainStation = GetComponent<TrainStationRuntime>()
                                     ?? GetComponentInChildren<TrainStationRuntime>()
                                     ?? gameObject.AddComponent<TrainStationRuntime>();
            BridgeTrainCivilVenue(CivilSystemKind.TrainStation, "train_station");
        }
        else if (kind == StationKind.Silo)
        {
            if (config.grainSilo == null)
                config.grainSilo = GetComponent<GrainSiloStubRuntime>()
                                  ?? GetComponentInChildren<GrainSiloStubRuntime>()
                                  ?? gameObject.AddComponent<GrainSiloStubRuntime>();
            BridgeTrainCivilVenue(CivilSystemKind.GrainSilo, "grain_silo");
        }
        else if (kind == StationKind.RailMaintenance)
        {
            if (config.railMaintenanceDepot == null)
                config.railMaintenanceDepot = GetComponent<RailMaintenanceDepotStub>()
                                             ?? GetComponentInChildren<RailMaintenanceDepotStub>()
                                             ?? gameObject.AddComponent<RailMaintenanceDepotStub>();
            BridgeTrainCivilVenue(CivilSystemKind.RailMaintenanceDepot, "rail_maintenance_depot");
        }
        else if (kind == StationKind.VotingBooth)
        {
            if (config.votingBooth == null)
                config.votingBooth = GetComponent<VotingBoothStation>()
                                    ?? GetComponentInChildren<VotingBoothStation>();
        }
    }

    void BridgeTrainCivilVenue(CivilSystemKind civilKind, string buildingTypeId)
    {
        var pdm = PersonaDayManager.Instance;
        if (pdm == null) return;
        var existing = pdm.lattice.Get(stableId);
        if (existing != null) return;
        var node = new CivilVenueNode
        {
            stableId = stableId,
            kind = civilKind,
            contextOwner = gameObject,
            buildingTypeId = buildingTypeId
        };
        pdm.RegisterVenue(node);
    }

    void BridgeToCivilVenue()
    {
        var pdm = PersonaDayManager.Instance;
        if (pdm == null) return;
        var existing = pdm.lattice.Get(stableId);
        if (existing != null) return;
        var node = new CivilVenueNode
        {
            stableId = stableId,
            kind = CivilSystemKind.Kitchen,
            contextOwner = gameObject,
            buildingTypeId = "restaurant",
            kitchenRuntime = config.kitchenVenue,
            kitchenBio = GetComponent<KitchenBioRhythmService>()
                         ?? GetComponentInChildren<KitchenBioRhythmService>()
        };
        pdm.RegisterVenue(node);
    }

    public static string KindToApi(StationKind kind) => kind.ToString().ToLowerInvariant();

    public Dictionary<string, object> ToPlacardDto()
    {
        var commodities = new List<object>();
        if (config?.commodities != null)
        {
            for (int i = 0; i < config.commodities.Count; i++)
            {
                var c = config.commodities[i];
                if (c == null) continue;
                commodities.Add(new Dictionary<string, object>
                {
                    ["commodityKey"] = c.commodityKey,
                    ["cronExpr"] = c.cronExpr,
                    ["surgeMult"] = c.surgeMult,
                    ["quantity"] = c.quantity,
                    ["price"] = c.price,
                    ["availability"] = c.availability
                });
            }
        }
        var assignments = new List<object>();
        if (config?.assignments != null)
        {
            for (int i = 0; i < config.assignments.Count; i++)
            {
                var a = config.assignments[i];
                if (a == null) continue;
                assignments.Add(new Dictionary<string, object>
                {
                    ["assignType"] = a.assignType,
                    ["refId"] = a.refId,
                    ["role"] = a.role,
                    ["peckingOrder"] = a.peckingOrder
                });
            }
        }
        string building = config != null ? config.buildingStableId : null;
        string vehicle = config != null ? (config.vehicleId ?? "") : "";
        float weight = config != null ? config.staffingWeight : 1f;
        return new Dictionary<string, object>
        {
            ["stableId"] = stableId,
            ["name"] = displayName,
            ["kind"] = KindToApi(kind),
            ["causalityLeafId"] = causalityLeafId,
            ["buildingStableId"] = building,
            ["vehicleId"] = vehicle,
            ["parentStationId"] = parentStableId,
            ["levelId"] = levelId,
            ["staffingWeight"] = weight,
            ["config"] = new Dictionary<string, object>
            {
                ["notes"] = config?.notes,
                ["vehicleRouteId"] = config?.vehicleRouteId
            },
            ["commodities"] = commodities,
            ["assignments"] = assignments
        };
    }
}
