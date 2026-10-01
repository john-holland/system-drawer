using System.Collections.Generic;
using UnityEngine;

/// <summary>Samples RailTrackStructure into a waypoint chain for TravelLegMode.Rail.</summary>
public sealed class RailTrackFollowPlanNode : BehaviorTreeNode
{
    public RailTrackStructure track;
    public string railSegmentId;
    public TrainVehicleRagdoll train;
    public int sampleCount = 16;
    public List<Vector3> sampledPath = new List<Vector3>();

    void Awake() => nodeType = NodeType.Action;

    public override void OnEnter(BehaviorTree tree)
    {
        if (train == null && tree != null)
            train = tree.GetComponentInParent<TrainVehicleRagdoll>();
        if (track == null && !string.IsNullOrEmpty(railSegmentId))
            track = RailTrackStructure.FindBySegmentId(railSegmentId);
        if (track == null && train != null && !string.IsNullOrEmpty(train.railSegmentId))
            track = RailTrackStructure.FindBySegmentId(train.railSegmentId);
        Sample();
        if (train != null)
            train.RebuildPlanarPaths();
    }

    public void Sample()
    {
        sampledPath.Clear();
        if (track == null) return;
        track.EnsureSplinePoints();
        int n = Mathf.Max(2, sampleCount);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)(n - 1);
            sampledPath.Add(track.SamplePosition(t));
        }
        if (train != null && train.cars != null && train.cars.Count > 0)
            train.CopySnakeWorldPositions(sampledPath);
    }

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        status = BehaviorTreeStatus.Success;
        return BehaviorTreeStatus.Success;
    }
}
