using UnityEngine;

/// <summary>
/// Dynamic generator for 3D objects. Output: Prefab / GameObject ref.
/// </summary>
[CreateAssetMenu(fileName = "Dynamic3DObjectGenerator", menuName = "Generated/Dynamic 3D Object Generator", order = 2)]
public class Dynamic3DObjectGenerator : DynamicGeneratorBase
{
    [Header("3D params")]
    [Tooltip("When true, generate as character (rigged); when false, generate as object.")]
    public bool isCharacter = false;
    [Tooltip("Mesh LOD level (0 = highest).")]
    public int meshLOD = 0;
    [Tooltip("Scale applied to generated object.")]
    public Vector3 scale = Vector3.one;
    [Tooltip("Add collider to generated prefab.")]
    public bool addCollider = true;
    [Tooltip("Optional material override for generated mesh.")]
    public Material materialOverride;

    public override string GeneratorTypeName => "3D Object";
}
