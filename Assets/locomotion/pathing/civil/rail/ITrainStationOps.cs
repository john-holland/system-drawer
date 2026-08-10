/// <summary>Station / silo / depot operations for BT cards without hardcoding MonoBehaviours.</summary>
public interface ITrainStationOps
{
    TrainVehicleRagdoll ActiveConsist { get; }
    TrainVehicleRagdoll ActiveCar { get; }

    bool CoupleCars(TrainVehicleRagdoll front, TrainVehicleRagdoll rear);
    bool DecoupleCar(TrainVehicleRagdoll car);
    bool SwapCar(int index, TrainVehicleRagdoll replacement);
    bool RemoveCar(TrainVehicleRagdoll car);
    bool UnloadBay(TrainVehicleRagdoll car, string bayId, VehicleRagdoll vehicle);
    bool UnfoldLimb(TrainVehicleRagdoll car, string limbId);
    bool RefoldLimb(TrainVehicleRagdoll car, string limbId);
    bool TransferBulk(TrainVehicleRagdoll car, string bayId, string commodityKey, float deltaQuantity);
    bool ReplaceBayContents(TrainVehicleRagdoll car, string bayId, VehicleRagdoll newVehicle);
    float InspectLashStable01(TrainVehicleRagdoll car);
}
