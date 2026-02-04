using UnityEngine;

/// <summary>
/// Dynamic generator for animation. Output: AnimationClip.
/// </summary>
[CreateAssetMenu(fileName = "DynamicAnimationGenerator", menuName = "Generated/Dynamic Animation Generator", order = 5)]
public class DynamicAnimationGenerator : DynamicGeneratorBase
{
    [Header("Animation params")]
    [Tooltip("Duration in seconds.")]
    public float durationSeconds = 2f;
    [Tooltip("Animation layer (for overrides).")]
    public int layer = 0;
    [Tooltip("Humanoid vs Generic.")]
    public bool humanoid = true;
    [Tooltip("Texture animation (UV/sprite) when true.")]
    public bool textureAnimation = false;

    public override string GeneratorTypeName => "Animation";
}
