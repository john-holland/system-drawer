using System.Collections.Generic;
using Planetary.Bridges;
using Planetary.Composition;
using UnityEngine;
using Weather;

namespace Planetary.Field
{
    /// <summary>
    /// Single write-surface reader: weather manifold + chart pullbacks blended by transition weights.
    /// </summary>
    [AddComponentMenu("Planetary/Canonical Spatiotemporal Field")]
    public sealed class CanonicalSpatiotemporalField : MonoBehaviour, ICanonicalSpatiotemporalField
    {
        public static CanonicalSpatiotemporalField Instance { get; private set; }

        public WeatherPhysicsManifold manifold;
        public PlanetBody planet;
        public PlanetShellManifoldGrid shellGrid;
        public PhysicalManifoldRelativitySolver relativitySolver;
        public HorizonLodSettings horizonLodSettings;
        public Transform focusTransform;

        [Tooltip("Optional narrative volumes for NarrativeTimeSlice chart gating.")]
        public List<MonoBehaviour> narrativeVolumeBehaviours = new List<MonoBehaviour>();

        [Tooltip("Attenuation outside narrative volumes for NarrativeTimeSlice chart.")]
        [Range(0f, 1f)] public float outsideNarrativeAttenuation = 0.25f;

        void Awake()
        {
            if (manifold == null)
                manifold = FindAnyObjectByType<WeatherPhysicsManifold>();
            if (planet == null)
                planet = FindAnyObjectByType<PlanetBody>();
            if (shellGrid == null && planet != null)
                shellGrid = planet.GetComponentInChildren<PlanetShellManifoldGrid>();
            if (relativitySolver == null)
                relativitySolver = FindAnyObjectByType<PhysicalManifoldRelativitySolver>();
        }

        void OnEnable()
        {
            Instance = this;
            TryRegisterServiceKey();
        }

        void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        public bool TrySample(Vector3 world, float narrativeTime, SpatiotemporalChart requestedChart, out SpatiotemporalSample sample)
        {
            sample = default;
            if (manifold == null)
                return false;

            switch (requestedChart)
            {
                case SpatiotemporalChart.PlanetShell:
                    return TrySamplePlanetShell(world, out sample);
                case SpatiotemporalChart.SurfaceTangent:
                    return TrySampleSurfaceTangent(world, out sample);
                case SpatiotemporalChart.SpaceTimeMetric:
                    return TrySampleSpaceTimeMetric(world, out sample);
                case SpatiotemporalChart.NarrativeTimeSlice:
                    return TrySampleNarrativeSlice(world, narrativeTime, out sample);
                default:
                    return TrySampleWorld(world, out sample);
            }
        }

        public bool TrySampleBlended(Vector3 world, float narrativeTime, out SpatiotemporalSample sample)
        {
            sample = default;
            if (manifold == null)
                return false;

            ComputeTransitionContext(world, out float altMsl, out float distKm);
            TransitionWeightSet w = TransitionWeightSet.Compute(altMsl, distKm, 0f, 2000f, horizonLodSettings);

            SpatiotemporalSample acc = default;
            float weightSum = 0f;
            BlendChart(SpatiotemporalChart.World, w.world, world, narrativeTime, ref acc, ref weightSum);
            BlendChart(SpatiotemporalChart.PlanetShell, w.planetShell, world, narrativeTime, ref acc, ref weightSum);
            BlendChart(SpatiotemporalChart.SurfaceTangent, w.surfaceTangent, world, narrativeTime, ref acc, ref weightSum);
            BlendChart(SpatiotemporalChart.SpaceTimeMetric, w.spaceTimeMetric, world, narrativeTime, ref acc, ref weightSum);
            BlendChart(SpatiotemporalChart.NarrativeTimeSlice, w.narrativeTimeSlice, world, narrativeTime, ref acc, ref weightSum);

            if (weightSum <= 1e-6f)
                return TrySampleWorld(world, out sample);

            acc.velocityWorld /= weightSum;
            acc.surfaceFriction /= weightSum;
            acc.surfaceTensionCoeff /= weightSum;
            acc.transitionWeight = 1f;
            acc.dominantChart = w.DominantChart();
            sample = acc;
            return true;
        }

        void BlendChart(
            SpatiotemporalChart chart,
            float weight,
            Vector3 world,
            float narrativeTime,
            ref SpatiotemporalSample acc,
            ref float weightSum)
        {
            if (weight <= 1e-6f)
                return;
            if (!TrySample(world, narrativeTime, chart, out SpatiotemporalSample s))
                return;
            acc.velocityWorld += s.velocityWorld * weight;
            acc.surfaceFriction += s.surfaceFriction * weight;
            acc.surfaceTensionCoeff += s.surfaceTensionCoeff * weight;
            acc.cell = s.cell;
            weightSum += weight;
        }

        bool TrySampleWorld(Vector3 world, out SpatiotemporalSample sample)
        {
            ManifoldCellData cell = manifold.GetDataAtPosition(world);
            sample = SpatiotemporalSample.FromCell(cell, cell.velocity, SpatiotemporalChart.World);
            return true;
        }

        bool TrySamplePlanetShell(Vector3 world, out SpatiotemporalSample sample)
        {
            if (shellGrid != null && shellGrid.TryGetShellSample(world, out Vector3 shellCenter, out _))
                world = shellCenter;
            ManifoldCellData cell = manifold.GetDataAtPosition(world);
            sample = SpatiotemporalSample.FromCell(cell, cell.velocity, SpatiotemporalChart.PlanetShell);
            return true;
        }

        bool TrySampleSurfaceTangent(Vector3 world, out SpatiotemporalSample sample)
        {
            if (planet != null && planet.TrySampleHeightAtWorld(world, out float height, out _))
            {
                Vector3 up = (world - planet.PlanetCenter).normalized;
                world = planet.PlanetCenter + up * (planet.PlanetRadius + height);
            }

            ManifoldCellData cell = manifold.GetDataAtPosition(world);
            Vector3 vel = cell.velocity;
            if (planet != null)
            {
                Vector3 up = (world - planet.PlanetCenter).normalized;
                vel = Vector3.ProjectOnPlane(vel, up);
            }

            sample = SpatiotemporalSample.FromCell(cell, vel, SpatiotemporalChart.SurfaceTangent);
            return true;
        }

        bool TrySampleSpaceTimeMetric(Vector3 world, out SpatiotemporalSample sample)
        {
            ManifoldCellData cell = manifold.GetDataAtPosition(world);
            Vector3 vel = cell.velocity;
            if (relativitySolver != null)
            {
                float factor = relativitySolver.SampleMetricFactor(world, vel.normalized);
                vel /= Mathf.Max(factor, 0.01f);
            }

            sample = SpatiotemporalSample.FromCell(cell, vel, SpatiotemporalChart.SpaceTimeMetric);
            return true;
        }

        bool TrySampleNarrativeSlice(Vector3 world, float narrativeTime, out SpatiotemporalSample sample)
        {
            float gate = ComputeNarrativeGate(world, narrativeTime);
            ManifoldCellData cell = manifold.GetDataAtPosition(world);
            Vector3 vel = cell.velocity * gate;
            sample = SpatiotemporalSample.FromCell(cell, vel, SpatiotemporalChart.NarrativeTimeSlice, gate);
            sample.surfaceFriction *= gate;
            return true;
        }

        float ComputeNarrativeGate(Vector3 world, float narrativeTime)
        {
            if (narrativeVolumeBehaviours == null || narrativeVolumeBehaviours.Count == 0)
                return 1f;

            bool inside = false;
            for (int i = 0; i < narrativeVolumeBehaviours.Count; i++)
            {
                if (narrativeVolumeBehaviours[i] is ISpatiotemporalVolume vol && vol.Contains(world, narrativeTime))
                {
                    inside = true;
                    break;
                }
            }

            return inside ? 1f : outsideNarrativeAttenuation;
        }

        void ComputeTransitionContext(Vector3 world, out float altitudeMsl, out float surfaceDistanceKm)
        {
            altitudeMsl = world.y;
            surfaceDistanceKm = 0f;
            if (planet == null)
                return;

            Transform focus = focusTransform != null ? focusTransform : Camera.main != null ? Camera.main.transform : null;
            Vector3 planetCenter = planet.PlanetCenter;
            float terrainH = 0f;
            planet.TrySampleHeightAtWorld(world, out terrainH, out _);
            altitudeMsl = PlanetaryHorizonLodController.ComputeAltitudeMsl(world, planetCenter, planet.PlanetRadius, terrainH);

            if (focus != null)
            {
                Vector3 ground = focus.position;
                if (planet.TrySampleHeightAtWorld(focus.position, out float fh, out _))
                {
                    Vector3 dir = (focus.position - planetCenter).normalized;
                    ground = planetCenter + dir * (planet.PlanetRadius + fh);
                }

                surfaceDistanceKm = PlanetaryHorizonLodController.TangentialDistKm(
                    ground - planetCenter, world - planetCenter, planet.PlanetRadius);
            }
        }

        static void TryRegisterServiceKey()
        {
            if (Instance == null)
                return;
            System.Type svcType = System.Type.GetType("SystemDrawerService, SystemDrawer");
            if (svcType == null)
                return;
            var svc = FindFirstObjectByType(svcType);
            if (svc == null)
                return;
            var method = svcType.GetMethod("Register", new[] { typeof(string), typeof(Object) });
            method?.Invoke(svc, new object[] { CanonicalFieldServiceKeys.CanonicalField, Instance });
        }

        public static CanonicalSpatiotemporalField Resolve()
        {
            if (Instance != null)
                return Instance;
            System.Type svcType = System.Type.GetType("SystemDrawerService, SystemDrawer");
            if (svcType != null)
            {
                var svc = FindFirstObjectByType(svcType);
                if (svc != null)
                {
                    var get = svcType.GetMethod("Get");
                    if (get != null)
                    {
                        var generic = get.MakeGenericMethod(typeof(CanonicalSpatiotemporalField));
                        return generic.Invoke(svc, new object[] { CanonicalFieldServiceKeys.CanonicalField }) as CanonicalSpatiotemporalField;
                    }
                }
            }

            return FindAnyObjectByType<CanonicalSpatiotemporalField>();
        }
    }
}
