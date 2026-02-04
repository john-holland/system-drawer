using UnityEngine;

/// <summary>
/// Dynamic generator for textures and UI assets. Output: Texture2D / Sprite.
/// </summary>
[CreateAssetMenu(fileName = "DynamicTextureUIGenerator", menuName = "Generated/Dynamic Texture UI Generator", order = 1)]
public class DynamicTextureUIGenerator : DynamicGeneratorBase
{
    [Header("Texture params")]
    [Tooltip("Output width.")]
    public int resolutionX = 512;
    [Tooltip("Output height.")]
    public int resolutionY = 512;
    [Tooltip("Texture format for generated asset.")]
    public TextureFormat format = TextureFormat.RGBA32;
    [Tooltip("Sprite mode when saving as sprite (0 = single, 1 = multiple).")]
    public int spriteMode = 0;

    public override string GeneratorTypeName => "Texture/UI";
}
