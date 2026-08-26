#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class AmbulationCrowdTests
{
    [Test]
    public void AmbulationCache_HitWithinTolerance_MissWhenOver()
    {
        AmbulationPathCache.Clear();
        var plan = new GenericMultiModalPathPlan();
        plan.segments.Add(new MultiModalSegment
        {
            waypoints = new List<Vector3> { Vector3.zero, Vector3.forward * 8f }
        });
        AmbulationPathCache.Put("quad", Vector3.zero, Vector3.forward * 8f, 1, plan, 1.5f);

        Assert.IsTrue(AmbulationPathCache.TryGet(
            "quad", Vector3.zero + Vector3.right * 0.4f, Vector3.forward * 8f, 1, 1.5f, out _));
        Assert.IsFalse(AmbulationPathCache.TryGet(
            "quad", Vector3.zero + Vector3.right * 4f, Vector3.forward * 8f, 1, 1.5f, out _));
        AmbulationPathCache.Clear();
    }

    [Test]
    public void Boids_Separation_PushesAwayFromNeighbor()
    {
        Vector3 offset = BoidsCrowdLayer.SeparationOffset(Vector3.zero, Vector3.right * 0.5f);
        Assert.Less(offset.x, 0f);
    }

    [Test]
    public void AmbulationLikelihood_NonHumanDefaultsHigherThanHuman()
    {
        Assert.Greater(AmbulationPathCache.NonHumanLikelihood01, AmbulationPathCache.HumanLikelihood01);
        var animalGo = new GameObject("Animal");
        var vehicleGo = new GameObject("Vehicle");
        var humanGo = new GameObject("Human");
        try
        {
            var animal = animalGo.AddComponent<AnimalAmbulatingActor>();
            var vehicle = vehicleGo.AddComponent<VehicleActor>();
            var human = humanGo.AddComponent<BaseAmbulatingActor>();
            Assert.AreEqual(AmbulationPathCache.NonHumanLikelihood01, AmbulationPathCache.DefaultLikelihood01(animal));
            Assert.AreEqual(AmbulationPathCache.NonHumanLikelihood01, AmbulationPathCache.DefaultLikelihood01(vehicle));
            Assert.AreEqual(AmbulationPathCache.HumanLikelihood01, AmbulationPathCache.DefaultLikelihood01(human));
        }
        finally
        {
            Object.DestroyImmediate(animalGo);
            Object.DestroyImmediate(vehicleGo);
            Object.DestroyImmediate(humanGo);
        }
    }

    [Test]
    public void CampusLayers_AndCrowdStamp_ApplyToTravelAgent()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        grid.width = 8;
        grid.height = 8;
        grid.EnsureCampusLayers();
        Assert.IsTrue(grid.layers.Exists(l => l != null && l.kind == CityPixelLayerKind.CampusLecture));

        var go = new GameObject("CrowdAgent");
        try
        {
            var agent = go.AddComponent<TravelAgent>();
            TravelAgentRegistry.Register(agent);
            go.transform.position = grid.CellToWorld(1, 1);
            var stamp = new CityPixelBrushStamp
            {
                cellX = 1,
                cellY = 1,
                crowdHint = CityPixelCrowdHint.Flock,
                flockGroupId = "quad",
                ambulationCacheKey = "walk-quad",
                cacheLikelihood01 = 0.8f,
                cacheToleranceM = 2f,
                travelHintRow = new TravelAuthoringRow { kind = TravelAuthoringRowKind.Hint, notes = "commute" }
            };
            CityPixelCrowdHints.ApplyStamp(stamp, grid.CellToWorld(1, 1), 8f);
            Assert.AreEqual(CityPixelCrowdHint.Flock, agent.crowdHint);
            Assert.AreEqual("quad", agent.flockGroupId);
            Assert.AreEqual("walk-quad", agent.ambulationCacheKey);
        }
        finally
        {
            var leftover = go.GetComponent<TravelAgent>();
            if (leftover != null)
                TravelAgentRegistry.Unregister(leftover);
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void CrowdDither_ScalesWithOccupancy()
    {
        Assert.Greater(Locomotion.Rendering.TransparentOccluder.DitherFromOccupancy(0.9f),
            Locomotion.Rendering.TransparentOccluder.DitherFromOccupancy(0.1f));
    }
}
#endif
