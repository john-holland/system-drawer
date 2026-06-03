using System.Collections.Generic;
using Planetary.Elemental;
using SdfMax;
using UnityEngine;

namespace Planetary.Composition
{
    public static class PlanetaryCompositionBaker
    {
        public static SdfMaxCompositionAsset Bake(
            PlanetBody body,
            PlanetaryPlanarBase planar,
            SdfMaxSolverProfile profile,
            PlanetaryCompositionProfile compositionProfile,
            AtmosphereRegressionProfile atmosphere,
            PlateDefinition[] plates)
        {
            var baseAsset = SdfMaxPlanetCompositionBuilder.Build(body, planar, profile);
            var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            asset.nodes = new List<SdfMaxNode>(baseAsset.nodes);
            float radius = body != null ? body.PlanetRadius : 1000f;
            int rootIdx = baseAsset.rootNodeIndex;

            if (compositionProfile != null && atmosphere != null)
            {
                foreach (var layer in compositionProfile.layers)
                {
                    if (!layer.enabled)
                        continue;
                    switch (layer.layer)
                    {
                        case PlanetaryCompositionLayer.Water:
                            rootIdx = AddShellBlend(asset, rootIdx, radius + layer.shellOffsetMeters, layer, 0.3f);
                            break;
                        case PlanetaryCompositionLayer.Atmosphere:
                            float atmosR = radius + atmosphere.troposphereTopM;
                            rootIdx = AddShellBlend(asset, rootIdx, atmosR, layer, atmosphere.pressureScaleHeightM * 0.0001f);
                            break;
                        case PlanetaryCompositionLayer.Weather:
                            rootIdx = AddShellBlend(asset, rootIdx, radius + atmosphere.cloudBaseM, layer, atmosphere.cloudDensityCoeff);
                            rootIdx = AddShellBlend(asset, rootIdx, radius + atmosphere.cloudTopM, layer, atmosphere.cloudDensityCoeff * 0.5f);
                            break;
                        case PlanetaryCompositionLayer.Lava:
                            rootIdx = AddShellBlend(asset, rootIdx, radius + layer.shellOffsetMeters, layer, layer.weight);
                            break;
                        case PlanetaryCompositionLayer.Mantle:
                            rootIdx = AddShellBlend(asset, rootIdx, radius + layer.shellOffsetMeters, layer, layer.weight);
                            break;
                    }
                }
            }

            asset.rootNodeIndex = rootIdx;
            return asset;
        }

        static int AddShellBlend(SdfMaxCompositionAsset asset, int rootIdx, float shellRadius, PlanetaryCompositionProfile.LayerSettings layer, float weight)
        {
            int shellIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.DisplacedSphere,
                sphereRadius = shellRadius,
                smoothRadius = layer.smoothRadius,
                weight = weight * layer.weight
            });
            int blendIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.SmoothMax,
                childIndexA = rootIdx,
                childIndexB = shellIdx,
                smoothRadius = layer.smoothRadius
            });
            return blendIdx;
        }

        static int AddNode(SdfMaxCompositionAsset asset, SdfMaxNode node)
        {
            int idx = asset.nodes.Count;
            asset.nodes.Add(node);
            return idx;
        }
    }
}
