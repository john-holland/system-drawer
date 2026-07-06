using UnityEngine;

namespace Planetary.Rendering
{
    public sealed class PlanetaryLodHandoffController
    {
        readonly PlanetMeshStreamingService _streaming;
        float _revealNadir;

        public float RevealNadir => _revealNadir;

        public PlanetaryLodHandoffController(PlanetMeshStreamingService streaming) => _streaming = streaming;

        public void Tick(PlanetBody body, Camera cam)
        {
            if (_streaming == null || body == null || cam == null)
                return;
            var sc = SphericalCoordinates.FromWorldPosition(
                cam.transform.position, body.PlanetCenter, body.StablePoleAxis, body.PrimeMeridianOffsetDeg);
            int lod = 0;
            int radius = 1;
            if (FeatureBudget.IsAvailable && FeatureBudget.IsFeatureActive(FeatureBudgetIds.PlanetStreaming))
            {
                float g = FeatureBudget.GetGranularity(FeatureBudgetIds.PlanetStreaming);
                lod = FeatureBudgetGranularityBridge.MapGranularityToLodTierOffset(1f - g);
                radius = Mathf.Max(1, Mathf.RoundToInt(g * 2f));
            }
            else if (FeatureBudget.IsAvailable && !FeatureBudget.IsFeatureActive(FeatureBudgetIds.PlanetStreaming))
            {
                return;
            }

            _streaming.RequestTilesAroundPlayer(body, lod, radius);
            _revealNadir = _streaming.GetCoverageFraction(sc.LatitudeDeg, sc.LongitudeDeg, body.chunksPerFace);
        }
    }
}
