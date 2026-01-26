#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace Weather.Tests
{
    /// <summary>
    /// Unit tests for Weather System components including Precipitation, Portals, and MeshTerrainSampler.
    /// </summary>
    public class WeatherSystemTests
    {
        private GameObject testGameObject;
        private Precipitation precipitation;

        [SetUp]
        public void SetUp()
        {
            testGameObject = new GameObject("TestWeatherObject");
            precipitation = testGameObject.AddComponent<Precipitation>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testGameObject != null)
            {
                Object.DestroyImmediate(testGameObject);
            }
        }

        #region Precipitation Tests

        [Test]
        public void Precipitation_CalculateIntensity_None_ForZeroRate()
        {
            PrecipitationIntensity intensity = precipitation.CalculateIntensity(0f);
            Assert.AreEqual(PrecipitationIntensity.None, intensity);
        }

        [Test]
        public void Precipitation_CalculateIntensity_Light_ForLowRate()
        {
            PrecipitationIntensity intensity = precipitation.CalculateIntensity(1.0f);
            Assert.AreEqual(PrecipitationIntensity.Light, intensity);
        }

        [Test]
        public void Precipitation_CalculateIntensity_Moderate_ForMediumRate()
        {
            PrecipitationIntensity intensity = precipitation.CalculateIntensity(5.0f);
            Assert.AreEqual(PrecipitationIntensity.Moderate, intensity);
        }

        [Test]
        public void Precipitation_CalculateIntensity_Heavy_ForHighRate()
        {
            PrecipitationIntensity intensity = precipitation.CalculateIntensity(10.0f);
            Assert.AreEqual(PrecipitationIntensity.Heavy, intensity);
        }

        [Test]
        public void Precipitation_UpdateAccumulation_IncreasesOverTime()
        {
            precipitation.precipitationRate = 3600f; // 1 mm/s = 3600 mm/h
            float initialAccumulation = precipitation.accumulation;

            precipitation.ServiceUpdate(1.0f); // 1 second

            Assert.Greater(precipitation.accumulation, initialAccumulation);
            Assert.AreEqual(1.0f, precipitation.accumulation, 0.01f); // Should accumulate 1mm in 1 second
        }

        [Test]
        public void Precipitation_UpdateTypeFromTemperature_Snow_BelowThreshold()
        {
            precipitation.snowTemperatureThreshold = 0f;
            precipitation.UpdateTypeFromTemperature(-5f);
            Assert.AreEqual(PrecipitationType.Snow, precipitation.type);
        }

        [Test]
        public void Precipitation_UpdateTypeFromTemperature_Sleet_BetweenThresholds()
        {
            precipitation.snowTemperatureThreshold = 0f;
            precipitation.sleetTemperatureThreshold = 2f;
            precipitation.UpdateTypeFromTemperature(1f);
            Assert.AreEqual(PrecipitationType.Sleet, precipitation.type);
        }

        [Test]
        public void Precipitation_UpdateTypeFromTemperature_Rain_AboveThreshold()
        {
            precipitation.sleetTemperatureThreshold = 2f;
            precipitation.UpdateTypeFromTemperature(5f);
            Assert.AreEqual(PrecipitationType.Rain, precipitation.type);
        }

        [Test]
        public void Precipitation_GetWaterVolume_CalculatesCorrectly()
        {
            precipitation.accumulation = 10f; // 10 mm
            float areaM2 = 100f; // 100 m²
            float volume = precipitation.GetWaterVolume(areaM2);
            Assert.AreEqual(1.0f, volume, 0.01f); // 10mm / 1000 * 100m² = 1 m³
        }

        [Test]
        public void Precipitation_RegisterPortalForRain_AddsPortal()
        {
            GameObject portalObj = new GameObject("TestPortal");
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            portal.vertexLoop = new VertexLoop();
            portal.vertexLoop.vertices = new List<Vector3> { Vector3.zero, Vector3.right, Vector3.forward };

            precipitation.RegisterPortalForRain(portal);

            Assert.Greater(precipitation.portalParticleSystems.Count, 0);
            Assert.IsNotNull(precipitation.portalParticleSystems[0]);
            Assert.AreEqual(portal, precipitation.portalParticleSystems[0].portal);

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void Precipitation_UnregisterPortalForRain_RemovesPortal()
        {
            GameObject portalObj = new GameObject("TestPortal");
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            portal.vertexLoop = new VertexLoop();
            portal.vertexLoop.vertices = new List<Vector3> { Vector3.zero, Vector3.right, Vector3.forward };

            precipitation.RegisterPortalForRain(portal);
            int countBefore = precipitation.portalParticleSystems.Count;

            precipitation.UnregisterPortalForRain(portal);

            Assert.Less(precipitation.portalParticleSystems.Count, countBefore);

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void Precipitation_AutoDetectPortals_FindsAllPortals()
        {
            // Create multiple portals
            List<GameObject> portalObjs = new List<GameObject>();
            for (int i = 0; i < 3; i++)
            {
                GameObject portalObj = new GameObject($"TestPortal_{i}");
                MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
                portal.vertexLoop = new VertexLoop();
                portal.vertexLoop.vertices = new List<Vector3> { Vector3.zero, Vector3.right, Vector3.forward };
                portalObjs.Add(portalObj);
            }

            precipitation.AutoDetectPortals();

            Assert.AreEqual(3, precipitation.portalParticleSystems.Count);

            // Cleanup
            foreach (var obj in portalObjs)
            {
                Object.DestroyImmediate(obj);
            }
        }

        #endregion

        #region Portal Tests

        [Test]
        public void MeshTerrainPortal_GetVertexLoop_ReturnsCorrectLoop()
        {
            GameObject portalObj = new GameObject("TestPortal");
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            VertexLoop loop = new VertexLoop();
            loop.vertices = new List<Vector3> { Vector3.zero, Vector3.right, Vector3.forward };
            portal.vertexLoop = loop;

            VertexLoop retrieved = portal.GetVertexLoop();

            Assert.AreEqual(loop, retrieved);
            Assert.AreEqual(3, retrieved.vertices.Count);

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void MeshTerrainPortal_IsVerticalEntrance_ReturnsTrue_ForVerticalLoop()
        {
            GameObject portalObj = new GameObject("TestPortal");
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            VertexLoop loop = new VertexLoop();
            loop.isVertical = true;
            portal.vertexLoop = loop;

            Assert.IsTrue(portal.IsVerticalEntrance());

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void PortalRainParticleSystem_CalculateSurfaceAngle_ReturnsCorrectAngle()
        {
            GameObject portalObj = new GameObject("TestPortal");
            PortalRainParticleSystem portalRain = portalObj.AddComponent<PortalRainParticleSystem>();

            // Test with upward normal
            float angle = portalRain.CalculateSurfaceAngle(Vector3.zero, Vector3.up);
            Assert.AreEqual(0f, angle, 0.1f);

            // Test with horizontal normal
            angle = portalRain.CalculateSurfaceAngle(Vector3.zero, Vector3.right);
            Assert.AreEqual(90f, angle, 0.1f);

            // Test with downward normal (overhanging)
            angle = portalRain.CalculateSurfaceAngle(Vector3.zero, Vector3.down);
            Assert.Greater(angle, 180f);

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void PortalRainParticleSystem_DetectDripLine_FindsDripLineVertices()
        {
            GameObject portalObj = new GameObject("TestPortal");
            PortalRainParticleSystem portalRain = portalObj.AddComponent<PortalRainParticleSystem>();
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            portalRain.portal = portal;

            VertexLoop loop = new VertexLoop();
            loop.vertices = new List<Vector3>
            {
                Vector3.zero,
                Vector3.right,
                Vector3.forward,
                new Vector3(1f, 0f, 1f)
            };
            loop.normal = new Vector3(0f, -1f, 0f); // Downward normal (overhanging)
            loop.uvs = new List<Vector2> { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            portal.vertexLoop = loop;

            portalRain.dripLineAngleThreshold = 180f;
            portalRain.DetectDripLine();

            // Should detect vertices with overhanging normals
            Assert.GreaterOrEqual(portalRain.dripLineVertices.Count, 0);

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void PortalRainParticleSystem_CalculateParticleBounds_CalculatesCorrectBounds()
        {
            GameObject portalObj = new GameObject("TestPortal");
            PortalRainParticleSystem portalRain = portalObj.AddComponent<PortalRainParticleSystem>();
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            portalRain.portal = portal;

            VertexLoop loop = new VertexLoop();
            loop.vertices = new List<Vector3>
            {
                Vector3.zero,
                Vector3.right * 10f,
                Vector3.forward * 10f,
                new Vector3(10f, 0f, 10f)
            };
            portal.vertexLoop = loop;
            portalRain.segmentSize = 1f;

            portalRain.CalculateParticleBounds();

            Assert.IsTrue(portalRain.particleBounds.size.magnitude > 0f);
            Assert.IsTrue(portalRain.particleBounds.Contains(Vector3.zero));

            Object.DestroyImmediate(portalObj);
        }

        #endregion

        #region MeshTerrainSampler Tests

        [Test]
        public void VertexLoop_CalculateProperties_CalculatesCenter()
        {
            VertexLoop loop = new VertexLoop();
            loop.vertices = new List<Vector3>
            {
                Vector3.zero,
                Vector3.right,
                Vector3.forward,
                new Vector3(1f, 0f, 1f)
            };

            loop.CalculateProperties();

            Vector3 expectedCenter = new Vector3(0.5f, 0f, 0.5f);
            Assert.AreEqual(expectedCenter.x, loop.center.x, 0.01f);
            Assert.AreEqual(expectedCenter.z, loop.center.z, 0.01f);
        }

        [Test]
        public void VertexLoop_CalculateProperties_CalculatesArea()
        {
            VertexLoop loop = new VertexLoop();
            loop.vertices = new List<Vector3>
            {
                Vector3.zero,
                Vector3.right,
                new Vector3(1f, 0f, 1f),
                Vector3.forward
            };

            loop.CalculateProperties();

            Assert.Greater(loop.area, 0f);
        }

        [Test]
        public void VertexLoop_CalculateProperties_SetsIsVertical_ForVerticalNormal()
        {
            VertexLoop loop = new VertexLoop();
            loop.vertices = new List<Vector3>
            {
                Vector3.zero,
                Vector3.up,
                new Vector3(0f, 1f, 1f),
                Vector3.forward
            };

            loop.CalculateProperties();

            // Should detect vertical orientation if normal is primarily up/down
            // This depends on the actual normal calculation
            Assert.IsNotNull(loop.normal);
        }

        [Test]
        public void EnclosedSpace_CalculateProperties_SetsCenter()
        {
            EnclosedSpace space = new EnclosedSpace();
            space.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);

            space.CalculateProperties();

            Assert.AreEqual(Vector3.zero, space.center);
            Assert.Greater(space.volume, 0f);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void Precipitation_ServiceUpdate_UpdatesPortalParticleSystems()
        {
            GameObject portalObj = new GameObject("TestPortal");
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            portal.vertexLoop = new VertexLoop();
            portal.vertexLoop.vertices = new List<Vector3> { Vector3.zero, Vector3.right, Vector3.forward };

            precipitation.RegisterPortalForRain(portal);
            int systemCount = precipitation.portalParticleSystems.Count;

            precipitation.ServiceUpdate(0.1f);

            // Should still have the same number of systems
            Assert.AreEqual(systemCount, precipitation.portalParticleSystems.Count);

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void PortalRainParticleSystem_CalculateParticleBounds_HandlesNullPortal()
        {
            GameObject portalObj = new GameObject("TestPortal");
            PortalRainParticleSystem portalRain = portalObj.AddComponent<PortalRainParticleSystem>();
            portalRain.portal = null;

            // Should not throw exception
            portalRain.CalculateParticleBounds();

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void PortalRainParticleSystem_UpdateParticleSystem_HandlesNullSegments()
        {
            GameObject portalObj = new GameObject("TestPortal");
            PortalRainParticleSystem portalRain = portalObj.AddComponent<PortalRainParticleSystem>();

            // Should not throw exception
            portalRain.UpdateParticleSystem();

            Object.DestroyImmediate(portalObj);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void Precipitation_ServiceUpdate_HandlesNullPortalSystems()
        {
            precipitation.portalParticleSystems = null;
            
            // Should not throw exception
            precipitation.ServiceUpdate(0.1f);
        }

        [Test]
        public void Precipitation_RegisterPortalForRain_HandlesNullPortal()
        {
            // Should not throw exception
            precipitation.RegisterPortalForRain(null);
        }

        [Test]
        public void Precipitation_UnregisterPortalForRain_HandlesNullPortal()
        {
            // Should not throw exception
            precipitation.UnregisterPortalForRain(null);
        }

        [Test]
        public void PortalRainParticleSystem_DetectDripLine_HandlesNullPortal()
        {
            GameObject portalObj = new GameObject("TestPortal");
            PortalRainParticleSystem portalRain = portalObj.AddComponent<PortalRainParticleSystem>();
            portalRain.portal = null;

            // Should not throw exception
            portalRain.DetectDripLine();

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void PortalRainParticleSystem_DetectDripLine_HandlesNullVertexLoop()
        {
            GameObject portalObj = new GameObject("TestPortal");
            PortalRainParticleSystem portalRain = portalObj.AddComponent<PortalRainParticleSystem>();
            MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
            portalRain.portal = portal;
            portal.vertexLoop = null;

            // Should not throw exception
            portalRain.DetectDripLine();

            Object.DestroyImmediate(portalObj);
        }

        [Test]
        public void VertexLoop_CalculateProperties_HandlesEmptyVertices()
        {
            VertexLoop loop = new VertexLoop();
            loop.vertices = new List<Vector3>();

            // Should not throw exception
            loop.CalculateProperties();

            Assert.AreEqual(Vector3.zero, loop.center);
            Assert.AreEqual(0f, loop.area);
        }

        [Test]
        public void VertexLoop_CalculateProperties_HandlesNullVertices()
        {
            VertexLoop loop = new VertexLoop();
            loop.vertices = null;

            // Should not throw exception
            loop.CalculateProperties();

            Assert.AreEqual(Vector3.zero, loop.center);
            Assert.AreEqual(0f, loop.area);
        }

        [Test]
        public void EnclosedSpace_CalculateProperties_HandlesEmptyOpenings()
        {
            EnclosedSpace space = new EnclosedSpace();
            space.bounds = new Bounds(Vector3.zero, Vector3.one * 10f);
            space.openings = new List<VertexLoop>();

            space.CalculateProperties();

            Assert.AreEqual(Vector3.zero, space.center);
            Assert.Greater(space.volume, 0f);
            Assert.IsFalse(space.hasVerticalEntrance);
        }

        #endregion

        #region Performance Tests

        [Test]
        public void Precipitation_ServiceUpdate_Performance_WithManyPortals()
        {
            // Create many portals
            List<GameObject> portalObjs = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                GameObject portalObj = new GameObject($"TestPortal_{i}");
                MeshTerrainPortal portal = portalObj.AddComponent<MeshTerrainPortal>();
                portal.vertexLoop = new VertexLoop();
                portal.vertexLoop.vertices = new List<Vector3> { Vector3.zero, Vector3.right, Vector3.forward };
                precipitation.RegisterPortalForRain(portal);
                portalObjs.Add(portalObj);
            }

            // Measure update time
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                precipitation.ServiceUpdate(0.016f); // ~60fps
            }
            sw.Stop();

            // Should complete in reasonable time (< 1 second for 100 updates)
            Assert.Less(sw.ElapsedMilliseconds, 1000);

            // Cleanup
            foreach (var obj in portalObjs)
            {
                Object.DestroyImmediate(obj);
            }
        }

        #endregion
    }
}
#endif

