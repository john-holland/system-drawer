using UnityEngine;

/// <summary>
/// Joint → mesh bone / loop-bound mapping. Parent under <see cref="AnimationBehaviorTree"/>
/// (RagdollSystem.animationTree / Default_animation_tree), not the input BehaviorTree controller.
/// </summary>
[AddComponentMenu("Locomotion/Animation/Mesh Bone Association")]
public sealed class MeshBoneAssociation : MonoBehaviour
{
    [Tooltip("Source joint trait, e.g. Human:Hips or Animal:Spine.")]
    public string sourceJointId;

    public string meshBoneName;
    public Transform meshBone;
    public string loopBoundName;
    public SkinnedMeshLoopSplitBounds loopBounds;
    public string assignmentSource = "custom";
}
