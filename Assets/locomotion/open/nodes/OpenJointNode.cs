using UnityEngine;

namespace Locomotion.Open.Nodes
{
    /// <summary>Opens a joint until OpenableJointDriver reaches Open state.</summary>
    public sealed class OpenJointNode : BehaviorTreeNode
    {
        public OpenableJointDriver driver;
        public OpenCloseBeatProfile profile;
        public AudioSource audioSource;

        void Awake() => nodeType = NodeType.Action;

        public override BehaviorTreeStatus Execute(BehaviorTree tree)
        {
            if (driver == null)
                return BehaviorTreeStatus.Failure;
            if (!driver.CanOpen())
                return BehaviorTreeStatus.Failure;

            if (driver.state == OpenableJointState.Open)
            {
                OpenCloseCausalityBridge.NotifyOpened(driver.name);
                OpenCloseBeatHooks.OnBeatOpened(tree, profile);
                return BehaviorTreeStatus.Success;
            }

            if (driver.state != OpenableJointState.Opening)
            {
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
