using UnityEngine;

namespace Locomotion.Open.Nodes
{
    /// <summary>Opens a joint until OpenableJointDriver reaches Open state.</summary>
    public sealed class OpenJointNode : BehaviorTreeNode
    {
        public OpenableJointDriver driver;
        public OpenCloseBeatProfile profile;
        public AudioSource audioSource;
        public string topologyNodeId;

        bool _messagesRaised;

        void Awake() => nodeType = NodeType.Action;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (driver == null)
                return BehaviorTreeStatus.Failure;
            if (!driver.CanOpen())
                return BehaviorTreeStatus.Failure;

            if (driver.state == OpenableJointState.Open)
            {
                if (!_messagesRaised)
                {
                    string id = !string.IsNullOrEmpty(topologyNodeId) ? topologyNodeId : driver.name;
                    OpenCloseCausalityBridge.NotifyOpened(driver.name);
                    OpenCloseBeatMessageBus.RaiseOpenBeat(id, profile, driver.transform.position);
                    OpenCloseBeatHooks.OnBeatOpened(tree, profile, id, driver.transform.position);
                    _messagesRaised = true;
                }
                return BehaviorTreeStatus.Success;
            }

            if (driver.state != OpenableJointState.Opening)
            {
                driver.ApplyProfile(profile);
                if (driver.driveMode == OpenCloseDriveMode.Animation && !driver.IsAnimationReady)
                    return BehaviorTreeStatus.Running;

                if (!driver.BeginOpen())
                    return BehaviorTreeStatus.Failure;

                if (profile != null && profile.soundOpen != null)
                {
                    if (audioSource != null)
                        audioSource.PlayOneShot(profile.soundOpen);
                    else
                        AudioSource.PlayClipAtPoint(profile.soundOpen, driver.transform.position);
                }

                OpenCloseCausalityBridge.NotifyActive(driver.name);
            }

            return BehaviorTreeStatus.Running;
        }
    }
}
