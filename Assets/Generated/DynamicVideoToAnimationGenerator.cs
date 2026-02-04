using UnityEngine;

/// <summary>
/// Generator for video → animation. Uses videoKey from primitive store; output is AnimationClip.
/// </summary>
[CreateAssetMenu(fileName = "DynamicVideoToAnimationGenerator", menuName = "Generated/Dynamic Video to Animation Generator", order = 6)]
public class DynamicVideoToAnimationGenerator : DynamicGeneratorBase
{
    [Header("Animation params")]
    [Tooltip("Duration in seconds for extracted/generated clip.")]
    public float durationSeconds = 2f;
    [Tooltip("Humanoid vs Generic.")]
    public bool humanoid = true;

    public override string GeneratorTypeName => "Video to Animation";
}
