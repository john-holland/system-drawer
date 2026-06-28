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

            float crustOuterRadius = radius;
            float maxSurfaceReliefM = Mathf.Clamp(radius * 0.05f, 0.5f, 500f);
            if (compositionProfile != null && atmosphere != null)
            {
                foreach (var layer in compositionProfile.layers)
                {
                    if (!layer.enabled)
                        continue;
                    switch (layer.layer)
                    {
                        case PlanetaryCompositionLayer.Core:
                        {
                            float coreOuter = radius + layer.shellOffsetMeters;
                            float coreInner = coreOuter - Mathf.Max(0.01f, layer.shellThicknessMeters);
                            if (coreInner <= 0.01f)
                                rootIdx = AddFilledSphereBlend(asset, rootIdx, coreOuter, layer, layer.weight);
                            else
                                rootIdx = AddAnnularShellBlend(asset, rootIdx, coreInner, coreOuter, layer, layer.weight);
                            break;
                        }
                        case PlanetaryCompositionLayer.Crust:
                            crustOuterRadius = radius + maxSurfaceReliefM;
                            rootIdx = AddAnnularShellBlend(
                                asset,
                                rootIdx,
                                radius - Mathf.Max(0f, layer.shellThicknessMeters),
                                crustOuterRadius,
                                layer,
                                layer.weight);
                            break;
                        case PlanetaryCompositionLayer.Water:
                            rootIdx = AddAnnularShellBlend(
                                asset,
                                rootIdx,
                                crustOuterRadius,
                                crustOuterRadius + Mathf.Max(1f, layer.shellThicknessMeters),
                                layer,
                                0.3f);
                            break;
                        case PlanetaryCompositionLayer.Atmosphere:
                            rootIdx = AddAnnularShellBlend(
                                asset,
                                rootIdx,
                                crustOuterRadius,
                                radius + atmosphere.troposphereTopM,
                                layer,
                                atmosphere.pressureScaleHeightM * 0.0001f);
                            break;
                        case PlanetaryCompositionLayer.Weather:
                            float cloudInner = radius + atmosphere.cloudBaseM;
                            float cloudOuter = radius + atmosphere.cloudTopM;
                            if (cloudInner < crustOuterRadius)
                                cloudInner = crustOuterRadius;
                            rootIdx = AddAnnularShellBlend(
                                asset,
                                rootIdx,
                                cloudInner,
                                cloudOuter,
                                layer,
                                atmosphere.cloudDensityCoeff);
                            break;
                        case PlanetaryCompositionLayer.Lava:
                            rootIdx = AddAnnularShellBlend(
                                asset,
                                rootIdx,
                                radius + layer.shellOffsetMeters - Mathf.Max(1f, layer.shellThicknessMeters),
                                radius + layer.shellOffsetMeters,
                                layer,
                                layer.weight);
                            break;
                        case PlanetaryCompositionLayer.Mantle:
                            rootIdx = AddAnnularShellBlend(
                                asset,
                                rootIdx,
                                radius + layer.shellOffsetMeters - Mathf.Max(1f, layer.shellThicknessMeters),
                                radius + layer.shellOffsetMeters,
                                layer,
                                layer.weight);
                            break;
                    }
                }
            }

            asset.rootNodeIndex = rootIdx;
            return asset;
        }

        static int AddAnnularShellBlend(
            SdfMaxCompositionAsset asset,
            int rootIdx,
            float innerRadius,
            float outerRadius,
            PlanetaryCompositionProfile.LayerSettings layer,
            float weight)
        {
            if (outerRadius <= innerRadius + 0.01f)
                return rootIdx;

            int outerIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.DisplacedSphere,
                sphereRadius = outerRadius,
                smoothRadius = layer.smoothRadius,
                weight = weight * layer.weight
            });
            int innerIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.DisplacedSphere,
                sphereRadius = innerRadius,
                smoothRadius = layer.smoothRadius,
                weight = weight * layer.weight
            });
            int bandIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.Subtract,
                childIndexA = outerIdx,
                childIndexB = innerIdx
            });
            int blendIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.SmoothMax,
                childIndexA = rootIdx,
                childIndexB = bandIdx,
                smoothRadius = layer.smoothRadius
            });
            return blendIdx;
        }

        static int AddFilledSphereBlend(
            SdfMaxCompositionAsset asset,
            int rootIdx,
            float sphereRadius,
            PlanetaryCompositionProfile.LayerSettings layer,
            float weight)
        {
            if (sphereRadius <= 0.01f)
                return rootIdx;

            int sphereIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.DisplacedSphere,
                sphereRadius = sphereRadius,
                smoothRadius = layer.smoothRadius,
                weight = weight * layer.weight
            });
            return AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.SmoothMax,
                childIndexA = rootIdx,
                childIndexB = sphereIdx,
                smoothRadius = layer.smoothRadius
            });
        }

        static int AddNode(SdfMaxCompositionAsset asset, SdfMaxNode node)
        {
            int idx = asset.nodes.Count;
            asset.nodes.Add(node);
            return idx;
        }
    }
}
