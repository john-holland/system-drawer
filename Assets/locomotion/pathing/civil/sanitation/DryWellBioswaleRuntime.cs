using UnityEngine;

/// <summary>Optional runoff feature where no buildings — SDF Max or mesh; uses HeightMapInteriorShaderBuffer.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Dry Well")]
public sealed class DryWellRuntime : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public Component sdfMaxSurface;
    public HeightMapInteriorShaderBuffer heightMapBuffer;
    public float capacityM3 = 12f;

    void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (heightMapBuffer == null)
            heightMapBuffer = GetComponent<HeightMapInteriorShaderBuffer>()
                              ?? GetComponentInParent<HeightMapInteriorShaderBuffer>();
        heightMapBuffer?.RegisterDescendingMesh(meshRenderer);
        var graph = FindFirstObjectByType<SewerGraph>();
        if (graph != null)
        {
            graph.nodes.Add(new SewerNode
            {
                nodeId = "drywell_" + GetInstanceID(),
                worldPosition = transform.position,
                isDryWell = true,
                building = gameObject
            });
        }
    }

    void OnDestroy() => heightMapBuffer?.UnregisterDescendingMesh(meshRenderer);
}

[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Sanitation/Bioswale")]
public sealed class BioswaleRuntime : MonoBehaviour
{
    public MeshRenderer meshRenderer;
    public HeightMapInteriorShaderBuffer heightMapBuffer;
    public float retention01 = 0.6f;

    void Awake()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (heightMapBuffer == null)
            heightMapBuffer = GetComponent<HeightMapInteriorShaderBuffer>()
                              ?? GetComponentInParent<HeightMapInteriorShaderBuffer>();
        heightMapBuffer?.RegisterDescendingMesh(meshRenderer);
    }

    void OnDestroy() => heightMapBuffer?.UnregisterDescendingMesh(meshRenderer);
}
