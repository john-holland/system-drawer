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
            _streaming.RequestTilesAroundPlayer(body, 0, 1);
            _revealNadir = _streaming.GetCoverageFraction(sc.LatitudeDeg, sc.LongitudeDeg, body.chunksPerFace);
        }
    }
}
