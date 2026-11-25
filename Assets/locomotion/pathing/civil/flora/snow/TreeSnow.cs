using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class TreeSnowFeature
{
    public string id;
    public float catch01 = 0.4f;
    public float snowWeight;
    public bool prebaked = true;
    public SnowManager manager;
    public List<SnowContactBox> boxes = new List<SnowContactBox>();
}

/// <summary>One snow manager per branch or leaf clump, plus the conglomerate total.</summary>
[AddComponentMenu("Locomotion/Civil/Tree Snow Manager")]
public sealed class TreeSnowManager : MonoBehaviour
{
    public TreeGenConfig config;
    public List<TreeSnowFeature> features = new List<TreeSnowFeature>();
    public float average;
    public bool hasBake;
    public float lodRadius = 8f;
    public float released;

    public float Compute(IList<float> parts)
    {
        float sum = 0f;
        int n = parts != null ? parts.Count : 0;
        for (int i = 0; i < n; i++)
            sum += parts[i];
        float mean = n > 0 ? sum / n : 0f;
        if (!hasBake)
            average = mean;
        else
            average = (average + mean) * 0.5f;
        hasBake = true;
        EnsureFeatures(n, mean);
        return average;
    }

    public void ResetBake()
    {
        average = 0f;
        hasBake = false;
        if (features == null)
            return;
        for (int i = 0; i < features.Count; i++)
        {
            if (features[i] != null && features[i].manager != null)
                DestroyImmediate(features[i].manager);
        }
        features.Clear();
    }

    void EnsureFeatures(int count, float mean)
    {
        if (features == null)
            features = new List<TreeSnowFeature>();
        int n = Mathf.Max(1, count);
        while (features.Count > n)
        {
            int last = features.Count - 1;
            if (features[last] != null && features[last].manager != null)
                DestroyImmediate(features[last].manager);
            features.RemoveAt(last);
        }
        while (features.Count < n)
        {
            var feature = new TreeSnowFeature
            {
                id = "feature_" + features.Count,
                snowWeight = mean,
                prebaked = true,
                manager = gameObject.AddComponent<SnowManager>()
            };
            features.Add(feature);
        }
        for (int i = 0; i < features.Count; i++)
            features[i].snowWeight = mean;
    }

    public bool CloseEnough(Vector3 worldPoint)
    {
        return (worldPoint - transform.position).sqrMagnitude <= lodRadius * lodRadius;
    }

    public float Release(float destructibility01, float snowWeight, float retinueLoad)
    {
        float mass = Mathf.Max(0f, snowWeight) * Mathf.Max(0f, retinueLoad) * Mathf.Clamp01(destructibility01);
        released += mass;
        for (int i = 0; i < features.Count; i++)
        {
            TreeSnowFeature feature = features[i];
            if (feature == null)
                continue;
            feature.snowWeight = Mathf.Max(0f, feature.snowWeight - mass / Mathf.Max(1, features.Count));
        }
        return mass;
    }

    public void ReportBox(int featureIndex, SnowContactBox box, Vector3 observer)
    {
        if (box == null || featureIndex < 0 || featureIndex >= features.Count)
            return;
        if (!CloseEnough(observer))
            return;
        TreeSnowFeature feature = features[featureIndex];
        feature.prebaked = false;
        feature.boxes.Add(box);
        feature.snowWeight = box.snowAmount;
    }
}

/// <summary>Publishes snow load into the statistical retinue the way plant growth publishes an empower event.</summary>
public sealed class SnowWeatherAdaptorRetinue
{
    public float snowLoad = 1f;

    public void Publish(IStatisticalRetinueDao dao, string treeId)
    {
        GrowthEventBus.Publish(dao, GrowthEvent.SnowLoad(treeId, snowLoad));
    }

    public static float ShakeFraction(float branchImpulse, float snowWeight, float load)
    {
        float strength = Mathf.Max(0.01f, branchImpulse);
        return Mathf.Clamp01(snowWeight * load / strength);
    }
}
