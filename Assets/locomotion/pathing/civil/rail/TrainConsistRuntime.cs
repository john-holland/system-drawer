using System.Collections.Generic;
using UnityEngine;

/// <summary>Ordered coupled cars — head/tail and formation group for rail travel / snake multibody.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Consist")]
public sealed class TrainConsistRuntime : MonoBehaviour
{
    public string consistId = "consist_1";
    public string formationGroupId = "train_snake";
    public List<TrainCarVehicleRagdoll> cars = new List<TrainCarVehicleRagdoll>();
    public bool linkedSegmentMultibody = true;
    public float nominalCouplerSpacingM = 1.2f;

    public TrainCarVehicleRagdoll Head => cars != null && cars.Count > 0 ? cars[0] : null;
    public TrainCarVehicleRagdoll Tail => cars != null && cars.Count > 0 ? cars[cars.Count - 1] : null;

    void Awake()
    {
        if (string.IsNullOrEmpty(consistId))
            consistId = gameObject.name;
        RebuildFromChildren();
    }

    public void RebuildFromChildren()
    {
        cars.Clear();
        var found = GetComponentsInChildren<TrainCarVehicleRagdoll>(true);
        for (int i = 0; i < found.Length; i++)
            if (found[i] != null)
                AddCar(found[i]);
        IndexCars();
    }

    public void RebuildFromCouplers(TrainCarVehicleRagdoll seed)
    {
        cars.Clear();
        if (seed == null) return;
        var head = WalkToHead(seed);
        var cur = head;
        var guard = 0;
        while (cur != null && guard++ < 256)
        {
            AddCar(cur);
            cur = cur.coupling != null && cur.coupling.rearConnected != null
                ? cur.coupling.rearConnected.car
                : null;
        }
        IndexCars();
    }

    static TrainCarVehicleRagdoll WalkToHead(TrainCarVehicleRagdoll seed)
    {
        var cur = seed;
        var guard = 0;
        while (cur?.coupling?.frontConnected?.car != null && guard++ < 256)
            cur = cur.coupling.frontConnected.car;
        return cur;
    }

    public void AddCar(TrainCarVehicleRagdoll car)
    {
        if (car == null || cars.Contains(car)) return;
        cars.Add(car);
        car.consist = this;
        car.consistId = consistId;
    }

    public bool RemoveCar(TrainCarVehicleRagdoll car)
    {
        if (car == null) return false;
        bool ok = cars.Remove(car);
        if (ok)
        {
            if (car.coupling != null)
            {
                car.coupling.DecoupleFront();
                car.coupling.DecoupleRear();
            }
            if (car.consist == this)
                car.consist = null;
            IndexCars();
        }
        return ok;
    }

    public bool ReplaceCar(int index, TrainCarVehicleRagdoll replacement)
    {
        if (replacement == null || index < 0 || index >= cars.Count) return false;
        var old = cars[index];
        TrainCouplingRuntime front = old?.coupling?.frontConnected;
        TrainCouplingRuntime rear = old?.coupling?.rearConnected;
        if (old != null)
        {
            old.coupling?.DecoupleFront();
            old.coupling?.DecoupleRear();
        }
        cars[index] = replacement;
        replacement.consist = this;
        replacement.consistId = consistId;
        if (front != null) replacement.coupling?.CoupleFrontTo(front);
        if (rear != null) replacement.coupling?.CoupleRearTo(rear);
        IndexCars();
        return true;
    }

    public void InsertCar(int index, TrainCarVehicleRagdoll car)
    {
        if (car == null) return;
        index = Mathf.Clamp(index, 0, cars.Count);
        cars.Insert(index, car);
        car.consist = this;
        car.consistId = consistId;
        IndexCars();
    }

    void IndexCars()
    {
        for (int i = 0; i < cars.Count; i++)
        {
            if (cars[i] == null) continue;
            cars[i].carIndexInConsist = i;
            cars[i].consistId = consistId;
            cars[i].consist = this;
        }
    }

    /// <summary>World positions for linked-segment snake (coupler chain).</summary>
    public void CopySnakeWorldPositions(List<Vector3> dst)
    {
        if (dst == null) return;
        dst.Clear();
        for (int i = 0; i < cars.Count; i++)
            if (cars[i] != null)
                dst.Add(cars[i].transform.position);
    }

    public float SpacingToNext(int index)
    {
        if (index < 0 || index >= cars.Count - 1) return nominalCouplerSpacingM;
        if (cars[index] == null || cars[index + 1] == null) return nominalCouplerSpacingM;
        return Vector3.Distance(cars[index].transform.position, cars[index + 1].transform.position);
    }
}
