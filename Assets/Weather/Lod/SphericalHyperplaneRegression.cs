using System;
using UnityEngine;

namespace Weather.Lod
{
    [Serializable]
    public sealed class HyperplaneLayer
    {
        public Vector3 normal = Vector3.up;
        public float offset;
        public Vector3 velocityBias;
        public float temperatureBias = 20f;
        public float pressureBias = 1013.25f;
        public float weight = 1f;
    }

    [Serializable]
    public sealed class SphericalHyperplaneRegression
    {
        public Vector3 center;
        public float effectiveRadius = float.PositiveInfinity;
        public HyperplaneLayer[] layers = Array.Empty<HyperplaneLayer>();
        public float residualVariance;

        public int LayerCount => layers?.Length ?? 0;

        public void FitFromSamples(Vector3 eggCenter, System.Collections.Generic.IReadOnlyList<ManifoldSample> samples,
            float linearityThreshold, int maxLayers)
        {
            center = eggCenter;
            maxLayers = Mathf.Clamp(maxLayers, 1, 8);
            if (samples == null || samples.Count == 0)
            {
                layers = new[] { CreateDefaultLayer() };
                residualVariance = 0f;
                effectiveRadius = float.PositiveInfinity;
                return;
            }

            Vector3 meanVel = Vector3.zero;
            float meanTemp = 0f;
            float meanPressure = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                meanVel += samples[i].data.velocity;
                meanTemp += samples[i].data.temperature;
                meanPressure += samples[i].data.pressure;
            }
            float inv = 1f / samples.Count;
            meanVel *= inv;
            meanTemp *= inv;
            meanPressure *= inv;

            float residual = 0f;
            for (int i = 0; i < samples.Count; i++)
            {
                Vector3 dv = samples[i].data.velocity - meanVel;
                residual += dv.sqrMagnitude;
            }
            residualVariance = residual * inv;

            if (residualVariance < linearityThreshold * linearityThreshold)
            {
                effectiveRadius = float.PositiveInfinity;
                layers = new[]
                {
                    new HyperplaneLayer
                    {
                        normal = meanVel.sqrMagnitude > 0.0001f ? meanVel.normalized : Vector3.forward,
                        offset = 0f,
                        velocityBias = meanVel,
                        temperatureBias = meanTemp,
                        pressureBias = meanPressure,
                        weight = 1f
                    }
                };
                return;
            }

            effectiveRadius = EstimateRadius(samples, eggCenter);
            int layerCount = Mathf.Min(maxLayers, 1 + Mathf.CeilToInt(residualVariance / Mathf.Max(0.01f, linearityThreshold)));
            layers = new HyperplaneLayer[layerCount];
            for (int l = 0; l < layerCount; l++)
            {
                float t = layerCount <= 1 ? 0f : l / (float)(layerCount - 1);
                Vector3 dir = FibonacciDirection(l, layerCount);
                layers[l] = new HyperplaneLayer
                {
                    normal = dir,
                    offset = t * effectiveRadius * 0.25f,
                    velocityBias = meanVel + dir * (residualVariance * 0.1f * (l + 1)),
                    temperatureBias = meanTemp,
                    pressureBias = meanPressure,
                    weight = 1f / layerCount
                };
            }
        }

        static HyperplaneLayer CreateDefaultLayer() => new HyperplaneLayer
        {
            normal = Vector3.up,
            velocityBias = Vector3.zero,
            temperatureBias = 20f,
            pressureBias = 1013.25f,
            weight = 1f
        };

        static float EstimateRadius(System.Collections.Generic.IReadOnlyList<ManifoldSample> samples, Vector3 eggCenter)
        {
            float maxR = 1f;
            for (int i = 0; i < samples.Count; i++)
            {
                float d = Vector3.Distance(samples[i].position, eggCenter);
                if (d > maxR)
                    maxR = d;
            }
            return maxR;
        }

        static Vector3 FibonacciDirection(int index, int count)
        {
            float golden = Mathf.PI * (3f - Mathf.Sqrt(5f));
            float y = 1f - (index / Mathf.Max(1f, count - 1f)) * 2f;
            float r = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
            float theta = golden * index;
            return new Vector3(Mathf.Cos(theta) * r, y, Mathf.Sin(theta) * r).normalized;
        }

        public ManifoldCellData Evaluate(Vector3 world)
        {
            if (layers == null || layers.Length == 0)
                return new ManifoldCellData { temperature = 20f, pressure = 1013.25f, density = 1.225f, mode = WeatherMode.Air };

            float[] scores = new float[layers.Length];
            float maxScore = float.NegativeInfinity;
            for (int i = 0; i < layers.Length; i++)
            {
                HyperplaneLayer layer = layers[i];
                Vector3 local = world - center;
                float planeDist = Vector3.Dot(layer.normal, local) - layer.offset;
                scores[i] = -planeDist * planeDist * layer.weight;
                if (scores[i] > maxScore)
                    maxScore = scores[i];
            }

            float sumExp = 0f;
            float[] weights = new float[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                weights[i] = Mathf.Exp(scores[i] - maxScore);
                sumExp += weights[i];
            }
            if (sumExp <= 0.0001f)
                sumExp = 1f;

            Vector3 vel = Vector3.zero;
            float temp = 0f;
            float pressure = 0f;
            for (int i = 0; i < layers.Length; i++)
            {
                float w = weights[i] / sumExp;
                HyperplaneLayer layer = layers[i];
                vel += layer.velocityBias * w;
                temp += layer.temperatureBias * w;
                pressure += layer.pressureBias * w;
            }

            return new ManifoldCellData
            {
                velocity = vel,
                temperature = temp,
                pressure = pressure,
                density = 1.225f,
                mode = WeatherMode.Wind
            };
        }

        public void PaintIntoManifold(WeatherPhysicsManifold manifold, Bounds bounds, float definitionLevel)
        {
            if (manifold == null || layers == null || layers.Length == 0)
                return;

            definitionLevel = Mathf.Clamp01(definitionLevel);
            int stride = Mathf.Max(1, manifold.advectionStride);
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            float step = Mathf.Max(manifold.cellResolution * stride, 0.5f);
            for (float x = min.x; x <= max.x; x += step)
            for (float y = min.y; y <= max.y; y += step)
            for (float z = min.z; z <= max.z; z += step)
            {
                Vector3 pos = new Vector3(x, y, z);
                ManifoldCellData predicted = Evaluate(pos);
                ManifoldCellData existing = manifold.GetDataAtPosition(pos);
                existing.velocity = Vector3.Lerp(existing.velocity, predicted.velocity, definitionLevel);
                existing.temperature = Mathf.Lerp(existing.temperature, predicted.temperature, definitionLevel);
                existing.pressure = Mathf.Lerp(existing.pressure, predicted.pressure, definitionLevel);
                manifold.SetDataAtPosition(pos, existing);
            }
        }
    }

    public struct ManifoldSample
    {
        public Vector3 position;
        public ManifoldCellData data;
    }
}
