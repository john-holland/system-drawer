using Planetary.Composition;
using UnityEngine;

namespace Planetary.Rendering
{
    public struct PlanetarySdfLodFrame
    {
        public float detailCoeff;
        public float horizonSdfWeight;
        public float revealNadir;
        public PlanetaryAltitudeBand band;
    }

    public sealed class PlanetarySdfLodController
    {
        readonly PlanetarySdfLodProfile _profile;
        readonly PlanetaryHorizonLodController _horizon;

        public PlanetarySdfLodController(PlanetarySdfLodProfile profile, PlanetaryHorizonLodSettings horizonSettings)
        {
            _profile = profile;
            _horizon = horizonSettings != null ? new PlanetaryHorizonLodController(horizonSettings) : null;
        }

        public PlanetarySdfLodFrame Compute(
            Vector3 cameraWorld,
            Vector3 planetCenter,
            float planetRadius,
            float localTerrainHeightM,
            float cloudBaseM,
            float cloudTopM,
            float revealNadir)
        {
            float altitudeMSL = PlanetaryHorizonLodController.ComputeAltitudeMsl(
                cameraWorld, planetCenter, planetRadius, localTerrainHeightM);
            float radialM = Mathf.Max(0f, Vector3.Distance(cameraWorld, planetCenter) - planetRadius);
            float surfaceDistKm = radialM * 0.001f;

            PlanetaryAltitudeBand band = _horizon != null
                ? _horizon.SelectBand(altitudeMSL, cloudBaseM, cloudTopM)
                : PlanetaryAltitudeBand.Surface;

            float detailCoeff = _profile != null
                ? Mathf.InverseLerp(_profile.farFullSdfKm, _profile.nearFullSdfKm, surfaceDistKm)
                : 0.5f;
            if (band == PlanetaryAltitudeBand.Space)
                detailCoeff = 0f;

            float altGate = _profile != null
                ? Mathf.InverseLerp(_profile.sdfHorizonMinAltM, _profile.sdfHorizonFullAltM, altitudeMSL)
                : 0f;
            float bandScale = band switch
            {
                PlanetaryAltitudeBand.Surface => _profile != null ? _profile.surfaceHorizonSdfScale : 0.2f,
                PlanetaryAltitudeBand.Troposphere => 0.6f,
                _ => 1f
            };
            float horizonSdfWeight = Mathf.Clamp01(altGate * detailCoeff * bandScale);
            if (band == PlanetaryAltitudeBand.Space)
                horizonSdfWeight = 1f;

            return new PlanetarySdfLodFrame
            {
                detailCoeff = detailCoeff,
                horizonSdfWeight = horizonSdfWeight,
                revealNadir = revealNadir,
                band = band
            };
        }
    }
}
