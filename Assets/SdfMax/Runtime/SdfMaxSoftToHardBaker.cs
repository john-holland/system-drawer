using System.Collections.Generic;
using UnityEngine;

namespace SdfMax
{
    /// <summary>Composites soft PixelLight / height layers into a hard SdfMax graph.</summary>
    public static class SdfMaxSoftToHardBaker
    {
        public static SdfMaxCompositionAsset BakeDisplacedTorus(
            float majorRadius,
            float minorRadius,
            float[,] height,
            float heightWeight = 1f)
        {
            var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            asset.name = "SoftToHardTorus";
            asset.nodes = new List<SdfMaxNode>
            {
                new SdfMaxNode
                {
                    op = SdfMaxOp.PrimitiveLeaf,
                    primitiveType = SdfPrimitiveType.DisplacedTorus,
                    torusMajorRadius = majorRadius,
                    torusMinorRadius = minorRadius,
                    weight = heightWeight,
                    halfExtents = Vector3.one * (majorRadius + minorRadius)
                }
            };
            asset.rootNodeIndex = 0;
            if (height != null)
                asset.nodes[0].constantValue = SampleHeightMax(height);
            return asset;
        }

        public static SdfMaxCompositionAsset BakeBoxUnionWithOpenings(
            Vector3 halfExtents,
            IList<Vector3> openingCenters,
            float openingRadius)
        {
            var asset = ScriptableObject.CreateInstance<SdfMaxCompositionAsset>();
            asset.nodes = new List<SdfMaxNode>
            {
                new SdfMaxNode
                {
                    op = SdfMaxOp.PrimitiveLeaf,
                    primitiveType = SdfPrimitiveType.Box,
                    halfExtents = halfExtents
                }
            };
            int root = 0;
            if (openingCenters != null)
            {
                for (int i = 0; i < openingCenters.Count; i++)
                {
                    int sph = asset.nodes.Count;
                    asset.nodes.Add(new SdfMaxNode
                    {
                        op = SdfMaxOp.PrimitiveLeaf,
                        primitiveType = SdfPrimitiveType.Sphere,
                        localPosition = openingCenters[i],
                        radius = Mathf.Max(0.05f, openingRadius),
                        sphereRadius = Mathf.Max(0.05f, openingRadius)
                    });
                    int sub = asset.nodes.Count;
                    asset.nodes.Add(new SdfMaxNode
                    {
                        op = SdfMaxOp.Subtract,
                        childIndexA = root,
                        childIndexB = sph
                    });
                    root = sub;
                }
            }
            asset.rootNodeIndex = root;
            return asset;
        }

        public static float CompositeHeight(float a, float b, int composite)
        {
            switch (composite)
            {
                case 1:
                    return Mathf.Max(a, b > 0.5f ? 1f : 0f);
                case 2:
                    return Mathf.Clamp01(a + b);
                default:
                    return Mathf.Max(a, b);
            }
        }

        static float SampleHeightMax(float[,] height)
        {
            float m = 0f;
            int h = height.GetLength(0);
            int w = height.GetLength(1);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (height[y, x] > m) m = height[y, x];
            return m;
        }
    }
}
