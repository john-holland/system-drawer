using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roads
{
    public enum RoadBakeMode
    {
        RibbonMesh,
        SdfCorridor
    }

    [Serializable]
    public class RoadHeightStamp
    {
        public int resolution = 64;
        public Bounds worldBounds;
        public float[] heights;
        public float minHeight;
        public float maxHeight;

        public Texture2D ToTexture()
        {
            if (heights == null || heights.Length == 0)
                return null;
            int res = Mathf.Max(4, resolution);
            var tex = new Texture2D(res, res, TextureFormat.RFloat, false);
            var pixels = new Color[res * res];
            float range = Mathf.Max(1e-4f, maxHeight - minHeight);
            for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                int idx = Mathf.Min(y * res + x, heights.Length - 1);
                float n = (heights[idx] - minHeight) / range;
                pixels[y * res + x] = new Color(n, n, n, 1f);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }

    [Serializable]
    public class RoadSdfCorridorSample
    {
        public Vector3 position;
        public float halfWidth;
        public float halfHeight;
        public float distanceAlong;
    }

    [Serializable]
    public class RoadMeshBakeData
    {
        public Mesh bakedMesh;
        public RoadHeightStamp heightStamp = new RoadHeightStamp();
        public List<RoadSdfCorridorSample> sdfSamples = new List<RoadSdfCorridorSample>();
        public Bounds worldBounds;
        public string roadSegmentId;
        public int buildVersion;
    }
}
