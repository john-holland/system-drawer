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
        public string topologyNodeId;

        bool _messagesRaised;

        void Awake() => nodeType = NodeType.Action;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (driver == null)
                return BehaviorTreeStatus.Failure;

            if (driver.state == OpenableJointState.Closed)
            {
                relatch?.Relock();
                if (!_messagesRaised)
                {
                    OpenCloseCausalityBridge.NotifyClosed(OpenCloseClosureMode.CloseBeatClosed, driver.name);
                    OpenCloseBeatMessageBus.RaiseCloseBeat(
                        !string.IsNullOrEmpty(topologyNodeId) ? topologyNodeId : driver.name,
                        profile,
                        driver.transform.position);
                    _messagesRaised = true;
                }
                return BehaviorTreeStatus.Success;
            }

            if (driver.state != OpenableJointState.Closing)
            {
                driver.ApplyProfile(profile);
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
