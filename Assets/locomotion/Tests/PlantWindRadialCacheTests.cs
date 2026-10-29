using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Weather;
using Weather.Lod;

public sealed class PlantWindRadialCacheTests
{
    [Test]
    public void ShaderDelta_MatchesControlPoints_ShelterAndEggLod()
    {
        var created = new List<GameObject>();
        GameObject root = null;
        try
        {
            root = new GameObject("plant-wind");
            root.SetActive(false);
            created.Add(root);
            var wind = root.AddComponent<Wind>();
            wind.speed = 0.3f;
            wind.direction = 270f;
            wind.enableWindVariation = false;
            wind.autoGenerateAltitudeLevels = true;
            var weather = root.AddComponent<WeatherSystem>();
            weather.autoFindSubsystems = false;
            weather.wind = wind;
            var manifold = root.AddComponent<WeatherPhysicsManifold>();
            manifold.cellCount = new Vector3Int(4, 1, 4);
            manifold.worldBounds = new Bounds(Vector3.zero, new Vector3(8f, 2f, 8f));
            manifold.showGizmos = false;
            weather.weatherPhysicsManifold = manifold;
            var service = root.AddComponent<WindAdvectionPlantRadialCachingService>();
            service.weather = weather;
            service.shelterFactor = 0.45f;
            service.gustAmplitude = 0.15f;
            service.eggMoveThreshold = 1f;
            service.cacheRealtimeOnEggMove = true;

            var eggGo = new GameObject("egg");
            created.Add(eggGo);
            var egg = eggGo.AddComponent<PlayerWeatherEggZone>();
            egg.radii = new Vector3(3f, 3f, 3f);
            service.egg = egg;

            var treeGo = new GameObject("tree");
            created.Add(treeGo);
            var tree = treeGo.AddComponent<TreeGrowthTravelAgent>();
            TreeGrowthStep growth = tree.SelectedStep;
            growth.weather01 = growth.water01 = growth.sun01 = growth.minerals01 = 1f;
            growth.limitWeather01 = growth.limitWater01 = growth.limitSun01 = growth.limitMinerals01 = 0.5f;

            var flowerGo = new GameObject("flower");
            created.Add(flowerGo);
            var flower = flowerGo.AddComponent<FlowerTravelAgent>();
            FlowerDevStep bloom = flower.SelectedStep;
            bloom.water01 = bloom.sun01 = bloom.ground01 = 1f;
            bloom.limitWater01 = bloom.limitSun01 = bloom.limitGround01 = 0.2f;
            bloom.limitRetinue01 = 0f;

            root.SetActive(true);

            PlantWindSubject upstream = MakeSubject(tree, flower, Vector3.zero);
            PlantWindSubject downstream = MakeSubject(tree, null, new Vector3(0.5f, 0f, 0f));
            PlantWindSubject far = MakeSubject(tree, null, new Vector3(6f, 0f, 0f));
            service.copse.Add(upstream);
            service.copse.Add(downstream);
            service.copse.Add(far);

            Assert.IsTrue(WindAdvectionPlantRadialCachingService.ShareManifoldCell(
                manifold, upstream.worldPosition, downstream.worldPosition));
            Assert.IsFalse(WindAdvectionPlantRadialCachingService.ShareManifoldCell(
                manifold, upstream.worldPosition, far.worldPosition));

            float force = 0.3f;
            float length = 1f;
            float stiffness = 0.5f;
            float limit = 0.5f;
            float tip = service.ControlPointDelta(force, length, stiffness, limit);
            var bin = new PlantWindBin
            {
                bend = tip,
                advectedWind = force,
                baseForce = force,
                effectiveLimit = limit,
                bendAxis = Vector3.right
            };
            Color tipTexel = PlantRadialWindCache.EncodeTexel(bin, 1f);
            Color midTexel = PlantRadialWindCache.EncodeTexel(bin, 0.5f);
            Assert.AreEqual(tip, PlantRadialWindCache.ShaderDelta(tipTexel), 1e-5f);
            Assert.AreEqual(
                PlantRadialWindCache.AlongDelta(tip, 0.5f, limit),
                PlantRadialWindCache.ShaderDelta(midTexel),
                1e-5f);
            Assert.AreEqual(0.2f, PlantRadialWindCache.EffectiveLimit(0.2f, 1f, 0.8f), 1e-5f);

            service.RebakeAll(1);
            Assert.LessOrEqual(upstream.cache.petals[0].limit, upstream.cache.tree.limit);
            Assert.AreEqual(0.2f, upstream.cache.petals[0].limit, 1e-4f);
            for (int i = 0; i < upstream.cache.petals[0].bins.Length; i++)
            {
                Assert.LessOrEqual(upstream.cache.petals[0].bins[i].bend, upstream.cache.petals[0].limit + 1e-4f);
                Assert.LessOrEqual(upstream.cache.petals[0].bins[i].effectiveLimit, upstream.cache.tree.limit);
            }

            Vector3 blowing = service.SampleWind(upstream.worldPosition);
            int sector = PlantRadialWindCache.SectorForDirection(blowing, service.sectors);
            float upstreamBend = upstream.cache.tree.bins[sector].bend;
            float downstreamBend = downstream.cache.tree.bins[sector].bend;
            float farBend = far.cache.tree.bins[sector].bend;
            Assert.AreEqual(upstreamBend, farBend, 1e-4f);
            Assert.Less(downstreamBend, upstreamBend);

            float before = BendSum(upstream.cache.tree);
            service.RebakeAll(2);
            Assert.AreNotEqual(before, BendSum(upstream.cache.tree));
            service.RebakeAll(1);

            Vector3 flowerRest = new Vector3(0.09f, 0.16f, 0f);
            upstream.flowerCentroid = flowerRest;
            service.ApplyLod(upstream, false);
            AssertPositions(upstream.branchRest, upstream.branchBake.positions);
            AssertPositions(upstream.fruitRest, upstream.fruitBake.positions);
            service.ApplyLod(upstream, true);
            Assert.Greater(Vector3.Distance(upstream.branchRest[upstream.branchRest.Length - 1], upstream.branchBake.positions[upstream.branchRest.Length - 1]), 1e-4f);
            Assert.Greater(Vector3.Distance(upstream.fruitRest[upstream.fruitRest.Length - 1], upstream.fruitBake.positions[upstream.fruitRest.Length - 1]), 1e-4f);
            Assert.AreEqual(flowerRest, service.ApplyFlowerCentroid(flowerRest, true));
            Assert.AreEqual(flowerRest, upstream.flowerCentroid);

            service.TickEggCenter(Vector3.zero, 1);
            int nearStamp = upstream.cache.rebakeStamp;
            int farStamp = far.cache.rebakeStamp;
            service.TickEggCenter(Vector3.zero, 1);
            Assert.AreEqual(nearStamp, upstream.cache.rebakeStamp);
            Assert.AreEqual(farStamp, far.cache.rebakeStamp);
            service.TickEggCenter(new Vector3(6f, 0f, 0f), 1);
            Assert.AreEqual(nearStamp, upstream.cache.rebakeStamp);
            Assert.Greater(far.cache.rebakeStamp, farStamp);
            service.cacheRealtimeOnEggMove = false;
            int held = far.cache.rebakeStamp;
            service.TickEggCenter(new Vector3(20f, 0f, 0f), 3);
            Assert.AreEqual(held, far.cache.rebakeStamp);

            PlantBranchDef snappedDef = new PlantBranchDef
            {
                broken = true,
                lengthMin = 1f,
                lengthMax = 1f,
                curvePoints = new List<Vector3> { Vector3.zero, Vector3.forward }
            };
            BranchPathBake snapped = BranchPathBake.Bake(snappedDef, 3, 4);
            Assert.IsTrue(snapped.broken);
            var snappedSubject = new PlantWindSubject
            {
                worldPosition = new Vector3(0f, 0f, 2f),
                tree = tree,
                branch = snappedDef,
                branchBake = snapped,
                branchRest = (Vector3[])snapped.positions.Clone(),
                fruitBake = BranchPathBake.Bake(snappedDef, 4, 4),
                limbTangent = Vector3.forward,
                limbLength = snapped.length
            };
            snappedSubject.fruitRest = (Vector3[])snappedSubject.fruitBake.positions.Clone();
            service.BakeSubject(snappedSubject, 1);
            Assert.AreEqual(0, snappedSubject.cache.branches.Count);
            Assert.AreEqual(0, snappedSubject.cache.leaves.Count);
            Assert.AreEqual(0, snappedSubject.cache.petals.Count);
            Assert.AreEqual(0, snappedSubject.cache.fruits.Count);
            Assert.IsTrue(snappedSubject.cache.omitBend);
            Vector3 snappedTip = snappedSubject.branchRest[snappedSubject.branchRest.Length - 1];
            Vector3 fruitTip = snappedSubject.fruitRest[snappedSubject.fruitRest.Length - 1];
            service.ApplyLod(snappedSubject, true);
            Assert.AreEqual(snappedTip, snappedSubject.branchBake.positions[snappedSubject.branchRest.Length - 1]);
            Assert.AreEqual(fruitTip, snappedSubject.fruitBake.positions[snappedSubject.fruitRest.Length - 1]);
            Assert.AreEqual(snappedTip, PlantRadialWindCache.ApplyWindVertex(snappedTip, true, 0.4f, Vector3.right, 1f));
            Assert.IsTrue(service.passCapsules.TryGet(snappedSubject.capsuleId, out PlantWindCapsule fallCap));
            Assert.AreEqual(PlantWindCapsuleIntent.Fall, fallCap.intent);
            Assert.AreEqual(service.limbRadius + service.capsuleExtraSize, fallCap.TotalRadius, 1e-4f);
            Assert.Greater(fallCap.TotalRadius, service.limbRadius);
            Assert.IsNull(fallCap.sourceCollider);
            Assert.IsTrue(fallCap.IsShaderInclusion);

            var treeRoot = new GameObject("tree-root");
            created.Add(treeRoot);
            var ownCollider = treeRoot.AddComponent<SphereCollider>();
            ownCollider.radius = 0.3f;
            Assert.AreEqual(0, service.passCapsules.ClaimOutsideDebris(ownCollider, treeRoot.transform, service.capsuleExtraSize));

            var debrisGo = new GameObject("outside-debris");
            created.Add(debrisGo);
            debrisGo.transform.position = new Vector3(4f, 0f, 0f);
            var debris = debrisGo.AddComponent<SphereCollider>();
            debris.radius = 0.22f;
            int includedBefore = service.passCapsules.ShaderInclusionCount;
            int debrisId = service.passCapsules.ClaimOutsideDebris(debris, treeRoot.transform, service.capsuleExtraSize);
            Assert.Greater(debrisId, 0);
            Assert.AreEqual(includedBefore + 1, service.passCapsules.ShaderInclusionCount);
            Assert.IsTrue(service.passCapsules.TryGet(debrisId, out PlantWindCapsule debrisCap));
            Assert.AreEqual(debris, debrisCap.sourceCollider);
            Assert.IsTrue(debrisCap.outsideDebris);
            Assert.AreEqual(0.22f, debrisCap.radius, 1e-3f);
            Assert.IsTrue(service.passCapsules.Contains(snappedSubject.capsuleId));

            debris.radius = 0.41f;
            Assert.AreEqual(debrisId, service.passCapsules.ClaimOutsideDebris(debris, treeRoot.transform, service.capsuleExtraSize));
            Assert.IsTrue(service.passCapsules.TryGet(debrisId, out debrisCap));
            Assert.AreEqual(0.41f, debrisCap.radius, 1e-3f);

            var climber = new GameObject("climber");
            created.Add(climber);
            climber.transform.position = new Vector3(0f, 2f, 0f);
            var body = climber.AddComponent<CapsuleCollider>();
            body.radius = 0.18f;
            body.height = 1.2f;
            Assert.AreEqual(0, service.passCapsules.ScanAttachmentStress(body, treeRoot.transform, false, 0.8f, service.capsuleExtraSize));
            int climbId = service.passCapsules.ScanAttachmentStress(body, treeRoot.transform, true, 0.8f, service.capsuleExtraSize);
            Assert.Greater(climbId, 0);
            Assert.AreNotEqual(debrisId, climbId);
            Assert.IsTrue(service.passCapsules.TryGet(climbId, out PlantWindCapsule climbCap));
            Assert.AreEqual(service.capsuleExtraSize * 1.8f, climbCap.extraSize, 1e-3f);
            Assert.IsTrue(service.passCapsules.Contains(snappedSubject.capsuleId));

            var wood = new BranchPathBake
            {
                positions = new[] { Vector3.zero, Vector3.right },
                broken = false
            };
            Vector3[] crossing = { Vector3.zero, new Vector3(0.5f, 0f, 0f) };
            Vector3[] fruit = { Vector3.zero };
            var intact = new PlantWindSubject
            {
                worldPosition = new Vector3(1f, 0f, 1f),
                tree = tree,
                branch = new PlantBranchDef
                {
                    lengthMin = 1f,
                    lengthMax = 1f,
                    curvePoints = new List<Vector3> { Vector3.zero, Vector3.forward }
                },
                petalCurve = crossing,
                petalNormal = Vector3.up,
                fruitPoints = fruit,
                liveBranches = new[] { wood },
                fruitClearRadius = 0.2f,
                woodClearRadius = 0.05f,
                limbTangent = Vector3.forward
            };
            intact.branchBake = BranchPathBake.Bake(intact.branch, 5, 4);
            service.BakeSubject(intact, 1);
            Assert.IsFalse(PlantWindClearance.Intersects(
                intact.cache.clearedPetalCurve, fruit, 0.2f, new[] { wood, intact.branchBake }, 0.05f));
            Assert.Greater(intact.cache.petals.Count, 0);

            int capsulesBeforeTear = service.passCapsules.Count;
            var torn = new PlantWindSubject
            {
                worldPosition = new Vector3(1f, 0f, 3f),
                tree = tree,
                branch = new PlantBranchDef
                {
                    lengthMin = 1f,
                    lengthMax = 1f,
                    curvePoints = new List<Vector3> { Vector3.zero, Vector3.forward }
                },
                petalFragment = new SdfSealedClothFragment { broken = true, curve = crossing },
                petalTear01 = 0.5f,
                petalCurve = crossing,
                limbTangent = Vector3.forward
            };
            torn.branchBake = BranchPathBake.Bake(torn.branch, 6, 4);
            service.BakeSubject(torn, 1);
            Assert.AreEqual(0, torn.cache.petals.Count);
            Assert.IsTrue(torn.cache.petalOmitted);
            Assert.AreEqual(0, torn.cache.clearedPetalCurve.Length);
            Assert.AreEqual(capsulesBeforeTear, service.passCapsules.Count);
            Assert.AreEqual(crossing[0], PlantRadialWindCache.ApplyWindVertex(crossing[0], torn.cache.petalOmitted, 0.4f, Vector3.up, 1f));

            int oldestId = snappedSubject.capsuleId;
            for (int i = 0; i < PlantWindCapsuleBuffer.SlotCount; i++)
            {
                service.passCapsules.Push(new PlantWindCapsule
                {
                    a = Vector3.right * i,
                    b = Vector3.right * i + Vector3.up,
                    radius = service.limbRadius,
                    extraSize = service.capsuleExtraSize,
                    intent = PlantWindCapsuleIntent.PassThrough
                });
            }
            Assert.IsFalse(service.passCapsules.Contains(oldestId));
            Assert.AreEqual(PlantWindCapsuleBuffer.SlotCount, service.passCapsules.Count);

            PlantBranchDef lodgedDef = new PlantBranchDef
            {
                broken = true,
                lengthMin = 1f,
                lengthMax = 1f,
                curvePoints = new List<Vector3> { Vector3.zero, Vector3.up }
            };
            BranchPathBake lodgedBake = BranchPathBake.Bake(lodgedDef, 7, 4);
            var lodgedSubject = new PlantWindSubject
            {
                worldPosition = new Vector3(0f, 1f, 2f),
                tree = tree,
                branch = lodgedDef,
                branchBake = lodgedBake,
                branchRest = (Vector3[])lodgedBake.positions.Clone(),
                lodged = true,
                limbTangent = Vector3.up,
                limbLength = lodgedBake.length
            };
            service.BakeSubject(lodgedSubject, 1);
            Assert.IsTrue(service.passCapsules.TryGet(lodgedSubject.capsuleId, out PlantWindCapsule lodgedCap));
            Assert.AreEqual(PlantWindCapsuleIntent.Lodge, lodgedCap.intent);
            Assert.AreEqual(0, lodgedSubject.cache.branches.Count);
            service.BakeSubject(lodgedSubject, 1);
            Assert.AreEqual(0, lodgedSubject.cache.branches.Count);
            Assert.IsTrue(service.passCapsules.TryGet(lodgedSubject.capsuleId, out lodgedCap));
            Assert.AreEqual(PlantWindCapsuleIntent.Lodge, lodgedCap.intent);
            Assert.AreEqual(
                lodgedSubject.branchRest[0],
                PlantRadialWindCache.ApplyWindVertex(lodgedSubject.branchRest[0], lodgedSubject.cache.omitBend, 0.5f, Vector3.right, 1f));
        }
        finally
        {
            if (root != null)
            {
                var service = root.GetComponent<WindAdvectionPlantRadialCachingService>();
                if (service != null)
                {
                    for (int i = 0; i < service.copse.Count; i++)
                    {
                        Texture2D tex = service.copse[i]?.cache?.texture;
                        if (tex != null)
                            Object.DestroyImmediate(tex);
                    }
                }
            }
            for (int i = 0; i < created.Count; i++)
            {
                if (created[i] != null)
                    Object.DestroyImmediate(created[i]);
            }
        }
    }

    static PlantWindSubject MakeSubject(TreeGrowthTravelAgent tree, FlowerTravelAgent flower, Vector3 position)
    {
        var branch = new PlantBranchDef
        {
            lengthMin = 1f,
            lengthMax = 1f,
            curvePoints = new List<Vector3> { Vector3.zero, Vector3.forward },
            windLimit = new PlantPartWindLimit { impulseCap = 4f, bendCap = 1f }
        };
        BranchPathBake limb = BranchPathBake.Bake(branch, 1, 4);
        BranchPathBake fruit = BranchPathBake.Bake(branch, 2, 4);
        return new PlantWindSubject
        {
            worldPosition = position,
            tree = tree,
            flower = flower,
            branch = branch,
            branchBake = limb,
            branchRest = (Vector3[])limb.positions.Clone(),
            fruitBake = fruit,
            fruitRest = (Vector3[])fruit.positions.Clone(),
            flowerCentroid = Vector3.zero,
            limbTangent = Vector3.forward,
            limbLength = limb.length
        };
    }

    static float BendSum(PlantRadialWindRing ring)
    {
        float sum = 0f;
        for (int i = 0; i < ring.bins.Length; i++)
            sum += ring.bins[i].bend;
        return sum;
    }

    static void AssertPositions(Vector3[] rest, Vector3[] current)
    {
        Assert.AreEqual(rest.Length, current.Length);
        for (int i = 0; i < rest.Length; i++)
            Assert.AreEqual(rest[i], current[i]);
    }
}
