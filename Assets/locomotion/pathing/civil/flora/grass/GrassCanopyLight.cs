using UnityEngine;

public enum CanopyLightLod
{
    MicroBsdf = 0,
    MesoVolumetricDice = 1,
    MacroBeerLambert = 2,
    InfiniteRaymarch = 3
}

public enum GrassFrustumMode
{
    EmotionalPlane = 0,
    MemoryRecall = 1,
    SummoningPortal = 2,
    FieldPortal = 3
}

/// <summary>
/// Screen-space canopy gather. Wet and dry neighbors do not pool light across a shoreline.
/// </summary>
public static class ScreenSpaceConvolutionKernel
{
    public static readonly float[] GaussianWeights = { 0.227027f, 0.1945946f, 0.1216216f, 0.054054f, 0.016216f };

    public struct Sample
    {
        public Color albedo;
        public float depth;
        public Vector3 world;
    }

    public static float DepthGate(float centerDepth, float neighborDepth, float threshold, bool centerWet, bool neighborWet)
    {
        if (centerWet != neighborWet)
            return 0f;
        return Mathf.Exp(-Mathf.Abs(centerDepth - neighborDepth) * Mathf.Max(0f, threshold));
    }

    public static bool Submerged(float worldY, float waterSurfaceY)
    {
        return waterSurfaceY - worldY > 0.001f;
    }

    public static Color Absorb(Color rgb, Vector3 absorption, float depth)
    {
        float d = Mathf.Max(0f, depth);
        rgb.r *= Mathf.Exp(-Mathf.Max(0f, absorption.x) * d);
        rgb.g *= Mathf.Exp(-Mathf.Max(0f, absorption.y) * d);
        rgb.b *= Mathf.Exp(-Mathf.Max(0f, absorption.z) * d);
        return rgb;
    }

    public static Vector2 RefractionOffset(Vector2 waveNormal, float submersion, float iorRatio, float strength)
    {
        if (submersion <= 0.001f)
            return Vector2.zero;
        return waveNormal * iorRatio * strength * Mathf.Min(submersion, 2f);
    }

    public static Color Convolve(
        Sample center, Sample neighbor, float depthThreshold, float thatchBias, float waterSurfaceY, Vector3 absorption, int tap)
    {
        int index = Mathf.Clamp(tap, 0, GaussianWeights.Length - 1);
        bool centerWet = Submerged(center.world.y, waterSurfaceY);
        bool neighborWet = Submerged(neighbor.world.y, waterSurfaceY);
        float gate = DepthGate(center.depth, neighbor.depth, depthThreshold, centerWet, neighborWet);
        float layer = Mathf.Clamp01(center.albedo.a);
        float density = Mathf.Pow(Mathf.Max(layer, 1e-4f), Mathf.Max(0f, thatchBias));
        Color sampled = neighbor.albedo;
        if (centerWet)
        {
            float depth = Mathf.Max(0f, waterSurfaceY - neighbor.world.y);
            sampled = Absorb(sampled, absorption, depth);
        }
        float weight = GaussianWeights[index] * gate * density;
        sampled.r *= weight;
        sampled.g *= weight;
        sampled.b *= weight;
        sampled.a = weight;
        return sampled;
    }

    public static Color Resolve(Color weightedSum, float weightTotal, Color centerAlbedo, float mix)
    {
        float w = Mathf.Max(weightTotal, 0.0001f);
        Color pooled = new Color(weightedSum.r / w, weightedSum.g / w, weightedSum.b / w, 1f);
        return Color.Lerp(centerAlbedo, pooled, Mathf.Clamp01(mix));
    }

    public static Color InvertToward(Color pooled, Color target, float influence)
    {
        return Color.Lerp(pooled, target, Mathf.Clamp01(influence));
    }

    public static Color PrebakeLod(CanopyLightLod lod, Color albedo, float layerHeight, float opticalDepth)
    {
        float h = Mathf.Clamp01(layerHeight);
        switch (lod)
        {
            case CanopyLightLod.MicroBsdf:
                return albedo * (0.5f + 0.5f * h);
            case CanopyLightLod.MesoVolumetricDice:
                return albedo * (1f - 0.35f * (1f - h));
            case CanopyLightLod.MacroBeerLambert:
                return albedo * Mathf.Exp(-Mathf.Max(0f, opticalDepth) * (1f - h));
            default:
                return albedo;
        }
    }

    public static Color RaymarchColumn(float[] density, Color[] colors)
    {
        Color acc = new Color(0f, 0f, 0f, 0f);
        if (density == null || colors == null)
            return acc;
        int n = Mathf.Min(density.Length, colors.Length);
        for (int i = n - 1; i >= 0; i--)
        {
            float layerHeight = n <= 1 ? 1f : i / (float)(n - 1);
            if (density[i] <= 1f - layerHeight)
                continue;
            Color layer = colors[i];
            layer.a = Mathf.Clamp01(density[i] * layerHeight);
            layer.r *= layer.a;
            layer.g *= layer.a;
            layer.b *= layer.a;
            float keep = 1f - acc.a;
            acc.r += layer.r * keep;
            acc.g += layer.g * keep;
            acc.b += layer.b * keep;
            acc.a += layer.a * keep;
            if (acc.a >= 0.95f)
                break;
        }
        return acc;
    }
}

[AddComponentMenu("Locomotion/Civil/Screen Space Convolution Kernel Renderer")]
public class ScreenSpaceConvolutionKernelRenderer : MonoBehaviour
{
    public CanopyLightLod lod = CanopyLightLod.MacroBeerLambert;
    public float opticalDepth = 1.2f;
    [Range(0f, 1f)] public float layerHeight = 1f;
    public Color albedo = new Color(0.32f, 0.58f, 0.24f, 1f);
    public ComputeShader convolve;
    public ComputeShader raymarch;
    public float depthThreshold = 8f;
    public float thatchDensityBias = 1f;
    public float waterSurfaceY = -999f;
    public Vector3 waterAbsorption = new Vector3(0.45f, 0.12f, 0.04f);
    public float refractionIndexRatio = 0.75f;
    public float refractionStrength = 0.02f;
    public Color prebaked = Color.black;

    public virtual Color Prebake()
    {
        if (lod == CanopyLightLod.InfiniteRaymarch)
        {
            float[] density = { 0.2f, 0.45f, 0.7f, 0.9f };
            Color[] colors =
            {
                albedo * 0.45f,
                albedo * 0.7f,
                albedo,
                albedo * 1.1f
            };
            prebaked = ScreenSpaceConvolutionKernel.RaymarchColumn(density, colors);
        }
        else
            prebaked = ScreenSpaceConvolutionKernel.PrebakeLod(lod, albedo, layerHeight, opticalDepth);
        return prebaked;
    }
}

[AddComponentMenu("Locomotion/Civil/Inverted Screen Space Convolution Kernel Renderer")]
public sealed class InvertedScreenSpaceConvolutionKernelRenderer : ScreenSpaceConvolutionKernelRenderer
{
    public Color targetColor = Color.white;
    [Range(0f, 1f)] public float influence = 0.35f;

    public override Color Prebake()
    {
        Color pooled = base.Prebake();
        prebaked = ScreenSpaceConvolutionKernel.InvertToward(pooled, targetColor, influence);
        return prebaked;
    }
}

/// <summary>Frustum quad that maps an image, a video frame, or a portal into the grass canopy.</summary>
[AddComponentMenu("Locomotion/Civil/Grass Frustum Display")]
public sealed class GrassFrustumDisplay : MonoBehaviour
{
    public GrassFrustumMode mode = GrassFrustumMode.EmotionalPlane;
    public Texture source;
    public InvertedScreenSpaceConvolutionKernelRenderer kernel;
    public int layerCount = 4;
    public bool depthGated = true;

    public Color Present(Color canopy)
    {
        Color target = source != null ? Average(source) : canopy;
        if (kernel != null)
        {
            kernel.targetColor = target;
            return kernel.Prebake();
        }
        return ScreenSpaceConvolutionKernel.InvertToward(canopy, target, mode == GrassFrustumMode.FieldPortal ? 0.5f : 0.35f);
    }

    static Color Average(Texture texture)
    {
        if (texture is Texture2D tex && tex.isReadable)
            return tex.GetPixelBilinear(0.5f, 0.5f);
        return new Color(0.45f, 0.55f, 0.35f, 1f);
    }
}
