using UnityEngine;

/// <summary>Shared train station / silo / depot ops for BT cards.</summary>
public abstract class TrainStationOpsBase : MonoBehaviour, ITrainStationOps
{
    public TrainVehicleRagdoll activeConsist;
    public TrainVehicleRagdoll activeCar;

    public TrainVehicleRagdoll ActiveConsist => activeConsist;
    public TrainVehicleRagdoll ActiveCar => activeCar;

    public virtual bool CoupleCars(TrainVehicleRagdoll front, TrainVehicleRagdoll rear)
    {
        if (front?.coupling == null || rear?.coupling == null) return false;
        bool ok = front.coupling.CoupleRearTo(rear.coupling);
        if (ok && activeConsist != null)
            activeConsist.RebuildFromCouplers(front);
        return ok;
    }

    public virtual bool DecoupleCar(TrainVehicleRagdoll car)
    {
        if (car?.coupling == null) return false;
        car.coupling.DecoupleFront();
        car.coupling.DecoupleRear();
        activeConsist?.RemoveCar(car);
        return true;
    }

    public virtual bool SwapCar(int index, TrainVehicleRagdoll replacement)
    {
        if (activeConsist == null) return false;
        return activeConsist.ReplaceCar(index, replacement);
    }

    public virtual bool RemoveCar(TrainVehicleRagdoll car)
    {
        if (activeConsist == null) return DecoupleCar(car);
        return activeConsist.RemoveCar(car);
    }

    public virtual bool UnloadBay(TrainVehicleRagdoll car, string bayId, VehicleRagdoll vehicle)
    {
        if (car == null) return false;
        car.SetBayRampUnfolded(bayId, true);
        return car.TryUnloadVehicle(vehicle, bayId);
    }

    public virtual bool UnfoldLimb(TrainVehicleRagdoll car, string limbId) =>
        car != null && car.TryUnfoldLimb(limbId);

    public virtual bool RefoldLimb(TrainVehicleRagdoll car, string limbId) =>
        car != null && car.TryRefoldLimb(limbId);

    public virtual bool TransferBulk(TrainVehicleRagdoll car, string bayId, string commodityKey, float deltaQuantity)
    {
        if (car == null) return false;
        var bay = car.FindBay(bayId);
        if (bay == null || bay.kind == TrainCarBayKind.Vehicle) return false;
        if (!string.IsNullOrEmpty(commodityKey))
            bay.bulkCommodityKey = commodityKey;
        bay.bulkQuantity = Mathf.Max(0f, bay.bulkQuantity + deltaQuantity);
        // Mirror into cargo inventory section when present.
        for (int i = 0; i < car.interiors.Count; i++)
        {
            var sec = car.interiors[i];
            if (sec == null || sec.sectionName != "cargo") continue;
            var item = sec.items.Find(it => it != null && it.itemId == bay.bulkCommodityKey);
            if (item == null)
            {
                item = new VehicleInventoryItem { itemId = bay.bulkCommodityKey, label = bay.bulkCommodityKey, count = 0 };
                sec.items.Add(item);
            }
            item.count = Mathf.Max(0, Mathf.RoundToInt(bay.bulkQuantity));
            break;
        }
        return true;
    }

    public virtual bool ReplaceBayContents(TrainVehicleRagdoll car, string bayId, VehicleRagdoll newVehicle)
    {
        if (car == null || newVehicle == null) return false;
        var bay = car.FindBay(bayId);
        if (bay == null) return false;
        bay.containedVehicles.Clear();
        return car.TryParkVehicle(newVehicle, bayId);
    }

    public virtual float InspectLashStable01(TrainVehicleRagdoll car)
    {
        if (car == null) return 1f;
        car.lashRuntime?.TickEvaluate(Vector3.zero);
        return car.LastLashStable01;
    }
}
