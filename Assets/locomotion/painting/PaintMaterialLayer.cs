using UnityEngine;

/// <summary>
/// Binds albedo/spec/rough from a PaintLayerExpression onto a surface material.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Paint Material Layer")]
public sealed class PaintMaterialLayer : MonoBehaviour
{
    public PaintCanvas canvas;
    public int layerIndex;
    public Renderer targetRenderer;

    void LateUpdate()
    {
        if (canvas == null || canvas.layerStack == null || targetRenderer == null) return;
        var layers = canvas.layerStack.layers;
        if (layerIndex < 0 || layerIndex >= layers.Count) return;
        var layer = layers[layerIndex];
        if (layer == null) return;

        var mat = layer.material != null ? layer.material : targetRenderer.material;
        if (layer.material != null)
            targetRenderer.material = layer.material;
        mat.color = Color.Lerp(layer.albedo, Color.white, layer.dry01 * 0.15f);
        // Hydro drives specular (matte beads vs semi-gloss film); dry still kills gloss
        float gloss = Mathf.Lerp(layer.specular, 0.1f, layer.dry01);
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", gloss);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", gloss);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", Mathf.Lerp(0.05f, 0f, layer.dry01));
        if (mat.HasProperty("_SpecColor"))
            mat.SetColor("_SpecColor", Color.Lerp(Color.white, layer.albedo, 1f - layer.specular));
        if (mat.HasProperty("_GlossyReflections") == false && mat.HasProperty("_Roughness"))
            mat.SetFloat("_Roughness", layer.roughness);
    }
}
