using System;
using Planetary.Pathing;
using UnityEngine;
using Weather;

namespace Planetary.Bridges
{
    public readonly struct ShellCellId : IEquatable<ShellCellId>
    {
        public readonly int LatBin;
        public readonly int LonBin;
        public readonly int AltitudeBand;
        public readonly bool IsPoleCap;

        public ShellCellId(int latBin, int lonBin, int altitudeBand, bool isPoleCap)
        {
            LatBin = latBin;
            LonBin = lonBin;
            AltitudeBand = altitudeBand;
            IsPoleCap = isPoleCap;
        }

        public bool Equals(ShellCellId other) =>
            LatBin == other.LatBin &&
            LonBin == other.LonBin &&
            AltitudeBand == other.AltitudeBand &&
            IsPoleCap == other.IsPoleCap;

        public override bool Equals(object obj) => obj is ShellCellId other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(LatBin, LonBin, AltitudeBand, IsPoleCap);
    }

    /// <summary>
    /// Planet-centric lat/lon/altitude shell grid with pole caps and longitude wrap.
    /// </summary>
    [AddComponentMenu("Planetary/Planet Shell Manifold Grid")]
    public sealed class PlanetShellManifoldGrid : MonoBehaviour
    {
        public PlanetBody planet;
        public WeatherPhysicsManifold weatherManifold;

        [Min(2)] public int latCount = 18;
        [Min(1)] public int lonCount = 36;
        [Min(1)] public int altitudeBandCount = 4;
        [Tooltip("Outer shell radius = planetRadius * multiplier for top altitude band.")]
        public float shellOuterRadiusMultiplier = 1.2f;

        const float PoleCapLatDeg = 89f;

        /// <summary>Total shell cells: two pole caps × bands + mid-lat grid × lon × bands.</summary>
        public int TotalCellCount =>
            2 * altitudeBandCount + Mathf.Max(0, latCount - 2) * lonCount * altitudeBandCount;

        /// <summary>Visit every discrete shell cell (pole caps + lat/lon/altitude bins).</summary>
        public void EnumerateAllCells(System.Action<ShellCellId> visit)
        {
            if (visit == null || latCount < 2)
                return;

            for (int b = 0; b < altitudeBandCount; b++)
            {
                visit(new ShellCellId(0, 0, b, isPoleCap: true));
                visit(new ShellCellId(latCount - 1, 0, b, isPoleCap: true));

                for (int li = 1; li < latCount - 1; li++)
                for (int lj = 0; lj < lonCount; lj++)
                    visit(new ShellCellId(li, lj, b, isPoleCap: false));
            }
        }

        void Awake()
        {
            if (planet == null)
                planet = GetComponentInParent<PlanetBody>();
        }

        void OnEnable()
        {
            RegisterWithService();
            RegisterPathingSolver();
        }

        void OnDisable()
        {
            UnregisterFromService();
        }

        void RegisterPathingSolver()
        {
            if (planet == null)
                return;
            PhysicalPathingSolverRegistry.Register(
                PhysicalPathingMedium.Space,
                new PlanetShellPathingSolver { Planet = planet, ShellGrid = this });
        }

        public void RegisterWithService()
        {
            var svcType = Type.GetType("SystemDrawerService, SystemDrawer");
            if (svcType == null)
                return;
            var instProp = svcType.GetProperty("Instance");
            var svc = instProp?.GetValue(null);
            if (svc == null)
                return;
            var register = svcType.GetMethod("Register");
            register?.Invoke(svc, new object[] { "planet.shellGrid", this });
        }

        void UnregisterFromService()
        {
            var svcType = Type.GetType("SystemDrawerService, SystemDrawer");
            if (svcType == null)
                return;
            var instProp = svcType.GetProperty("Instance");
            var svc = instProp?.GetValue(null);
            if (svc == null)
                return;
            var unregister = svcType.GetMethod("Unregister");
            unregister?.Invoke(svc, new object[] { "planet.shellGrid" });
        }

        public bool TryWorldToCell(Vector3 world, out ShellCellId id)
        {
            id = default;
            if (planet == null)
                return false;

            SphericalCoordinates sc = SphericalCoordinates.FromWorldPosition(
                world,
                planet.PlanetCenter,
                planet.StablePoleAxis,
                planet.PrimeMeridianOffsetDeg);

            float absLat = Mathf.Abs(sc.LatitudeDeg);
            if (absLat >= PoleCapLatDeg)
            {
                int capLat = sc.LatitudeDeg >= 0f ? latCount - 1 : 0;
                int band = ResolveAltitudeBand(sc.Radius);
                id = new ShellCellId(capLat, 0, band, isPoleCap: true);
                return true;
            }

            float latNorm = (sc.LatitudeDeg + 90f) / 180f;
            int latBin = Mathf.Clamp(Mathf.FloorToInt(latNorm * latCount), 1, latCount - 2);
            int lonBin = WrapLonBin(Mathf.FloorToInt((sc.LongitudeDeg + 180f) / 360f * lonCount));
            int altBand = ResolveAltitudeBand(sc.Radius);
            id = new ShellCellId(latBin, lonBin, altBand, isPoleCap: false);
            return true;
        }

        public Vector3 CellCenterWorld(ShellCellId id)
        {
            if (planet == null)
                return Vector3.zero;

            float radius = BandCenterRadius(id.AltitudeBand);
            if (id.IsPoleCap)
            {
                float latDeg = id.LatBin == 0 ? -90f : 90f;
                return new SphericalCoordinates(latDeg, 0f, radius)
                    .ToWorldPosition(planet.PlanetCenter, planet.StablePoleAxis, planet.PrimeMeridianOffsetDeg);
            }

            float latDegMid = (id.LatBin + 0.5f) / latCount * 180f - 90f;
            float lonDegMid = (id.LonBin + 0.5f) / lonCount * 360f - 180f;
            return new SphericalCoordinates(latDegMid, lonDegMid, radius)
                .ToWorldPosition(planet.PlanetCenter, planet.StablePoleAxis, planet.PrimeMeridianOffsetDeg);
        }

        public ManifoldCellData Sample(ShellCellId id)
        {
            Vector3 center = CellCenterWorld(id);
            if (weatherManifold != null)
                return weatherManifold.GetDataAtPosition(center);
            return default;
        }

        public void Stamp(ShellCellId id, ManifoldCellData data)
        {
            if (weatherManifold == null)
                return;
            weatherManifold.SetDataAtPosition(CellCenterWorld(id), data);
        }

        /// <summary>Backward-compatible helper for canonical field pullbacks.</summary>
        public bool TryGetShellSample(Vector3 world, out Vector3 shellCellCenter, out ManifoldCellData cellHint)
        {
            shellCellCenter = world;
            cellHint = default;
            if (!TryWorldToCell(world, out ShellCellId id))
                return false;
            shellCellCenter = CellCenterWorld(id);
            cellHint = Sample(id);
            return true;
        }

        public void RebuildAndSyncToWeatherManifold()
        {
            if (weatherManifold == null)
                SceneServiceLookup.TryResolve("weather.physicsManifold", out weatherManifold);
            if (weatherManifold == null || planet == null)
                return;

            var adapter = GetComponent<PlanetShellToWeatherManifoldAdapter>();
            if (adapter == null)
                adapter = gameObject.AddComponent<PlanetShellToWeatherManifoldAdapter>();
            adapter.shellGrid = this;
            adapter.weatherManifold = weatherManifold;
            adapter.SyncAllCells();
        }

        int WrapLonBin(int lonBin) => (lonBin % lonCount + lonCount) % lonCount;

        int ResolveAltitudeBand(float radius)
        {
            float inner = planet.PlanetRadius;
            float outer = planet.PlanetRadius * shellOuterRadiusMultiplier;
            float t = Mathf.InverseLerp(inner, outer, radius);
            return Mathf.Clamp(Mathf.FloorToInt(t * altitudeBandCount), 0, altitudeBandCount - 1);
        }

        float BandCenterRadius(int band)
        {
            float inner = planet.PlanetRadius;
            float outer = planet.PlanetRadius * shellOuterRadiusMultiplier;
            float t = (band + 0.5f) / altitudeBandCount;
            return Mathf.Lerp(inner, outer, t);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (planet == null)
                return;

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            float r = planet.PlanetRadius;
            for (int lat = 0; lat <= latCount; lat++)
            {
                float latDeg = lat / (float)latCount * 180f - 90f;
                if (Mathf.Abs(latDeg) >= PoleCapLatDeg)
                    continue;
                for (int lon = 0; lon < lonCount; lon++)
                {
                    float lonDeg = lon / (float)lonCount * 360f - 180f;
                    Vector3 a = new SphericalCoordinates(latDeg, lonDeg, r)
                        .ToWorldPosition(planet.PlanetCenter, planet.StablePoleAxis, planet.PrimeMeridianOffsetDeg);
                    float nextLon = (lon + 1) / (float)lonCount * 360f - 180f;
                    Vector3 b = new SphericalCoordinates(latDeg, nextLon, r)
                        .ToWorldPosition(planet.PlanetCenter, planet.StablePoleAxis, planet.PrimeMeridianOffsetDeg);
                    Gizmos.DrawLine(a, b);
                }
            }

            Gizmos.color = Color.yellow;
            Vector3 northCap = CellCenterWorld(new ShellCellId(latCount - 1, 0, 0, true));
            Vector3 southCap = CellCenterWorld(new ShellCellId(0, 0, 0, true));
            Gizmos.DrawWireSphere(northCap, r * 0.02f);
            Gizmos.DrawWireSphere(southCap, r * 0.02f);
        }
#endif
    }
}
