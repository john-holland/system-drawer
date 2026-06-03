using Planetary.Composition;
using Planetary.Elemental;
using Planetary.Rendering;
using Planetary.Tectonics;
using SdfMax;
using SpatialVolumes;
using UnityEngine;

namespace Planetary
{
    /// <summary>
    /// Planet host: planar sampling, SDF composition, chunked mesh render, LOD overlay.
    /// </summary>
    public sealed class PlanetBody : MonoBehaviour, global::Weather.IExternalHeightProvider
    {
        public PlanetaryPlanarBase planarBase;
        public SdfMaxSolverProfile solverProfile;
        public SdfMaxCompositionAsset composition;
        public PlanetaryCompositionProfile compositionProfile;
        public PlanetarySdfLodProfile sdfLodProfile;
        public PlanetaryHorizonLodSettings horizonLodSettings;
        public ElementalRule[] elementalRules = System.Array.Empty<ElementalRule>();
        public PlanetFeatureFlags featureFlags = PlanetFeatureFlags.StablePoles | PlanetFeatureFlags.MagneticPoles;
        public float planetRadius = 1000f;
        public Vector3 stablePoleAxis = Vector3.up;
        public Vector3 magneticPoleAxis = Vector3.up;
        public float primeMeridianOffsetDeg;
        public int meshResolution = 32;
        public int chunksPerFace = 2;

        public PlanetRenderer planetRenderer;
        public PlanetMeshStreamingService streamingService;
        public SpatialVolumeProvider volumeProvider;
        public PlanetarySdfLodRenderer sdfLodRenderer;
        public PlanetInteriorPhysicsUpdater interiorUpdater;
        public PlanetarySimulationScheduler simulationScheduler;

        PlanetPlanarEvaluationContext _planarContext;
        readonly MaterialRegressionService _regression = new MaterialRegressionService();
        int _compositionVersion;

        public Vector3 PlanetCenter => transform.position;
        public float PlanetRadius => planetRadius;
        public Vector3 StablePoleAxis => stablePoleAxis;
        public float PrimeMeridianOffsetDeg => primeMeridianOffsetDeg;
        public int CompositionVersion => _compositionVersion;

        void Awake()
        {
            if (planetRenderer == null)
                planetRenderer = GetComponentInChildren<PlanetRenderer>();
            if (streamingService == null)
                streamingService = GetComponent<PlanetMeshStreamingService>();
            if (volumeProvider == null)
                volumeProvider = GetComponent<SpatialVolumeProvider>();
            if (sdfLodRenderer == null)
                sdfLodRenderer = GetComponentInChildren<PlanetarySdfLodRenderer>();
            if (interiorUpdater == null)
                interiorUpdater = GetComponent<PlanetInteriorPhysicsUpdater>();
            RebuildAll();
        }

        [ContextMenu("Rebuild Planet")]
        public void RebuildAll()
        {
            if (planarBase != null)
                planarBase.RebuildSources(PlanetCenter, stablePoleAxis, primeMeridianOffsetDeg, streamingService);

            RebakeComposition();
            RebuildChunkMeshes();
            if (sdfLodRenderer != null)
                sdfLodRenderer.Rebake();
        }

        public void RebakeComposition()
        {
            _regression.Engine.SetRules(elementalRules);
            var plates = _regression.RegressPlatesFromSurface(this, 8, elementalRules);
            var estimator = new AtmosphereCompositionEstimator();
            var atmos = estimator.Estimate(this, FindFirstObjectByType<global::Weather.WeatherPhysicsManifold>());
            if (compositionProfile != null)
                composition = PlanetaryCompositionBaker.Bake(this, planarBase, solverProfile, compositionProfile, atmos, plates);
            else
                composition = SdfMaxPlanetCompositionBuilder.Build(this, planarBase, solverProfile);

            _planarContext = new PlanetPlanarEvaluationContext(this, planarBase);
            _compositionVersion++;

            if (volumeProvider != null)
            {
                volumeProvider.composition = composition;
                volumeProvider.profile = solverProfile;
                volumeProvider.RebuildIfDirty(true);
            }
        }

        void RebuildChunkMeshes()
        {
            if (planetRenderer == null || planarBase == null)
                return;
            var chunks = PlanetMeshBuilder.BuildChunks(
                planetRadius,
                meshResolution,
                chunksPerFace,
                (lat, lon) => planarBase.SampleHeight(lat, lon));
            planetRenderer.SetChunks(chunks, transform);
        }

        public SdfMaxExpressionGraph CreateExpressionGraph()
        {
            return new SdfMaxExpressionGraph(
                composition,
                solverProfile,
                transform.localToWorldMatrix,
                _planarContext);
        }

        public bool TrySampleHeightAtWorld(Vector3 worldPos, out float heightMeters, out float slopeDeg)
        {
            heightMeters = 0f;
            slopeDeg = 0f;
            if (planarBase == null)
                return false;
            var sc = SphericalCoordinates.FromWorldPosition(worldPos, PlanetCenter, stablePoleAxis, primeMeridianOffsetDeg);
            heightMeters = planarBase.SampleHeight(sc.LatitudeDeg, sc.LongitudeDeg);
            slopeDeg = planarBase.SampleSlope(sc.LatitudeDeg, sc.LongitudeDeg);
            return true;
        }
    }
}
