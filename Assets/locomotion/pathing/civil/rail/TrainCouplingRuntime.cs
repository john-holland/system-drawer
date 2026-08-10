using UnityEngine;

/// <summary>Front/rear couplers — attach/detach feeds consist + multibody linked segments.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Rail/Train Coupling")]
public sealed class TrainCouplingRuntime : MonoBehaviour
{
    public TrainVehicleRagdoll car;
    public TrainCouplingRuntime frontConnected;
    public TrainCouplingRuntime rearConnected;
    public float couplerSpacingM = 1.2f;
    public bool locked = true;

    void Awake()
    {
        if (car == null)
            car = GetComponent<TrainVehicleRagdoll>();
    }

    public bool CoupleFrontTo(TrainCouplingRuntime other)
    {
        if (other == null || other == this) return false;
        frontConnected = other;
        other.rearConnected = this;
        AlignTo(other, ahead: true);
        SyncConsist(other);
        if (car != null)
            car.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.Couple,
                SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public bool CoupleRearTo(TrainCouplingRuntime other)
    {
        if (other == null || other == this) return false;
        rearConnected = other;
        other.frontConnected = this;
        AlignTo(other, ahead: false);
        SyncConsist(other);
        if (car != null)
            car.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.Couple,
                SendMessageOptions.DontRequireReceiver);
        return true;
    }

    public void DecoupleFront()
    {
        if (frontConnected != null)
        {
            frontConnected.rearConnected = null;
            frontConnected = null;
        }
        if (car != null)
            car.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.Decouple,
                SendMessageOptions.DontRequireReceiver);
    }

    public void DecoupleRear()
    {
        if (rearConnected != null)
        {
            rearConnected.frontConnected = null;
            rearConnected = null;
        }
        if (car != null)
            car.SendMessage("OnNarrativeSchedulerAction", TrainCarNarrativeActionIds.Decouple,
                SendMessageOptions.DontRequireReceiver);
    }

    void AlignTo(TrainCouplingRuntime other, bool ahead)
    {
        if (car == null || other.car == null) return;
        Vector3 dir = other.car.transform.forward;
        float sign = ahead ? -1f : 1f;
        float len = EstimateLength(other.car) * 0.5f + EstimateLength(car) * 0.5f + couplerSpacingM;
        car.transform.position = other.car.transform.position + dir * (sign * len);
        car.transform.rotation = other.car.transform.rotation;
    }

    static float EstimateLength(TrainVehicleRagdoll c)
    {
        if (c == null) return 6f;
        var cols = c.GetComponentsInChildren<Collider>();
        if (cols == null || cols.Length == 0) return 6f;
        Bounds b = cols[0].bounds;
        for (int i = 1; i < cols.Length; i++)
            b.Encapsulate(cols[i].bounds);
        return Mathf.Max(4f, b.size.z);
    }

    void SyncConsist(TrainCouplingRuntime other)
    {
        var host = car != null ? (car.headTrain != null ? car.headTrain : car) : null;
        if (host == null && other.car != null)
            host = other.car.headTrain != null ? other.car.headTrain : other.car;
        if (host == null) return;
        host.RebuildFromCouplers(car != null ? car : other.car);
        if (other.car != null)
            other.car.headTrain = host;
        if (car != null)
            car.headTrain = host;
    }
}
