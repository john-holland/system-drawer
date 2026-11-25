using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SnowGranularityLayer
{
    public string id = "pack";
    public float heightMinInches;
    public float heightMaxInches = 6f;
    public float lowerBlend = 0.25f;
    public float upperBlend = 0.25f;
    [Min(0.01f)] public float beadSize = 0.25f;

    public bool ContainsHeight(float inches)
    {
        return inches >= heightMinInches && inches <= heightMaxInches;
    }

    public float Blend(float inches)
    {
        if (inches < heightMinInches)
            return Mathf.Clamp01(1f - (heightMinInches - inches) / Mathf.Max(0.01f, lowerBlend));
        if (inches > heightMaxInches)
            return Mathf.Clamp01(1f - (inches - heightMaxInches) / Mathf.Max(0.01f, upperBlend));
        return 1f;
    }
}

/// <summary>Snow sitting on one renderer.</summary>
[AddComponentMenu("Locomotion/Civil/Snow Cover")]
public sealed class SnowCover : MonoBehaviour
{
    public Renderer target;
    [Min(0f)] public float depthInches;
    [Range(0f, 1f)] public float stickiness = 0.4f;
    [Range(0f, 1f)] public float firmness = 0.35f;
    [Range(0f, 1f)] public float pollenMix;
    public Color snowAlbedo = Color.white;
    public Color pollenAlbedo = new Color(0.93f, 0.86f, 0.45f, 1f);
    public SdfSealedClothSkin skin;
    public List<SnowGranularityLayer> layers = new List<SnowGranularityLayer>();

    public Color DisplayAlbedo => SnowAlbedo.Mix(snowAlbedo, pollenAlbedo, pollenMix);

    public SnowGranularityLayer LayerAt(float inches)
    {
        if (layers == null)
            return null;
        SnowGranularityLayer best = null;
        float bestBlend = -1f;
        for (int i = 0; i < layers.Count; i++)
        {
            SnowGranularityLayer layer = layers[i];
            if (layer == null)
                continue;
            float blend = layer.Blend(inches);
            if (blend > bestBlend)
            {
                best = layer;
                bestBlend = blend;
            }
        }
        return best;
    }

    public void PushToRenderer()
    {
        if (target == null)
            target = GetComponent<Renderer>();
        if (target == null)
            return;
        var block = new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        block.SetFloat(SnowOnSurfaceIds.Depth, depthInches);
        block.SetFloat(SnowOnSurfaceIds.Pollen, pollenMix);
        block.SetColor(SnowOnSurfaceIds.Color, snowAlbedo);
        block.SetColor(SnowOnSurfaceIds.PollenColor, pollenAlbedo);
        target.SetPropertyBlock(block);
    }
}

public static class SnowOnSurfaceIds
{
    public static readonly int Depth = Shader.PropertyToID("_DepthInches");
    public static readonly int Pollen = Shader.PropertyToID("_PollenMix");
    public static readonly int Color = Shader.PropertyToID("_SnowColor");
    public static readonly int PollenColor = Shader.PropertyToID("_PollenColor");
}

public static class SnowAlbedo
{
    public static Color Mix(Color snow, Color pollen, float mix)
    {
        if (mix <= 0f)
            return snow;
        return Color.Lerp(snow, pollen, Mathf.Clamp01(mix));
    }
}

/// <summary>Diggable snow pack. Mineral checks stay on soil because the kind is Snow.</summary>
[AddComponentMenu("Locomotion/Civil/Snow Volume")]
public sealed class SnowVolume : DiggableVolume
{
    public List<SnowGranularityLayer> layers = new List<SnowGranularityLayer>();
    public SdfSealedClothSkin snowSkin;

    void Awake()
    {
        volumeKind = DiggableVolumeKind.Snow;
        materialClass = "snow";
        if (layers == null || layers.Count == 0)
            layers = new List<SnowGranularityLayer> { new SnowGranularityLayer() };
        integralGranularity = layers[0].beadSize;
    }

    public void ApplyShellBand(float thickness)
    {
        iceBandThickness = Mathf.Max(0f, thickness);
    }
}

/// <summary>Registry of covers, ambulation histories, and live snowfalls.</summary>
[AddComponentMenu("Locomotion/Civil/Snow Manager")]
public sealed class SnowManager : MonoBehaviour
{
    public List<SnowCover> covers = new List<SnowCover>();
    public List<SnowAmbulationHistory> ambulation = new List<SnowAmbulationHistory>();
    public List<SnowFallEvent> falls = new List<SnowFallEvent>();
    public NarrativeCalendarHost calendar;

    public void Register(SnowCover cover)
    {
        if (cover != null && !covers.Contains(cover))
            covers.Add(cover);
    }

    public SnowFallEvent BeginFall(string id, IList<string> surfaces, Locomotion.Narrative.NarrativeDateTime when)
    {
        var fall = SnowFallEvent.Open(id, surfaces, when);
        falls.Add(fall);
        if (calendar != null && calendar.asset != null && fall.calendarEvent != null)
            calendar.asset.events.Add(fall.calendarEvent);
        return fall;
    }
}

[Serializable]
public sealed class SnowAmbulationHistory
{
    public string actorId;
    public string gaitId;
    public float snowLevelInches;
}

/// <summary>Optional calendar the snow manager writes falls into. Kept as a component so tests can omit it.</summary>
[AddComponentMenu("Locomotion/Civil/Narrative Calendar Host")]
public sealed class NarrativeCalendarHost : MonoBehaviour
{
    public Locomotion.Narrative.NarrativeCalendarAsset asset;
}
