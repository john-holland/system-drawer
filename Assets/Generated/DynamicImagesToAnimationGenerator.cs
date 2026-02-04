using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generator for images → animation. Uses image keys from primitive store; composes as video then video → animation.
/// </summary>
[CreateAssetMenu(fileName = "DynamicImagesToAnimationGenerator", menuName = "Generated/Dynamic Images to Animation Generator", order = 7)]
public class DynamicImagesToAnimationGenerator : DynamicGeneratorBase
{
    [Header("Image sources")]
    [Tooltip("Keys of stored image primitives (order = frame order).")]
    public List<string> imageKeys = new List<string>();

    [Header("Animation params")]
    [Tooltip("Duration in seconds for generated clip.")]
    public float durationSeconds = 2f;

    public override string GeneratorTypeName => "Images to Animation";
}
