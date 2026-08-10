using UnityEngine;

public enum TrainStationOpsActionKind
{
    Couple = 0,
    SwapCar = 1,
    UnloadBay = 2,
    LimbWork = 3,
    SiloLoadUnload = 4,
    DepotReplace = 5,
    DepotLashInspect = 6
}

/// <summary>Station BT action — invokes ITrainStationOps / cards from a station hierarchy host.</summary>
public sealed class TrainStationOpsActionNode : BehaviorTreeNode
{
    public TrainStationOpsActionKind action = TrainStationOpsActionKind.Couple;
    public TrainStationOpsBase ops;
    public TrainVehicleRagdoll carA;
    public TrainVehicleRagdoll carB;
    public VehicleRagdoll nestedVehicle;
    public string bayId = "deck";
    public string limbId = "main_crane";
    public bool unfoldLimb = true;
    public bool siloLoadIntoCar = true;
    public float siloAmount;
    public bool depotPullIntoShop = true;
    public int swapOrReinsertIndex;
    public bool depotRelash;

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        if (ops == null && tree != null)
            ops = tree.GetComponentInParent<TrainStationOpsBase>();
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        var host = tree != null ? tree.gameObject : gameObject;
        bool ok = false;
        switch (action)
        {
            case TrainStationOpsActionKind.Couple:
                ok = new TrainStationCoupleCard { ops = ops, front = carA, rear = carB }.Execute(host);
                break;
            case TrainStationOpsActionKind.SwapCar:
                ok = new TrainStationSwapCarCard
                {
                    ops = ops,
                    carIndex = swapOrReinsertIndex,
                    replacement = carB
                }.Execute(host);
                break;
            case TrainStationOpsActionKind.UnloadBay:
                ok = new TrainStationUnloadBayCard
                {
                    ops = ops,
                    car = carA,
                    bayId = bayId,
                    vehicle = nestedVehicle
                }.Execute(host);
                break;
            case TrainStationOpsActionKind.LimbWork:
                ok = new TrainStationLimbWorkCard
                {
                    ops = ops,
                    car = carA,
                    limbId = limbId,
                    unfold = unfoldLimb
                }.Execute(host);
                break;
            case TrainStationOpsActionKind.SiloLoadUnload:
                ok = new SiloLoadUnloadCard
                {
                    ops = ops,
                    car = carA,
                    bayId = bayId,
                    amount = siloAmount,
                    loadIntoCar = siloLoadIntoCar
                }.Execute(host);
                break;
            case TrainStationOpsActionKind.DepotReplace:
                ok = new RailDepotReplaceCarCard
                {
                    ops = ops,
                    car = carA,
                    pullIntoShop = depotPullIntoShop,
                    reinsertIndex = swapOrReinsertIndex
                }.Execute(host);
                break;
            case TrainStationOpsActionKind.DepotLashInspect:
                ok = new RailDepotLashInspectCard
                {
                    ops = ops,
                    car = carA,
                    relash = depotRelash
                }.Execute(host, out _);
                break;
        }
        status = ok ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
        return status;
    }
}
