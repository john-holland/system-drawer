using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Optional seated walk reminder: idle-debounced timer → stand → description-filtered waypoint → walk → re-sit.
/// </summary>
public class SeatedWalkReminderNode : BehaviorTreeNode
{
    public SeatedWalkReminderPolicy policy = new SeatedWalkReminderPolicy();
    public SpatialDescriptionFilter descriptionFilter = new SpatialDescriptionFilter();
    public List<SpatialTaggedPoint> candidateWaypoints = new List<SpatialTaggedPoint>();
    public ComputerPeripheryStation peripheryStation;
    public SitSurfaceContact reSitSurface;
    public bool playerInputActive;

    enum Phase { Watch, Stand, Walk, ReSit, Done }
    Phase _phase = Phase.Watch;
    Vector3 _walkTarget;
    PathfindingNode _pathNode;
    bool _pathStarted;
    SitOnSurfaceNode _sitNode;

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (tree == null)
            return BehaviorTreeStatus.Failure;

        RagdollSystem ragdoll = tree.GetComponent<RagdollSystem>();
        var runtime = ragdoll != null ? ragdoll.GetComponent<SeatedOccupancyRuntime>() : null;
        var sheet = ragdoll != null ? ragdoll.GetComponent<LifeSystemsSheet>() : null;
        bool seated = runtime != null && runtime.occupied && runtime.mode == SurfaceOccupancyMode.Sit;

        if (_phase == Phase.Watch)
        {
            if (policy == null || !policy.enabled)
                return BehaviorTreeStatus.Success;

            bool fire = policy.Tick(Time.deltaTime, playerInputActive, seated, sheet);
            if (!fire)
                return BehaviorTreeStatus.Running;

            if (!descriptionFilter.TryPickWaypoint(candidateWaypoints, out _walkTarget, out _))
            {
                // Fallback: small offset from actor
                _walkTarget = ragdoll != null
                    ? ragdoll.transform.position + Vector3.forward * 2f
                    : Vector3.zero;
            }

            _phase = Phase.Stand;
            if (runtime != null)
                runtime.EndOccupancy();
            if (peripheryStation != null)
                peripheryStation.Vacate(ragdoll != null ? ragdoll.gameObject : null);
            policy.AcknowledgeHandled();
        }

        if (_phase == Phase.Stand)
        {
            // Instant stand for reminder branch; NarrativeStandAction can replace later.
            _phase = Phase.Walk;
            _pathStarted = false;
            _pathNode = tree.FindNode(n => n is PathfindingNode) as PathfindingNode;
        }

        if (_phase == Phase.Walk)
        {
            if (_pathNode != null && ragdoll != null)
            {
                _pathNode.origin = ragdoll.transform.position;
                _pathNode.destination = _walkTarget;
                if (!_pathStarted)
                {
                    _pathStarted = true;
                    _pathNode.OnEnter(tree);
                }
                var st = _pathNode.Execute(tree);
                if (st == BehaviorTreeStatus.Running)
                    return BehaviorTreeStatus.Running;
            }
            _phase = Phase.ReSit;
        }

        if (_phase == Phase.ReSit)
        {
            SitSurfaceContact contact = reSitSurface
                ?? (peripheryStation != null ? peripheryStation.seat.contact : null)
                ?? (runtime != null ? runtime.surface : null);
            if (contact == null || ragdoll == null)
            {
                _phase = Phase.Done;
                return BehaviorTreeStatus.Success;
            }

            if (_sitNode == null)
            {
                var go = new GameObject("WalkReminder_ReSit");
                go.transform.SetParent(transform, false);
                _sitNode = go.AddComponent<SitOnSurfaceNode>();
                _sitNode.surface = contact;
            }

            _sitNode.surface = contact;
            var sitStatus = _sitNode.Execute(tree);
            if (sitStatus == BehaviorTreeStatus.Running)
                return BehaviorTreeStatus.Running;
            if (peripheryStation != null)
                peripheryStation.Occupy(ragdoll.gameObject, SurfaceOccupancyMode.Sit);
            _phase = Phase.Done;
            return BehaviorTreeStatus.Success;
        }

        return BehaviorTreeStatus.Success;
    }

    public override void OnEnter(BehaviorTree tree)
    {
        _phase = Phase.Watch;
        _pathStarted = false;
        _pathNode = null;
        if (policy != null)
            policy.Reset();
    }

    public override void OnExit(BehaviorTree tree)
    {
        if (_pathNode != null)
            _pathNode.OnExit(tree);
        if (_sitNode != null)
            _sitNode.OnExit(tree);
        _phase = Phase.Watch;
    }

    /// <summary>Call from player input path so debounce resets.</summary>
    public void NotifyPlayerInput()
    {
        playerInputActive = true;
        policy?.NotifyPlayerInput();
    }

    public void ClearPlayerInput()
    {
        playerInputActive = false;
    }
}
