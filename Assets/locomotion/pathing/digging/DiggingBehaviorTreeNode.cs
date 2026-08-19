using UnityEngine;
using Locomotion.Rig;

/// <summary>Ambulates to dig contact (optional skip) then IK card. Mirrors OpenCloseAmbulateToStopNode.</summary>
[AddComponentMenu("Locomotion/Digging/Digging Behavior Tree Node")]
public sealed class DiggingBehaviorTreeNode : BehaviorTreeNode
{
    public Vector3 approachAnchor;
    public Vector3 contactWorld;
    public bool stopAmbulation = true;
    public DiggingCard card;
    public PhysicsCardSolver physicsSolver;
    public HierarchicalPathingSolver pathfindingSolver;
    public float reachRadiusMeters = 0.6f;
    public DigActionQueue actionQueue;
    public DiggableVolume volume;
    public DigScoopSph scoopSph;

    PathfindingNode _pathNode;
    bool _pathBuilt;
    bool _ambulationDone;

    void Awake() => nodeType = NodeType.Sequence;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (tree == null)
            return BehaviorTreeStatus.Failure;

        if (physicsSolver == null)
            physicsSolver = tree.GetComponent<PhysicsCardSolver>();
        if (physicsSolver != null)
            physicsSolver.skipAmbulationWalkingFilter = !stopAmbulation;

        if (!stopAmbulation)
            return BehaviorTreeStatus.Success;

        if (pathfindingSolver == null)
            pathfindingSolver = Object.FindAnyObjectByType<HierarchicalPathingSolver>();

        if (!_pathBuilt)
            BuildPath(tree);

        if (!_ambulationDone && _pathNode != null)
        {
            var pathStatus = _pathNode.Execute(tree);
            if (pathStatus == BehaviorTreeStatus.Running)
                return BehaviorTreeStatus.Running;
            if (pathStatus == BehaviorTreeStatus.Failure)
                return BehaviorTreeStatus.Failure;
            _ambulationDone = true;
        }

        ApplyScoop();
        return BehaviorTreeStatus.Success;
    }

    void ApplyScoop()
    {
        if (actionQueue == null)
            actionQueue = GetComponent<DigActionQueue>() ?? GetComponentInParent<DigActionQueue>();
        Vector3 contact = contactWorld.sqrMagnitude > 1e-6f ? contactWorld : approachAnchor;
        if (actionQueue != null)
            actionQueue.EnqueueContact(contact, 0.25f, Time.time);
        if (volume == null)
            volume = GetComponent<DiggableVolume>() ?? GetComponentInParent<DiggableVolume>();
        volume?.ApplyScoop(scoopSph, contact, 0.25f);
    }

    void BuildPath(BehaviorTree tree)
    {
        _pathBuilt = true;
        _pathNode = gameObject.GetComponent<PathfindingNode>();
        if (_pathNode == null)
            _pathNode = gameObject.AddComponent<PathfindingNode>();
        _pathNode.pathfindingSolver = pathfindingSolver;
        _pathNode.destination = approachAnchor.sqrMagnitude > 1e-6f ? approachAnchor : contactWorld;
    }
}

[AddComponentMenu("Locomotion/Digging/Digging Behavior Tree Plan Node")]
public sealed class DiggingBehaviorTreePlanNode : BehaviorTreeNode
{
    public DiggingTopologyAsset topology;
    public GameObject tool;
    public BoneMap boneMap;
    public bool stopAmbulation = true;

    public TopologicalDigSolver.CompileResult Compile()
    {
        return TopologicalDigSolver.Compile(topology, tool, boneMap);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        var compiled = Compile();
        return compiled.stepIds.Count > 0 ? BehaviorTreeStatus.Success : BehaviorTreeStatus.Failure;
    }
}
