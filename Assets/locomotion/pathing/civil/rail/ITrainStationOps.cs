/// <summary>Station / silo / depot operations for BT cards without hardcoding MonoBehaviours.</summary>
public interface ITrainStationOps
{
    TrainConsistRuntime ActiveConsist { get; }
    TrainCarVehicleRagdoll ActiveCar { get; }

    bool CoupleCars(TrainCarVehicleRagdoll front, TrainCarVehicleRagdoll rear);
    bool DecoupleCar(TrainCarVehicleRagdoll car);
    bool SwapCar(int index, TrainCarVehicleRagdoll replacement);
    bool RemoveCar(TrainCarVehicleRagdoll car);
    bool UnloadBay(TrainCarVehicleRagdoll car, string bayId, VehicleRagdoll vehicle);
    bool UnfoldLimb(TrainCarVehicleRagdoll car, string limbId);
    bool RefoldLimb(TrainCarVehicleRagdoll car, string limbId);
    bool TransferBulk(TrainCarVehicleRagdoll car, string bayId, string commodityKey, float deltaQuantity);
    bool ReplaceBayContents(TrainCarVehicleRagdoll car, string bayId, VehicleRagdoll newVehicle);
    float InspectLashStable01(TrainCarVehicleRagdoll car);
}
