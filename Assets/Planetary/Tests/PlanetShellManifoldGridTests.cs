using System.Collections.Generic;
using NUnit.Framework;
using Planetary;
using Planetary.Bridges;
using UnityEngine;

namespace Planetary.Tests
{
    public class PlanetShellManifoldGridTests
    {
        PlanetBody _planet;
        PlanetShellManifoldGrid _grid;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("PlanetTest");
            _planet = go.AddComponent<PlanetBody>();
            _planet.planetRadius = 1000f;
            _planet.stablePoleAxis = Vector3.up;
            _grid = go.AddComponent<PlanetShellManifoldGrid>();
            _grid.planet = _planet;
            _grid.latCount = 18;
            _grid.lonCount = 36;
            _grid.altitudeBandCount = 4;
        }

        [TearDown]
        public void TearDown()
        {
            if (_planet != null)
                Object.DestroyImmediate(_planet.gameObject);
        }

        Vector3 ShellWorld(float latDeg, float lonDeg, float radiusScale = 1f)
        {
            float radius = _planet.PlanetRadius * radiusScale;
            return new SphericalCoordinates(latDeg, lonDeg, radius)
                .ToWorldPosition(_planet.PlanetCenter, _planet.StablePoleAxis, _planet.PrimeMeridianOffsetDeg);
        }

        [Test]
        public void LongitudeWrap_Maps359ToSameBinAsZero()
        {
            Vector3 nearZero = ShellWorld(0f, 0f);
            Vector3 near359 = ShellWorld(0f, 359f);

            Assert.IsTrue(_grid.TryWorldToCell(nearZero, out ShellCellId a));
            Assert.IsTrue(_grid.TryWorldToCell(near359, out ShellCellId b));
            Assert.AreEqual(a.LonBin, b.LonBin);
        }

        [Test]
        public void PoleCap_UsesSingleCell()
        {
            Vector3 north = ShellWorld(89.5f, 45f);
            Vector3 north2 = ShellWorld(89.5f, 200f);

            Assert.IsTrue(_grid.TryWorldToCell(north, out ShellCellId a));
            Assert.IsTrue(_grid.TryWorldToCell(north2, out ShellCellId b));
            Assert.IsTrue(a.IsPoleCap);
            Assert.IsTrue(b.IsPoleCap);
            Assert.AreEqual(a.LatBin, b.LatBin);
        }

        [Test]
        public void WorldCellWorld_RoundTripWithinEpsilon()
        {
            Vector3 world = ShellWorld(25f, 120f, 1.05f);

            Assert.IsTrue(_grid.TryWorldToCell(world, out ShellCellId id));
            Vector3 center = _grid.CellCenterWorld(id);
            Assert.Less(Vector3.Distance(world, center), _planet.PlanetRadius * 0.15f);
        }

        /// <summary>
        /// Shell indexing is purely spherical — it must not depend on SDF/plate coverage.
        /// Every azimuth at the equator (including directions where composition may leave holes) maps to a cell.
        /// </summary>
        [Test]
        public void FullAzimuthSweep_Equator_ResolvesEveryDegree()
        {
            const int steps = 360;
            var seenLonBins = new HashSet<int>();

            for (int i = 0; i < steps; i++)
            {
                float lonDeg = i - 180f;
                Vector3 world = ShellWorld(0f, lonDeg);

                Assert.IsTrue(_grid.TryWorldToCell(world, out ShellCellId id), $"TryWorldToCell failed at lon={lonDeg}°");
                Assert.IsFalse(id.IsPoleCap, $"Equator point mapped to pole cap at lon={lonDeg}°");

                seenLonBins.Add(id.LonBin);

                Vector3 center = _grid.CellCenterWorld(id);
                float centerRadius = Vector3.Distance(center, _planet.PlanetCenter);
                Assert.That(centerRadius, Is.InRange(_planet.PlanetRadius * 0.95f, _planet.PlanetRadius * 1.25f),
                    $"Cell center off shell at lon={lonDeg}°");
            }

            Assert.AreEqual(_grid.lonCount, seenLonBins.Count,
                "Equatorial 360° sweep did not reach every longitude bin");
        }

        [Test]
        public void FullAzimuthSweep_MultipleLatitudes_CoversAllLonBins()
        {
            float[] latitudes = { -70f, -40f, 0f, 40f, 70f };
            var seenLonBins = new HashSet<int>();

            foreach (float lat in latitudes)
            {
                for (int lonStep = 0; lonStep < 360; lonStep++)
                {
                    float lonDeg = lonStep - 180f;
                    Vector3 world = ShellWorld(lat, lonDeg, 1.05f);

                    Assert.IsTrue(_grid.TryWorldToCell(world, out ShellCellId id),
                        $"Failed at lat={lat}° lon={lonDeg}°");

                    if (!id.IsPoleCap)
                        seenLonBins.Add(id.LonBin);
                }
            }

            Assert.AreEqual(_grid.lonCount, seenLonBins.Count,
                "Multi-latitude azimuth sweep did not cover all longitude bins");
        }

        /// <summary>
        /// Every discrete shell cell (lat × lon × altitude band, including pole caps).
        /// Default grid: 18×36×4 → 2312 cells. Increase latCount/lonCount for finer squares later.
        /// </summary>
        [Test]
        public void EveryGridCell_CenterRoundTripsAndIsDistinct()
        {
            var seen = new HashSet<ShellCellId>();
            int visited = 0;

            _grid.EnumerateAllCells(id =>
            {
                visited++;
                Assert.IsTrue(seen.Add(id), $"Duplicate shell cell {id}");

                Vector3 center = _grid.CellCenterWorld(id);
                float radius = Vector3.Distance(center, _planet.PlanetCenter);
                Assert.That(radius,
                    Is.InRange(_planet.PlanetRadius, _planet.PlanetRadius * _grid.shellOuterRadiusMultiplier * 1.01f),
                    $"Cell center off shell for {id}");

                Assert.IsTrue(_grid.TryWorldToCell(center, out ShellCellId resolved),
                    $"TryWorldToCell failed at center of {id}");
                Assert.AreEqual(id, resolved, $"Cell center round-trip mismatch for {id}");
            });

            Assert.AreEqual(_grid.TotalCellCount, visited,
                "EnumerateAllCells count should match TotalCellCount");
            Assert.AreEqual(_grid.TotalCellCount, seen.Count);
        }

        /// <summary>
        /// Finer grid smoke test (2× resolution in lat/lon). Full enumeration stays fast; increase counts when needed.
        /// </summary>
        [Test]
        public void EveryGridCell_FinerResolution_StillRoundTrips()
        {
            _grid.latCount = 36;
            _grid.lonCount = 72;
            _grid.altitudeBandCount = 4;

            int failures = 0;
            _grid.EnumerateAllCells(id =>
            {
                Vector3 center = _grid.CellCenterWorld(id);
                if (!_grid.TryWorldToCell(center, out ShellCellId resolved) || !id.Equals(resolved))
                    failures++;
            });

            Assert.AreEqual(0, failures, "Finer grid had cell center round-trip failures");
            Assert.AreEqual(_grid.TotalCellCount, 2 * 4 + 34 * 72 * 4);
        }

        /// <summary>
        /// Exhaustive angular sampling: every integer latitude (−90…+90) × every integer longitude (360°).
        /// Complements <see cref="EveryGridCell_CenterRoundTripsAndIsDistinct"/> (per-cell vs per-degree).
        /// </summary>
        [Test]
        [Timeout(600000)]
        public void FullSphereSweep_EveryLatAndLonDegree_ResolvesAcrossGlobe()
        {
            var seenNonPoleLatBins = new HashSet<int>();
            var seenLonBins = new HashSet<int>();
            int poleCapSamples = 0;
            int nonPoleSamples = 0;

            for (int latDeg = -90; latDeg <= 90; latDeg++)
            {
                for (int lonStep = 0; lonStep < 360; lonStep++)
                {
                    float lonDeg = lonStep - 180f;
                    Vector3 world = ShellWorld(latDeg, lonDeg, 1.05f);

                    Assert.IsTrue(_grid.TryWorldToCell(world, out ShellCellId id),
                        $"TryWorldToCell failed at lat={latDeg}° lon={lonDeg}°");

                    bool expectPoleCap = Mathf.Abs(latDeg) >= 89;
                    if (expectPoleCap)
                    {
                        Assert.IsTrue(id.IsPoleCap, $"Expected pole cap at lat={latDeg}° lon={lonDeg}°");
                        poleCapSamples++;
                    }
                    else
                    {
                        Assert.IsFalse(id.IsPoleCap, $"Unexpected pole cap at lat={latDeg}° lon={lonDeg}°");
                        seenNonPoleLatBins.Add(id.LatBin);
                        seenLonBins.Add(id.LonBin);
                        nonPoleSamples++;

                        Vector3 center = _grid.CellCenterWorld(id);
                        float centerRadius = Vector3.Distance(center, _planet.PlanetCenter);
                        Assert.That(centerRadius,
                            Is.InRange(_planet.PlanetRadius, _planet.PlanetRadius * _grid.shellOuterRadiusMultiplier * 1.05f),
                            $"Cell center outside shell envelope at lat={latDeg}° lon={lonDeg}°");
                    }
                }
            }

            int expectedNonPoleLatBins = _grid.latCount - 2;
            Assert.AreEqual(expectedNonPoleLatBins, seenNonPoleLatBins.Count,
                $"Expected {expectedNonPoleLatBins} non-pole latitude bins from full lat sweep");
            Assert.AreEqual(_grid.lonCount, seenLonBins.Count,
                "Full sphere sweep did not reach every longitude bin");
            Assert.Greater(poleCapSamples, 0, "Pole-cap latitudes should have been sampled");
            Assert.AreEqual(181 * 360, poleCapSamples + nonPoleSamples,
                "Sample count mismatch for full lat/lon degree grid");
        }

        /// <summary>
        /// Points above the nominal surface (vacuum / SDF holes between plates) still resolve on the shell grid.
        /// </summary>
        [Test]
        public void ElevatedShellRadius_ResolvesAroundFullAzimuth_WithoutSurfaceSample()
        {
            const float radiusScale = 1.12f;

            for (int lonStep = 0; lonStep < 360; lonStep += 5)
            {
                float lonDeg = lonStep - 180f;
                for (float lat = -75f; lat <= 75f; lat += 15f)
                {
                    Vector3 world = ShellWorld(lat, lonDeg, radiusScale);

                    Assert.IsTrue(_grid.TryWorldToCell(world, out ShellCellId id),
                        $"Elevated shell point failed at lat={lat}° lon={lonDeg}°");
                    Assert.GreaterOrEqual(id.AltitudeBand, 0);
                    Assert.Less(id.AltitudeBand, _grid.altitudeBandCount);

                    Vector3 center = _grid.CellCenterWorld(id);
                    float dist = Vector3.Distance(center, _planet.PlanetCenter);
                    Assert.That(dist, Is.InRange(_planet.PlanetRadius, _planet.PlanetRadius * _grid.shellOuterRadiusMultiplier * 1.05f),
                        $"Cell center outside shell envelope at lat={lat}° lon={lonDeg}°");
                }
            }
        }

        [Test]
        public void PrimeMeridianOffset_DoesNotLeaveAzimuthGaps()
        {
            _planet.primeMeridianOffsetDeg = 47f;
            var seenLonBins = new HashSet<int>();

            for (int lonStep = 0; lonStep < 360; lonStep++)
            {
                float lonDeg = lonStep - 180f;
                Vector3 world = ShellWorld(15f, lonDeg);

                Assert.IsTrue(_grid.TryWorldToCell(world, out ShellCellId id));
                if (!id.IsPoleCap)
                    seenLonBins.Add(id.LonBin);
            }

            Assert.AreEqual(_grid.lonCount, seenLonBins.Count,
                "Prime meridian offset should not reduce longitude bin coverage");
        }
    }
}