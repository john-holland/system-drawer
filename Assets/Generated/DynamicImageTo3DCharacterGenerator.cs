using UnityEngine;

/// <summary>
/// Generator for image → 3D character. Uses imageKey from primitive store; output is character prefab.
/// </summary>
[CreateAssetMenu(fileName = "DynamicImageTo3DCharacterGenerator", menuName = "Generated/Dynamic Image to 3D Character Generator", order = 3)]
public class DynamicImageTo3DCharacterGenerator : DynamicGeneratorBase
{
    [Header("3D character params")]
    [Tooltip("Scale applied to generated character.")]
    public Vector3 scale = Vector3.one;

    public override string GeneratorTypeName => "Image to 3D Character";
}
