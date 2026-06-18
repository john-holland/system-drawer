using System;
using System.Collections.Generic;
using UnityEngine;

namespace Weather.Lod
{
    [Serializable]
    public sealed class WeatherManifoldDiffEntry
    {
        public Vector3 position;
        public ManifoldCellData data;
    }

    [Serializable]
    public sealed class WeatherManifoldDiffBundle
    {
        public WeatherManifoldDiffEntry[] entries;
    }

    [Serializable]
    public sealed class HyperplaneRegressionPayload
    {
        public Vector3 center;
        public float effectiveRadius;
        public float residualVariance;
        public HyperplaneLayer[] layers;
    }

    /// <summary>Encodes egg updates as hyperplane regression and/or sparse manifold diff.</summary>
    public static class HyperplaneWeatherDiffCodec
    {
        public static byte[] EncodeRegression(SphericalHyperplaneRegression regression)
        {
            if (regression == null)
                return null;
            var payload = new HyperplaneRegressionPayload
            {
                center = regression.center,
                effectiveRadius = float.IsPositiveInfinity(regression.effectiveRadius) ? -1f : regression.effectiveRadius,
                residualVariance = regression.residualVariance,
                layers = regression.layers
            };
            return System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
        }

        public static SphericalHyperplaneRegression DecodeRegression(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            var payload = JsonUtility.FromJson<HyperplaneRegressionPayload>(json);
            if (payload == null)
                return null;
            return new SphericalHyperplaneRegression
            {
                center = payload.center,
                effectiveRadius = payload.effectiveRadius < 0f ? float.PositiveInfinity : payload.effectiveRadius,
                residualVariance = payload.residualVariance,
                layers = payload.layers ?? Array.Empty<HyperplaneLayer>()
            };
        }

        public static byte[] EncodeSparseDiff(WeatherPhysicsManifold manifold, Bounds bounds, int stride)
        {
            if (manifold == null)
                return null;
            var entries = new List<WeatherManifoldDiffEntry>();
            stride = Mathf.Max(1, stride);
            float step = Mathf.Max(manifold.cellResolution * stride, 0.5f);
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (float x = min.x; x <= max.x; x += step)
            for (float y = min.y; y <= max.y; y += step)
            for (float z = min.z; z <= max.z; z += step)
            {
                var pos = new Vector3(x, y, z);
                entries.Add(new WeatherManifoldDiffEntry
                {
                    position = pos,
                    data = manifold.GetDataAtPosition(pos)
                });
            }
            return System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(new WeatherManifoldDiffBundle { entries = entries.ToArray() }));
        }

        public static void ApplySparseDiff(byte[] bytes, WeatherPhysicsManifold manifold)
        {
            if (bytes == null || bytes.Length == 0 || manifold == null)
                return;
            string json = System.Text.Encoding.UTF8.GetString(bytes);
            WeatherManifoldDiffBundle bundle = JsonUtility.FromJson<WeatherManifoldDiffBundle>(json);
            if (bundle?.entries == null)
                return;
            for (int i = 0; i < bundle.entries.Length; i++)
            {
                WeatherManifoldDiffEntry e = bundle.entries[i];
                if (e == null)
                    continue;
                manifold.SetDataAtPosition(e.position, e.data);
            }
        }

        public static List<ManifoldSample> CollectSamples(WeatherPhysicsManifold manifold, Vector3 center, Vector3 radii, int stride)
        {
            var samples = new List<ManifoldSample>(64);
            if (manifold == null)
                return samples;
            Bounds bounds = WeatherEggBounds.GetAabb(center, radii);
            float step = Mathf.Max(manifold.cellResolution * Mathf.Max(1, stride), 0.5f);
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (float x = min.x; x <= max.x; x += step)
            for (float y = min.y; y <= max.y; y += step)
            for (float z = min.z; z <= max.z; z += step)
            {
                Vector3 pos = new Vector3(x, y, z);
                if (!WeatherEggBounds.Contains(center, radii, pos))
                    continue;
                samples.Add(new ManifoldSample
                {
                    position = pos,
                    data = manifold.GetDataAtPosition(pos)
                });
            }
            return samples;
        }
    }
}
