using UnityEngine;

/// <summary>
/// Drives InkDryingLayer: 30s see-through on dry start, then opaque. Gloss follows wet/dry specular.
/// </summary>
[AddComponentMenu("Locomotion/Painting/Ink Drying Layer Driver")]
public sealed class InkDryingLayerDriver : MonoBehaviour
{
    public InkMaterialProfile ink;
    public PaintCanvas canvas;
    public Renderer targetRenderer;
    public Material layerMaterial;
    public InkDryingNarrativeBridge narrative;

    public bool drying;
    public bool seeThrough = true;
    public bool opaqueNotified;
    public float dry01;
    public float elapsedSeconds;
    [Min(0f)] public float seeThroughDrySeconds = 30f;

    const string ShaderName = "Locomotion/Painting/InkDryingLayer";

    public InkMaterialProfile ResolveInk()
    {
        if (ink != null) return ink;
        if (canvas != null && canvas.inkProfile != null) return canvas.inkProfile;
        return InkMaterialProfile.CreateInkDefaults();
    }

    public Material EnsureMaterial()
    {
        if (layerMaterial != null) return layerMaterial;
        var sh = Shader.Find(ShaderName);
        if (sh == null)
            sh = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
        layerMaterial = new Material(sh) { name = "InkDryingLayer" };
        if (targetRenderer != null)
            targetRenderer.material = layerMaterial;
        return layerMaterial;
    }

    public void BeginDry()
    {
        var profile = ResolveInk();
        seeThroughDrySeconds = profile.seeThroughDrySeconds;
        drying = true;
        seeThrough = true;
        opaqueNotified = false;
        dry01 = 0f;
        elapsedSeconds = 0f;
        Bind();
        if (narrative != null)
            narrative.EnqueueDryStart();
        else
            GetComponent<InkDryingNarrativeBridge>()?.EnqueueDryStart();
    }

    public void Tick(float dt)
    {
        if (!drying) return;
        elapsedSeconds += Mathf.Max(0f, dt);
        ApplyElapsed(elapsedSeconds);
    }

    public void ApplyElapsed(float elapsed)
    {
        elapsedSeconds = Mathf.Max(0f, elapsed);
        var profile = ResolveInk();
        float window = Mathf.Max(0.0001f, seeThroughDrySeconds > 0f ? seeThroughDrySeconds : profile.seeThroughDrySeconds);
        seeThrough = elapsedSeconds < window;
        dry01 = Mathf.Clamp01(elapsedSeconds / window);
        if (!seeThrough && !opaqueNotified)
        {
            opaqueNotified = true;
            if (narrative != null)
                narrative.EnqueueDryOpaque();
            else
                GetComponent<InkDryingNarrativeBridge>()?.EnqueueDryOpaque();
        }
        Bind();
    }

    void LateUpdate()
    {
        if (drying)
            Tick(Time.deltaTime);
        else
            Bind();
    }

    public void Bind()
    {
        var mat = EnsureMaterial();
        var profile = ResolveInk();
        float spec = Mathf.Lerp(profile.specularWet, profile.specularDry, dry01);
        Color albedo = profile.defaultInkColor;
        if (canvas != null && canvas.layerStack != null)
        {
            var layer = canvas.layerStack.TopWetLayer();
            if (layer != null)
            {
                albedo = layer.albedo;
                if (!drying)
                    dry01 = layer.dry01;
            }
        }
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", albedo);
        if (mat.HasProperty("_Dry01"))
            mat.SetFloat("_Dry01", dry01);
        if (mat.HasProperty("_Specular"))
            mat.SetFloat("_Specular", spec);
        if (mat.HasProperty("_SeeThrough"))
            mat.SetFloat("_SeeThrough", seeThrough ? 1f : 0f);
        if (mat.HasProperty("_SeeThroughAlpha"))
            mat.SetFloat("_SeeThroughAlpha", profile.seeThroughAlpha);
        if (mat.HasProperty("_Glossiness"))
            mat.SetFloat("_Glossiness", spec);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", spec);
        if (targetRenderer != null)
            targetRenderer.material = mat;
    }
}
