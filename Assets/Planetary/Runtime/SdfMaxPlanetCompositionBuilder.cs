using System.Collections.Generic;
using SdfMax;
using UnityEngine;

namespace Planetary
{
    public static class SdfMaxPlanetCompositionBuilder
    {
        public static SdfMaxCompositionAsset Build(PlanetBody body, PlanetaryPlanarBase planar, SdfMaxSolverProfile profile)
        {
            var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            asset.nodes = new List<SdfMaxNode>();
            float radius = body != null ? body.PlanetRadius : 1000f;

            int noiseIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.FractalNoise,
                noiseSeed = profile != null && profile.noiseDefaults != null ? profile.noiseDefaults.seed : 1,
                noiseFrequency = profile != null && profile.noiseDefaults != null ? profile.noiseDefaults.frequency : 0.01f,
                weight = 0.3f
            });

            int shellIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.DisplacedSphere,
                sphereRadius = radius,
                childIndexA = noiseIdx,
                smoothRadius = 5f
            });

            int rootIdx = shellIdx;
            if (planar?.featureStack != null)
            {
                for (int i = 0; i < planar.featureStack.features.Count; i++)
                {
                    var f = planar.featureStack.features[i];
                    if (f == null)
                        continue;
                    int stampIdx = AddNode(asset, new SdfMaxNode
                    {
                        op = SdfMaxOp.PrimitiveLeaf,
                        primitiveType = SdfPrimitiveType.PlanarStamp,
                        planarFeatureIndex = i,
                        stampFootprintMeters = f.footprintRadiusMeters,
                        weight = f.strength
                    });
                    int blendIdx = AddNode(asset, new SdfMaxNode
                    {
                        op = SdfMaxOp.SmoothMax,
                        childIndexA = rootIdx,
                        childIndexB = stampIdx,
                        smoothRadius = f.smoothRadius
                    });
                    rootIdx = blendIdx;
                }
            }

            int latLonIdx = AddNode(asset, new SdfMaxNode
            {
                op = SdfMaxOp.PrimitiveLeaf,
                primitiveType = SdfPrimitiveType.LatLonShell,
                sphereRadius = radius,
                childIndexA = rootIdx,
                smoothRadius = 2f,
                weight = 1f
            });

            asset.rootNodeIndex = latLonIdx;
            return asset;
        }

        static int AddNode(SdfMaxCompositionAsset asset, SdfMaxNode node)
        {
            int idx = asset.nodes.Count;
            asset.nodes.Add(node);
            return idx;
        }
    }
}
