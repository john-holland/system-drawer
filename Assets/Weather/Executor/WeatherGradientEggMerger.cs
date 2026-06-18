using System.Collections.Generic;
using UnityEngine;
using Weather.Lod;

namespace Weather.Executor
{
    public sealed class WeatherGradientEggMerger
    {
        public void MergeClientPayloads(
            IReadOnlyList<WeatherEggClientPayload> payloads,
            WeatherPhysicsManifold manifold,
            float definitionLevel)
        {
            if (payloads == null || payloads.Count == 0 || manifold == null)
                return;

            for (int p = 0; p < payloads.Count; p++)
            {
                WeatherEggClientPayload payload = payloads[p];
                if (payload == null)
                    continue;

                float weight = WeatherKalmanMerge.ServerClientWeight(payload.confidence, payload.timeoutOrder);
                SphericalHyperplaneRegression regression = HyperplaneWeatherDiffCodec.DecodeRegression(payload.regressionPayload);
                if (regression != null)
                {
                    Bounds bounds = WeatherEggBounds.GetAabb(payload.eggCenter, payload.eggRadii);
                    regression.PaintIntoManifold(manifold, bounds, definitionLevel * weight);
                }

                if (payload.sparseDiffPayload != null && payload.sparseDiffPayload.Length > 0)
                    HyperplaneWeatherDiffCodec.ApplySparseDiff(payload.sparseDiffPayload, manifold);
            }
        }

        public void MergeOverlappingEggs(PlayerWeatherEggRegistry registry, WeatherPhysicsManifold manifold)
        {
            if (registry == null || manifold == null || registry.Eggs.Count < 2)
                return;

            for (int i = 0; i < registry.Eggs.Count; i++)
            {
                PlayerWeatherEggZone a = registry.Eggs[i];
                if (a == null)
                    continue;
                for (int j = i + 1; j < registry.Eggs.Count; j++)
                {
                    PlayerWeatherEggZone b = registry.Eggs[j];
                    if (b == null)
                        continue;
                    Bounds overlap = WeatherEggBounds.GetAabb(a.Center, a.Radii);
                    overlap.Encapsulate(WeatherEggBounds.GetAabb(b.Center, b.Radii));
                    float step = Mathf.Max(manifold.cellResolution * manifold.advectionStride, 0.5f);
                    Vector3 min = overlap.min;
                    Vector3 max = overlap.max;
                    for (float x = min.x; x <= max.x; x += step)
                    for (float y = min.y; y <= max.y; y += step)
                    for (float z = min.z; z <= max.z; z += step)
                    {
                        Vector3 pos = new Vector3(x, y, z);
                        if (!a.Contains(pos) || !b.Contains(pos))
                            continue;
                        float wa = registry.ComputeOverlapWeight(a, b, pos);
                        ManifoldCellData da = a.QueryLocal(pos, manifold);
                        ManifoldCellData db = b.QueryLocal(pos, manifold);
                        ManifoldCellData merged = WeatherKalmanMerge.BlendCells(da, db, wa);
                        manifold.SetDataAtPosition(pos, merged);
                    }
                }
            }
        }
    }
}
