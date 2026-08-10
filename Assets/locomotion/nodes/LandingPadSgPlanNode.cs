using UnityEngine;

/// <summary>SG4D-style plan node that stamps a RoadLot / parking pad under a building hierarchy.</summary>
public sealed class LandingPadSgPlanNode : BehaviorTreeNode
{
    public string lotId = "landing_pad";
    public Vector3 padSize = new Vector3(30f, 2f, 30f);
    public bool attachParkingLot;
    public RoadLot createdLot;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        Transform parent = tree != null ? tree.transform : transform;
        var go = new GameObject("RoadLot_" + lotId);
        go.transform.SetParent(parent, false);
        go.transform.position = parent.position;
        createdLot = go.AddComponent<RoadLot>();
        createdLot.lotId = lotId;
        createdLot.padSize = padSize;
        createdLot.EnsureDefaultsSafe();
        if (attachParkingLot)
        {
            var parking = go.AddComponent<ParkingLot>();
            parking.lotId = lotId + "_parking";
        }
        status = BehaviorTreeStatus.Success;
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree) => BehaviorTreeStatus.Success;
}

// Small helper on RoadLot via extension method style partial usage
public static class RoadLotBootstrap
{
    public static void EnsureDefaultsSafe(this RoadLot lot)
    {
        if (lot == null) return;
        if (lot.boundary == null)
            lot.boundary = lot.GetComponent<RoadLotBoundarySpline>()
                           ?? lot.gameObject.AddComponent<RoadLotBoundarySpline>();
        lot.boundary.EnsureClosedLoopDefault();
    }
}
