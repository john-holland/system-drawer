using UnityEngine;

/// <summary>
/// Ink vs paintlike film: fast SPH dry, thin layers, high dilution, single-layer mix by default.
/// </summary>
[CreateAssetMenu(fileName = "InkMaterialProfile", menuName = "Locomotion/Painting/Ink Material Profile")]
public sealed class InkMaterialProfile : ScriptableObject
{
    [Tooltip("When false, each deposit dilutes into the top wet layer. Paintlike ink keeps stacked films.")]
    public bool paintlikeInk;
    public bool singleLayerMixing = true;
    [Range(0f, 1f)] public float dilution = 0.75f;
    [Min(0.00005f)] public float layerThicknessM = 0.0004f;
    [Tooltip("SPH wet decay scale. Paint hydro uses 0.02; ink defaults much faster.")]
    [Min(0.01f)] public float sphDryRate = 0.45f;
    [Range(0f, 1f)] public float specularWet = 0.85f;
    [Range(0f, 1f)] public float specularDry = 0.08f;
    [Tooltip("Seconds the film stays see-through while drying (whiteboard spy gag).")]
    [Min(0f)] public float seeThroughDrySeconds = 30f;
    [Range(0f, 1f)] public float seeThroughAlpha = 0.12f;
    public Color defaultInkColor = new Color(0.05f, 0.06f, 0.12f, 1f);

    public static InkMaterialProfile CreateInkDefaults()
    {
        var p = CreateInstance<InkMaterialProfile>();
        p.name = "InkDefaults";
        p.paintlikeInk = false;
        p.singleLayerMixing = true;
        p.dilution = 0.75f;
        p.layerThicknessM = 0.0004f;
        p.sphDryRate = 0.45f;
        p.specularWet = 0.85f;
        p.specularDry = 0.08f;
        p.seeThroughDrySeconds = 30f;
        p.seeThroughAlpha = 0.12f;
        return p;
    }

    public bool MixesIntoSingleLayer => singleLayerMixing && !paintlikeInk;
}
