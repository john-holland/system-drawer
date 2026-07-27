using System;
using UnityEngine;

/// <summary>Per-garment UV stretch layer: renderer, masks, elastic params.</summary>
[Serializable]
public sealed class ClothUvStretchLayer
{
    public string layerId = "singlet";
    public Renderer renderer;
    public Material materialOverride;
    public ClothElasticProperties elastic = new ClothElasticProperties();

    [Tooltip("R = where fabric may slide (1) vs stick (0).")]
    public Texture2D slideMaskTex;

    [Tooltip("G or grayscale = local elastic scale (lycra vs leather).")]
    public Texture2D elasticMaskTex;

    [Tooltip("Optional layer-pair id mask.")]
    public Texture2D layerIdMask;

    [NonSerialized] public Vector2 slipUv;
    [NonSerialized] public Vector2 slipVelocity;
    [NonSerialized] public float strain01;
    [NonSerialized] public float contactWeight01;

    public Material ResolveMaterial()
    {
        if (materialOverride != null) return materialOverride;
        return renderer != null ? renderer.material : null;
    }
}
