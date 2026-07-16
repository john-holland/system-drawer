using UnityEngine;

/// <summary>BT plan node: find grabbable → carry/climb → place → optional turn → Sit/StandOn occupy.</summary>
public class PlaceBuildTopologyPlanNode : BehaviorTreeNode
{
    public PlaceBuildTopologyAsset topology;
    public float findRadius = 8f;
    public SurfaceOccupancyMode occupyMode = SurfaceOccupancyMode.Sit;

    enum Phase { Find, Place, Occupy, Done }
    Phase _phase = Phase.Find;
    GameObject _prop;
    SitSurfaceContact _builtContact;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        RagdollSystem ragdoll = tree != null ? tree.GetComponent<RagdollSystem>() : null;
        if (ragdoll == null)
            return BehaviorTreeStatus.Failure;

        var bridge = topology != null ? topology.bridge : new SeatStandBridgeSpec { occupancy = occupyMode };

        if (_phase == Phase.Find)
        {
            _prop = PlaceBuildTopologyBtBuilder.FindGrabbable(ragdoll.transform.position, findRadius, bridge);
            if (_prop == null)
                return BehaviorTreeStatus.Failure;
            _phase = Phase.Place;
        }

        if (_phase == Phase.Place)
        {
            Vector3 placePos = ragdoll.transform.position + ragdoll.transform.forward * 0.5f;
            if (topology != null && topology.nodes != null && topology.nodes.Count > 0)
                placePos = topology.nodes[0].placeWorldPosition.sqrMagnitude > 1e-4f
                    ? topology.nodes[0].placeWorldPosition
                    : placePos;
            _prop.transform.position = placePos;
            _builtContact = SitSurfaceContact.FromWorldPlane(_prop.transform, placePos + Vector3.up * 0.2f, Vector3.up,
                bridge.minContactHalfExtent, bridge.minContactHalfExtent);
            _phase = Phase.Occupy;
        }

        if (_phase == Phase.Occupy)
        {
            var runtime = ragdoll.GetComponent<SeatedOccupancyRuntime>();
            if (runtime == null)
                runtime = ragdoll.gameObject.AddComponent<SeatedOccupancyRuntime>();
            SurfaceOccupancyMode mode = topology != null && topology.nodes != null && topology.nodes.Count > 0
                ? topology.nodes[0].occupyMode
                : occupyMode;
            if (mode == SurfaceOccupancyMode.StandOn)
                runtime.BeginStandOn(_builtContact);
            else
                runtime.BeginSit(_builtContact);
            _phase = Phase.Done;
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Success;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _phase = Phase.Find;
        _prop = null;
        _builtContact = null;
    }
}
