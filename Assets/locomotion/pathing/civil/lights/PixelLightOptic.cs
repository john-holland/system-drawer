using UnityEngine;

/// <summary>
/// Bread-pan / trapezoid optic: transparent top, metallic sides, internal light.
/// Optics params drive luminance + lenticulation / lensing.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Pixel Light Optic")]
public sealed class PixelLightOptic : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public Light interiorLight;
    public Material topMaterial;
    public Material sideMaterial;

    [Header("Optics")]
    public Texture2D luminanceTexture;
    public Texture2D lenticulationTexture;
    [Range(0f, 1f)] public float lenticulationSmooth01 = 0.35f;
    public float lenticulationRotationDeg;
    [Range(0.01f, 32f)] public float lenticulationGranularity = 8f;
    public Vector2 lensingVector2 = new Vector2(0.5f, 0.5f);
    [Range(0f, 2f)] public float lensSize = 0.35f;

    static readonly int IdSmooth = Shader.PropertyToID("_LenticulationSmooth");
    static readonly int IdRot = Shader.PropertyToID("_LenticulationRotation");
    static readonly int IdGran = Shader.PropertyToID("_LenticulationGranularity");
    static readonly int IdLens = Shader.PropertyToID("_Lensing");
    static readonly int IdLensSize = Shader.PropertyToID("_LensSize");
    static readonly int IdEmission = Shader.PropertyToID("_EmissionColor");
    static readonly int IdLumTex = Shader.PropertyToID("_LuminanceTex");
    static readonly int IdLentTex = Shader.PropertyToID("_LenticulationTex");

    MaterialPropertyBlock _mpb;

    void Awake()
    {
        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        if (interiorLight == null) interiorLight = GetComponentInChildren<Light>();
        EnsureBreadPanMesh();
        ApplyOpticsToMaterials();
    }

    public void EnsureBreadPanMesh()
    {
        if (meshFilter == null)
            meshFilter = gameObject.AddComponent<MeshFilter>();
        if (meshRenderer == null)
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        if (meshFilter.sharedMesh == null)
            meshFilter.sharedMesh = BuildBreadPanMesh();
        if (interiorLight == null)
        {
            var lightGo = new GameObject("InteriorLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            interiorLight = lightGo.AddComponent<Light>();
            interiorLight.type = LightType.Point;
            interiorLight.range = 2.5f;
            interiorLight.intensity = 1.2f;
        }
    }

    public void ApplyEmission(Color emission, float intensity, float averageLuminance01)
    {
        if (interiorLight != null)
        {
            interiorLight.color = emission;
            interiorLight.intensity = intensity * Mathf.Lerp(0.15f, 1f, averageLuminance01);
        }
        if (meshRenderer == null) return;
        _mpb ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_mpb);
        _mpb.SetColor(IdEmission, emission * intensity * averageLuminance01);
        ApplyOpticsBlock(_mpb);
        meshRenderer.SetPropertyBlock(_mpb);
    }

    public void ApplyOpticsToMaterials()
    {
        if (meshRenderer == null) return;
        _mpb ??= new MaterialPropertyBlock();
        meshRenderer.GetPropertyBlock(_mpb);
        ApplyOpticsBlock(_mpb);
        meshRenderer.SetPropertyBlock(_mpb);
    }

    void ApplyOpticsBlock(MaterialPropertyBlock mpb)
    {
        mpb.SetFloat(IdSmooth, lenticulationSmooth01);
        mpb.SetFloat(IdRot, lenticulationRotationDeg * Mathf.Deg2Rad);
        mpb.SetFloat(IdGran, lenticulationGranularity);
        mpb.SetVector(IdLens, new Vector4(lensingVector2.x, lensingVector2.y, lensSize, 0f));
        mpb.SetFloat(IdLensSize, lensSize);
        if (luminanceTexture != null) mpb.SetTexture(IdLumTex, luminanceTexture);
        if (lenticulationTexture != null) mpb.SetTexture(IdLentTex, lenticulationTexture);
    }

    /// <summary>Trapezoid bread-pan: wide open top, narrower base.</summary>
    public static Mesh BuildBreadPanMesh()
    {
        var mesh = new Mesh { name = "PixelLightBreadPan" };
        // Top trapezoid (larger) Y=0.12, bottom Y=0
        float topW = 0.55f, topD = 0.28f;
        float botW = 0.4f, botD = 0.2f;
        float h = 0.12f;
        var v = new Vector3[]
        {
            new Vector3(-botW, 0, -botD), new Vector3(botW, 0, -botD),
            new Vector3(botW, 0, botD), new Vector3(-botW, 0, botD),
            new Vector3(-topW, h, -topD), new Vector3(topW, h, -topD),
            new Vector3(topW, h, topD), new Vector3(-topW, h, topD)
        };
        var tris = new int[]
        {
            // bottom
            0, 2, 1, 0, 3, 2,
            // top (transparent cover facing up)
            4, 5, 6, 4, 6, 7,
            // sides
            0, 1, 5, 0, 5, 4,
            1, 2, 6, 1, 6, 5,
            2, 3, 7, 2, 7, 6,
            3, 0, 4, 3, 4, 7
        };
        mesh.vertices = v;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
