using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PlantLayerDurationTexture
{
    public string layerId = "sapwood";
    public float durationSec = 31_536_000f;
    public Texture2D albedo;
    [Range(0f, 2f)] public float fresnel01 = 0.35f;
}

/// <summary>Phloem down, sapwood up, cambium, rings, heartwood.</summary>
[AddComponentMenu("Locomotion/Civil/Tree Health Bio Rhythm")]
public sealed class TreeHealthBioRhythm : MonoBehaviour
{
    [Range(0f, 1f)] public float phloemDown01 = 0.5f;
    [Range(0f, 1f)] public float sapwoodUp01 = 0.5f;
    [Range(0f, 1f)] public float cambium01 = 0.2f;
    [Range(0f, 1f)] public float heartwood01;
    public float growthAgeSec;
    public List<PlantLayerDurationTexture> layers = new List<PlantLayerDurationTexture>();
    public TreeGrowthTravelAgent growthAgent;

    void Awake()
    {
        if (growthAgent == null)
            growthAgent = GetComponent<TreeGrowthTravelAgent>();
        if (layers.Count == 0)
        {
            layers.Add(new PlantLayerDurationTexture { layerId = "bark", durationSec = 1f });
            layers.Add(new PlantLayerDurationTexture { layerId = "phloem", durationSec = 8f });
            layers.Add(new PlantLayerDurationTexture { layerId = "cambium", durationSec = 2f });
            layers.Add(new PlantLayerDurationTexture { layerId = "sapwood", durationSec = 20f });
            layers.Add(new PlantLayerDurationTexture { layerId = "heartwood", durationSec = 40f });
        }
    }

    public void Tick(float dt)
    {
        growthAgeSec += Mathf.Max(0f, dt);
        phloemDown01 = Mathf.Clamp01(phloemDown01 + dt * 0.01f);
        sapwoodUp01 = Mathf.Clamp01(sapwoodUp01 + dt * 0.008f);
        cambium01 = Mathf.Clamp01(0.15f + Mathf.Repeat(growthAgeSec * 0.001f, 0.4f));
        heartwood01 = Mathf.Clamp01(growthAgeSec / 400f);
    }
}

[AddComponentMenu("Locomotion/Travel/Tree Physics Manifold Growth Travel Agent")]
public sealed class TreePhysicsManifoldGrowthTravelAgent : TreeGrowthTravelAgent
{
    public TreeHealthBioRhythm health;
    public Weather.WeatherPhysicsManifold manifold;

    void Reset()
    {
        enforceNaturalGrowthFromPhysicsManifolds = true;
        health = GetComponent<TreeHealthBioRhythm>();
    }
}
