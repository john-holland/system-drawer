using UnityEngine;

namespace Planetary.Composition
{
    public enum LodTier
    {
        FullSim,
        MidPrebake,
        FarImpostor,
        SpaceImpostor
    }

    public sealed class PlanetaryHorizonLodController
    {
        readonly HorizonLodSettings _settings;

        public PlanetaryHorizonLodController(HorizonLodSettings settings) => _settings = settings;

        public PlanetaryAltitudeBand SelectBand(float altitudeMSL, float cloudBaseM, float cloudTopM)
        {
            if (altitudeMSL > (_settings != null ? _settings.upperAtmosphereMaxM : 80000f))
                return PlanetaryAltitudeBand.Space;
            if (altitudeMSL > (_settings != null ? _settings.troposphereMaxM : 12000f))
                return PlanetaryAltitudeBand.UpperAtmosphere;
            if (altitudeMSL >= cloudBaseM && altitudeMSL <= cloudTopM)
                return PlanetaryAltitudeBand.CloudLayer;
            if (altitudeMSL > (_settings != null ? _settings.surfaceBandMaxM : 2000f))
                return PlanetaryAltitudeBand.Troposphere;
            return PlanetaryAltitudeBand.Surface;
        }

        public LodTier SelectLod(float surfaceDistanceKm, float altitudeMSL, float cloudBaseM, float cloudTopM)
        {
            float fullSimKm = _settings != null ? _settings.fullSimRadiusKm : 50f;
            float horizonKm = _settings != null ? _settings.horizonDistanceKm : 500f;
            return SelectLod(surfaceDistanceKm, altitudeMSL, cloudBaseM, cloudTopM, fullSimKm, horizonKm);
        }

        public LodTier SelectLod(
            float surfaceDistanceKm,
            float altitudeMSL,
            float cloudBaseM,
            float cloudTopM,
            float fullSimRadiusKm,
            float horizonDistanceKm)
        {
            var band = SelectBand(altitudeMSL, cloudBaseM, cloudTopM);
            if (band == PlanetaryAltitudeBand.Space)
                return LodTier.SpaceImpostor;
            if (surfaceDistanceKm > horizonDistanceKm)
                return LodTier.FarImpostor;
            if (surfaceDistanceKm > fullSimRadiusKm
                || altitudeMSL > (_settings != null ? _settings.fullSimAltitudeMaxM : 12000f))
                return LodTier.MidPrebake;
            return LodTier.FullSim;
        }

        public static float ComputeAltitudeMsl(Vector3 cameraWorld, Vector3 planetCenter, float planetRadius, float localTerrainHeightM)
        {
            float radial = Vector3.Distance(cameraWorld, planetCenter);
            return Mathf.Max(0f, radial - planetRadius - localTerrainHeightM);
        }

        public static float TangentialDistKm(Vector3 groundTrack, Vector3 targetOnSphere, float planetRadius)
        {
            Vector3 a = groundTrack.normalized;
            Vector3 b = targetOnSphere.normalized;
            float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(a, b), -1f, 1f));
            return angle * planetRadius * 0.001f;
        }
    }
}
