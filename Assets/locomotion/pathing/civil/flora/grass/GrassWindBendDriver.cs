using UnityEngine;

/// <summary>Pushes a grass radial cache, cut, and capsule inclusion onto GrassWindBend.</summary>
[AddComponentMenu("Locomotion/Civil/Grass Wind Bend Driver")]
public sealed class GrassWindBendDriver : MonoBehaviour
{
    public WindAdvectionPlantRadialCachingService service;
    public int subjectIndex;
    public Renderer target;
    [Range(0f, 1f)] public float cutAlong = 1f;
    public float layerHeight = 1f;
    public Vector2 macroScale = new Vector2(0.05f, 0.05f);
    public Vector2 microScale = new Vector2(0.2f, 0.2f);
    public float macroVelocity = 0.35f;
    public float microVelocity = 0.8f;

    static readonly int WindCacheId = Shader.PropertyToID("_WindCache");
    static readonly int MacroWindId = Shader.PropertyToID("_MacroWind");
    static readonly int CutAlongId = Shader.PropertyToID("_CutAlong");
    static readonly int LayerHeightId = Shader.PropertyToID("_LayerHeight");
    static readonly int MacroScaleId = Shader.PropertyToID("_MacroScale");
    static readonly int MicroScaleId = Shader.PropertyToID("_MicroScale");
    static readonly int MacroVelocityId = Shader.PropertyToID("_MacroVelocity");
    static readonly int MicroVelocityId = Shader.PropertyToID("_MicroVelocity");
    static readonly int Block0Id = Shader.PropertyToID("_BlockAlong0");
    static readonly int Block1Id = Shader.PropertyToID("_BlockAlong1");
    static readonly int BlockOnId = Shader.PropertyToID("_BlockMotion");

    public void Apply()
    {
        if (target == null)
            target = GetComponent<Renderer>();
        if (target == null || service == null || service.copse == null)
            return;
        if (subjectIndex < 0 || subjectIndex >= service.copse.Count)
            return;
        PlantWindSubject subject = service.copse[subjectIndex];
        if (subject == null || subject.cache == null)
            return;
        Material material = target.material;
        if (subject.cache.texture != null)
            material.SetTexture(WindCacheId, subject.cache.texture);
        if (subject.cache.macroTexture != null)
            material.SetTexture(MacroWindId, subject.cache.macroTexture);
        material.SetFloat(CutAlongId, cutAlong);
        material.SetFloat(LayerHeightId, layerHeight);
        material.SetVector(MacroScaleId, macroScale);
        material.SetVector(MicroScaleId, microScale);
        material.SetFloat(MacroVelocityId, macroVelocity);
        material.SetFloat(MicroVelocityId, microVelocity);
        bool blocked = subject.cache.motionBlocks.Count > 0;
        material.SetFloat(BlockOnId, blocked ? 1f : 0f);
        if (blocked)
        {
            GrassMotionBlock block = subject.cache.motionBlocks[0];
            material.SetFloat(Block0Id, block.along0);
            material.SetFloat(Block1Id, block.along1);
        }
        if (service.passCapsules != null)
            service.passCapsules.Bind(material, subject.cache.omitBend);
    }

    void LateUpdate()
    {
        Apply();
    }
}
