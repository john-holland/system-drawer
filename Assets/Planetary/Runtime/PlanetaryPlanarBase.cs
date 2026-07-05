using System.Collections.Generic;
using Planetary.Sources;
using SdfMax;
using UnityEngine;

namespace Planetary
{
    [CreateAssetMenu(fileName = "PlanetaryPlanarBase", menuName = "Planetary/Planar Base")]
    public sealed class PlanetaryPlanarBase : ScriptableObject
    {
        public PlanetaryPlanarFeatureStack featureStack;
        public Texture2D authoredHeightmap;
        public NoiseLibrarySettings proceduralNoise = new NoiseLibrarySettings();
        public Texture2D googleMaskPreview;

        readonly List<IPlanetaryPlanarSource> _sources = new List<IPlanetaryPlanarSource>();

        public void RebuildSources(Vector3 planetCenter, Vector3 poleAxis, float primeMeridian, PlanetMeshStreamingService streaming)
        {
            _sources.Clear();
            _sources.Add(new ProceduralPlanarSource(proceduralNoise));
            if (featureStack != null)
                _sources.Add(new PlanarStampPlanarSource(featureStack, planetCenter, poleAxis, primeMeridian));
            if (authoredHeightmap != null)
                _sources.Add(new AuthoredHeightmapPlanarSource(authoredHeightmap));
            if (streaming != null)
                _sources.Add(new ContinuuuumTilePlanarSource(streaming));
            if (googleMaskPreview != null)
            {
                int res = Mathf.Min(googleMaskPreview.width, 256);
                var mask = new float[res, res];
                for (int y = 0; y < res; y++)
                for (int x = 0; x < res; x++)
                    mask[x, y] = TextureSamplingUtility.SampleAlphaBilinear(
                        googleMaskPreview, x / (float)(res - 1), y / (float)(res - 1));
                _sources.Add(new GoogleMapsShapesPlanarSource(mask, res));
            }
        }

        public PlanetDataSourceMask GetDataSourceMask()
        {
            PlanetDataSourceMask m = PlanetDataSourceMask.None;
            for (int i = 0; i < _sources.Count; i++)
                m |= _sources[i].Mask;
            return m;
        }

        public float SampleHeight(float latDeg, float lonDeg)
        {
            float h = 0f;
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                float s = _sources[i].SampleHeight(latDeg, lonDeg);
                if (s != 0f)
                    h = s;
            }
            return h;
        }

        public float SampleSlope(float latDeg, float lonDeg)
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                float s = _sources[i].SampleSlope(latDeg, lonDeg);
                if (s > 0f)
                    return s;
            }
            return 0f;
        }

        public int SampleBiome(float latDeg, float lonDeg)
        {
            for (int i = _sources.Count - 1; i >= 0; i--)
            {
                int b = _sources[i].SampleBiome(latDeg, lonDeg);
                if (b != 0)
                    return b;
            }
            return 0;
        }
    }
}
