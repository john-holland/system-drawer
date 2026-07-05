using UnityEngine;

namespace Planetary.Sources
{
    /// <summary>Height from continuuuum planet_tiles (filled by PlanetMeshStreamingService cache).</summary>
    public sealed class ContinuuuumTilePlanarSource : IPlanetaryPlanarSource
    {
        readonly PlanetMeshStreamingService _streaming;

        public ContinuuuumTilePlanarSource(PlanetMeshStreamingService streaming) => _streaming = streaming;

        public PlanetDataSourceMask Mask => PlanetDataSourceMask.Continuuuum;

        public float SampleHeight(float latDeg, float lonDeg) =>
            _streaming != null ? _streaming.SampleCachedHeight(latDeg, lonDeg) : 0f;

        public float SampleSlope(float latDeg, float lonDeg) => 0f;
        public int SampleBiome(float latDeg, float lonDeg) => 0;
    }
}
