using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Planetary.Celestial
{
    public static class GalacticNightSkyBakeSession
    {
        public static bool IsActive { get; private set; }

        public static void Begin() => IsActive = true;
        public static void End() => IsActive = false;

        public static GalacticNightSkyCacheRecord BakeEquirect(
            Vector3 observerWorld,
            string observerBodyId,
            float anchorLat,
            float anchorLon,
            float anchorAltM,
            IEnumerable<GalacticBodyRecord> catalog,
            Transform galacticOrigin,
            int width = 512,
            int height = 256,
            string outputFolder = "GalacticNightSkyCaches")
        {
            Begin();
            try
            {
                var stars = GalacticNightSkyStarCatalog.BuildVisibleStars(observerWorld, catalog, galacticOrigin);
                var tex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
                var pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    float v = y / (float)(height - 1);
                    float lat = Mathf.Lerp(-90f, 90f, v);
                    for (int x = 0; x < width; x++)
                    {
                        float u = x / (float)(width - 1);
                        float lon = Mathf.Lerp(-180f, 180f, u);
                        Vector3 dir = LatLonToDirection(lat, lon);
                        Color c = SampleStars(dir, stars);
                        pixels[y * width + x] = c;
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();

                string outputDir = Path.Combine(Application.dataPath, outputFolder);
                Directory.CreateDirectory(outputDir);
                string fileName = $"{observerBodyId}_{anchorLat:F1}_{anchorLon:F1}.png";
                string path = Path.Combine(outputDir, fileName);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(tex);

                return new GalacticNightSkyCacheRecord
                {
                    cacheId = Guid.NewGuid().ToString("N"),
                    observerBodyId = observerBodyId,
                    anchorLat = anchorLat,
                    anchorLon = anchorLon,
                    anchorAltM = anchorAltM,
                    localPath = $"Assets/{outputFolder}/{fileName}",
                    starCount = stars.Count
                };
            }
            finally
            {
                End();
            }
        }

        static Vector3 LatLonToDirection(float latDeg, float lonDeg)
        {
            float lat = latDeg * Mathf.Deg2Rad;
            float lon = lonDeg * Mathf.Deg2Rad;
            float cl = Mathf.Cos(lat);
            return new Vector3(Mathf.Sin(lon) * cl, Mathf.Sin(lat), Mathf.Cos(lon) * cl);
        }

        static Color SampleStars(Vector3 dir, List<GalacticNightSkyStarCatalog.StarPoint> stars)
        {
            Color c = Color.black;
            for (int i = 0; i < stars.Count; i++)
            {
                float d = Vector3.Dot(dir, stars[i].direction);
                if (d > 0.9995f)
                {
                    float bright = 1f / Mathf.Max(0.5f, stars[i].magnitude);
                    c += stars[i].color * bright;
                }
            }
            return c;
        }
    }
}
