using UnityEngine;

namespace Locomotion.Open.Nodes
{
    /// <summary>Closes a joint until OpenableJointDriver reaches Closed state.</summary>
    public sealed class CloseJointNode : BehaviorTreeNode
    {
        public OpenableJointDriver driver;
        public OpenCloseBeatProfile profile;
        public OpenableLatch relatch;
        public AudioSource audioSource;

        void Awake() => nodeType = NodeType.Action;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (driver == null)
                return BehaviorTreeStatus.Failure;

            if (driver.state == OpenableJointState.Closed)
            {
                relatch?.Relock();
                OpenCloseCausalityBridge.NotifyClosed(OpenCloseClosureMode.CloseBeatClosed, driver.name);
                return BehaviorTreeStatus.Success;
            }

            if (driver.state != OpenableJointState.Closing)
            {
                if (!driver.BeginClose())
                    return BehaviorTreeStatus.Failure;
                if (profile != null && profile.soundClose != null)
                {
                    if (audioSource != null)
                        audioSource.PlayOneShot(profile.soundClose);
                    else
                        AudioSource.PlayClipAtPoint(profile.soundClose, driver.transform.position);
                }
            }

            return BehaviorTreeStatus.Running;
        }
    }
}
