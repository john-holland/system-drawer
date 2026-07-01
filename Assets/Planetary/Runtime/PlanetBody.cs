using Planetary.Bridges;
using Planetary.Composition;
using Planetary.Elemental;
using Planetary.Rendering;
using Planetary.Tectonics;
using SdfMax;
using SpatialVolumes;
using UnityEngine;
using Weather;

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
        public HorizonLodSettings horizonLodSettings;
        public ElementalRule[] elementalRules = System.Array.Empty<ElementalRule>();
        public PlanetFeatureFlags featureFlags = PlanetFeatureFlags.StablePoles | PlanetFeatureFlags.MagneticPoles;
        public float planetRadius = 1000f;
        public Vector3 stablePoleAxis = Vector3.up;
        public Vector3 magneticPoleAxis = Vector3.up;
        public float primeMeridianOffsetDeg;

        [Header("Society sim")]
        [Tooltip("Matches society_planets.planet_id in continuum API")]
        public string societyPlanetId = "earth";

        public int meshResolution = 32;
        public int chunksPerFace = 2;

        public PlanetRenderer planetRenderer;
        public PlanetMeshStreamingService streamingService;
        public SpatialVolumeProvider volumeProvider;
        public PlanetarySdfLodRenderer sdfLodRenderer;
        public PlanetInteriorPhysicsUpdater interiorUpdater;
        public PlanetarySimulationScheduler simulationScheduler;

        [Header("Physics bridge gizmos")]
        public bool drawPhysicsBridgeGizmos = true;
        public PlanetPhysicsManifoldBridge physicsManifoldBridge;

        [Header("Play mode")]
        [Tooltip("When off, Enter Play uses the assigned composition and skips plate regression + SDF rebake (use Rebuild Planet in editor first).")]
        public bool rebuildOnPlayAwake;

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
            EnsureComponentRefs();
            if (Application.isPlaying && !rebuildOnPlayAwake && HasPlayableComposition())
            {
                SyncPlayModeFromBakedComposition();
                return;
            }

            RebuildAll();
        }

        void EnsureComponentRefs()
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
            if (physicsManifoldBridge == null)
                physicsManifoldBridge = GetComponentInChildren<PlanetPhysicsManifoldBridge>();
        }

        bool HasPlayableComposition() =>
            composition != null && composition.nodes != null && composition.nodes.Count > 0;

        void SyncPlayModeFromBakedComposition()
        {
            _planarContext = new PlanetPlanarEvaluationContext(this, planarBase);
            ApplyCompositionToVolumeProvider(false);
            if (sdfLodRenderer != null)
                sdfLodRenderer.EnsureLodMeshes();
        }

        [ContextMenu("Rebuild Planet")]
        public void RebuildAll()
        {
            using (PerfTrace.Scope("RebuildAll"))
            {
                if (planarBase != null)
                    planarBase.RebuildSources(PlanetCenter, stablePoleAxis, primeMeridianOffsetDeg, streamingService);

                RebakeComposition();
                RebuildChunkMeshes();
                if (sdfLodRenderer != null)
                    sdfLodRenderer.Rebake();
            }
        }

        public void RebakeComposition()
        {
            using (PerfTrace.Scope("RebakeComposition"))
            {
                RebakeCompositionCore();
            }
        }

        void RebakeCompositionCore()
        {
            _regression.Engine.SetRules(elementalRules);
            var plates = _regression.RegressPlatesFromSurface(this, 8, elementalRules);
            PlateTectonicsPhysicsSolver.ResetPlateStress(plates);
            if (interiorUpdater != null && interiorUpdater.plateSolver != null)
            {
                interiorUpdater.plateSolver.ClearPlates();
                interiorUpdater.plateSolver.plates = plates;
            }
            RebakeCompositionCoreWithPlates(plates);
        }

        public void ApplyCompositionToVolumeProvider() => ApplyCompositionToVolumeProvider(true);

        public void ApplyCompositionToVolumeProvider(bool forceVolumeRebuild)
        {
            if (volumeProvider == null)
                return;
            volumeProvider.composition = composition;
            volumeProvider.profile = solverProfile;
            volumeProvider.RebuildIfDirty(forceVolumeRebuild);
        }

        /// <summary>Clears solver plates, regresses from surface, rebakes composition. Used by Rebuild Now on volume provider.</summary>
        public void RebuildTectonicPlates(bool stepPhysics = false)
        {
            if (interiorUpdater != null)
            {
                interiorUpdater.RebuildTectonicPlates(clearExisting: true, stepPhysics: stepPhysics);
                interiorUpdater.RebakeFromPlates(interiorUpdater.plateSolver != null
                    ? interiorUpdater.plateSolver.plates
                    : System.Array.Empty<PlateDefinition>());
                return;
            }

            if (interiorUpdater == null)
                interiorUpdater = GetComponent<PlanetInteriorPhysicsUpdater>();
            _regression.Engine.SetRules(elementalRules);
            var plates = _regression.RegressPlatesFromSurface(this, 8, elementalRules);
            PlateTectonicsPhysicsSolver.ResetPlateStress(plates);
            if (interiorUpdater != null && interiorUpdater.plateSolver != null)
            {
                interiorUpdater.plateSolver.ClearPlates();
                interiorUpdater.plateSolver.plates = plates;
                if (stepPhysics)
                    interiorUpdater.plateSolver.Step(1f, transform.position);
            }

            RebakeCompositionCoreWithPlates(plates);
        }

        void RebakeCompositionCoreWithPlates(PlateDefinition[] plates)
        {
            _regression.Engine.SetRules(elementalRules);
            var estimator = new AtmosphereCompositionEstimator();
            WeatherPhysicsManifold weatherManifold = null;
            SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
            var atmos = estimator.Estimate(this, weatherManifold);
            if (compositionProfile != null)
                composition = PlanetaryCompositionBaker.Bake(this, planarBase, solverProfile, compositionProfile, atmos, plates);
            else
                composition = SdfMaxPlanetCompositionBuilder.Build(this, planarBase, solverProfile);

            _planarContext = new PlanetPlanarEvaluationContext(this, planarBase);
            _compositionVersion++;
            ApplyCompositionToVolumeProvider();
            if (physicsManifoldBridge != null)
            {
                physicsManifoldBridge.planet = this;
                physicsManifoldBridge.StampFromCompositionBake();
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
                (lat, lon) => planarBase.SampleHeight(lat, lon),
                Vector3.zero,
                stablePoleAxis);
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
