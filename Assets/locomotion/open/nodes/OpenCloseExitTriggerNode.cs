using UnityEngine;

namespace Locomotion.Open.Nodes
{
    /// <summary>Waits until actor/camera leaves stop radius, then succeeds for close branch.</summary>
    public sealed class OpenCloseExitTriggerNode : BehaviorTreeNode
    {
        public Transform actor;
        public Transform stopCenter;
        public float exitRadiusM = 2.5f;
        public bool useCameraInstead;
        public UnityEngine.Camera rigCamera;

        bool _wasInside = true;

        void Awake() => nodeType = NodeType.Condition;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            var center = stopCenter != null ? stopCenter.position : transform.position;
            Vector3 probe;
            if (useCameraInstead && rigCamera != null)
                probe = rigCamera.transform.position;
            else if (actor != null)
                probe = actor.position;
            else
                probe = tree.transform.position;

            float d = Vector3.Distance(probe, center);
            if (_wasInside && d > exitRadiusM)
            {
                _wasInside = false;
                return BehaviorTreeStatus.Success;
            }
            if (d <= exitRadiusM)
                _wasInside = true;
            return BehaviorTreeStatus.Running;
        }
    }
}
