using UnityEngine;

/// <summary>BT node: while periphery gate open, gaze at published webtop window centroid.</summary>
public class WebtopEyesBehaviorNode : BehaviorTreeNode
{
    public EyesGazeController gaze;
    public ComputerPeripheryStation periphery;
    public Vector3 fallbackMonitorLocalOffset = new Vector3(0f, 0.35f, 0.4f);

    public override BehaviorTreeStatus Execute(BehaviorTree tree)
    {
        if (periphery != null && !periphery.toolUseGate.AllowsToolUse())
            return BehaviorTreeStatus.Success;

        if (gaze == null && tree != null)
            gaze = tree.GetComponent<EyesGazeController>();
        if (gaze == null)
            return BehaviorTreeStatus.Failure;

        Vector3 target = gaze.webtopWindowCentroid;
        if (target.sqrMagnitude < 1e-6f && periphery != null)
        {
            Transform mon = periphery.monitorAnchor != null ? periphery.monitorAnchor : periphery.transform;
            target = mon.TransformPoint(fallbackMonitorLocalOffset);
        }
        gaze.SetWebtopCentroid(target);
        return BehaviorTreeStatus.Running;
    }
}
