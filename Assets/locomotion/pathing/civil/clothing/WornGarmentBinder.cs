using UnityEngine;

/// <summary>Worn garment uses ClothUvStretchDriver — no third stretch stack.</summary>
[AddComponentMenu("Locomotion/Civil/Worn Garment Binder")]
public sealed class WornGarmentBinder : MonoBehaviour
{
    public ClothUvStretchDriver stretch;
    public ClothingDamageLayer damage;
    public TayloringTravelAgent tayloring;
    public MeshFilter meshFilter;

    void Awake()
    {
        if (stretch == null)
            stretch = GetComponent<ClothUvStretchDriver>() ?? gameObject.AddComponent<ClothUvStretchDriver>();
        if (damage == null)
            damage = GetComponent<ClothingDamageLayer>();
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>();
    }

    public void BindBakedStep(TayloringStep step)
    {
        if (step?.bakedMesh == null) return;
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = step.bakedMesh;
        if (stretch == null)
            stretch = gameObject.AddComponent<ClothUvStretchDriver>();
    }
}
