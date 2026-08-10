using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class TrainCarTests
{
    [Test]
    public void Consist_CoupleAndUncouple_OrdersCars()
    {
        var root = new GameObject("consist");
        var consist = root.AddComponent<TrainConsistRuntime>();
        consist.consistId = "c1";
        var a = NewCar("a");
        var b = NewCar("b");
        a.transform.SetParent(root.transform);
        b.transform.SetParent(root.transform);
        try
        {
            Assert.IsTrue(a.coupling.CoupleRearTo(b.coupling));
            consist.RebuildFromCouplers(a);
            Assert.AreEqual(2, consist.cars.Count);
            Assert.AreEqual(a, consist.Head);
            Assert.AreEqual(b, consist.Tail);
            Assert.IsTrue(consist.RemoveCar(b));
            Assert.AreEqual(1, consist.cars.Count);
        }
        finally
        {
            // Cars are parented under root — destroy root only (children go with it).
            if (root != null)
                Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void Unfold_ExposesLimbInResultantApi()
    {
        var car = NewCar("car");
        try
        {
            Assert.IsTrue(car.TryUnfoldLimb("main_crane"));
            var limbs = car.Resultants.Limbs().Unfolded().OfRole(TrainCarLimbRole.Crane).ToList();
            Assert.AreEqual(1, limbs.Count);
            Assert.IsTrue(limbs[0].IsUnfolded);
        }
        finally { Object.DestroyImmediate(car.gameObject); }
    }

    [Test]
    public void Close_ParkVehicle_AddsToBay()
    {
        var car = NewCar("flat");
        var cargo = new GameObject("truck").AddComponent<VehicleRagdoll>();
        cargo.vehicleId = "truck_1";
        try
        {
            Assert.IsTrue(car.TryParkVehicle(cargo, "deck"));
            Assert.AreEqual(1, car.Resultants.Vehicles().Parked().OfKind("truck").ToList().Count);
            Assert.IsTrue(car.TryUnloadVehicle(cargo, "deck"));
            Assert.AreEqual(0, car.FindBay("deck").containedVehicles.Count);
        }
        finally
        {
            Object.DestroyImmediate(car.gameObject);
            Object.DestroyImmediate(cargo.gameObject);
        }
    }

    [Test]
    public void FoldFailureBranch_MarksFailed()
    {
        var host = new GameObject("bt");
        var tree = host.AddComponent<BehaviorTree>();
        var car = NewCar("car");
        car.transform.SetParent(host.transform);
        var fail = host.AddComponent<TrainCarFoldFailureBranchNode>();
        var unfold = host.AddComponent<TrainCarUnfoldPlanNode>();
        unfold.car = car;
        unfold.simulateFailure = true;
        unfold.durationSec = 0f;
        fail.car = car;
        fail.children = new List<BehaviorTreeNode> { unfold };
        try
        {
            fail.OnEnter(tree);
            var st = fail.Execute(tree);
            Assert.AreEqual(BehaviorTreeStatus.Failure, st);
            Assert.IsTrue(car.LastFoldFailed);
        }
        finally
        {
            Object.DestroyImmediate(host);
        }
    }

    [Test]
    public void Stability_ImpossibleKeepStable_AlwaysStable()
    {
        var eval = new CargoStabilityEvaluator();
        var bake = ScriptableObject.CreateInstance<CargoStabilityBakeAsset>();
        bake.prebakedTipRisk01 = 1f;
        bake.deckPolygonXZ = new List<Vector2>
        {
            new Vector2(-0.1f, -0.1f),
            new Vector2(0.1f, -0.1f),
            new Vector2(0.1f, 0.1f),
            new Vector2(-0.1f, 0.1f)
        };
        var deck = new GameObject("deck").transform;
        try
        {
            bool ok = eval.Evaluate(
                CargoStabilityMode.ImpossibleKeepStable,
                null,
                bake,
                deck,
                deck.TransformPoint(new Vector3(10f, 0f, 10f)),
                new Vector3(50f, 0f, 0f),
                1f);
            Assert.IsTrue(ok);
            Assert.AreEqual(1f, eval.LastLashStable01, 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(bake);
            Object.DestroyImmediate(deck.gameObject);
        }
    }

    [Test]
    public void Stability_Nominal_TipsOutsidePolygon()
    {
        var eval = new CargoStabilityEvaluator();
        var bake = ScriptableObject.CreateInstance<CargoStabilityBakeAsset>();
        bake.deckPolygonXZ = new List<Vector2>
        {
            new Vector2(-1f, -1f),
            new Vector2(1f, -1f),
            new Vector2(1f, 1f),
            new Vector2(-1f, 1f)
        };
        var profile = ScriptableObject.CreateInstance<CargoLashProfile>();
        profile.tipUnstable01 = 0.5f;
        var deck = new GameObject("deck").transform;
        try
        {
            bool ok = eval.Evaluate(
                CargoStabilityMode.Nominal,
                profile,
                bake,
                deck,
                deck.TransformPoint(new Vector3(5f, 0f, 5f)),
                Vector3.zero,
                0f);
            Assert.IsFalse(ok);
        }
        finally
        {
            Object.DestroyImmediate(bake);
            Object.DestroyImmediate(profile);
            Object.DestroyImmediate(deck.gameObject);
        }
    }

    [Test]
    public void SiloAndDepot_SwapAndBulk_Ops()
    {
        var stationGo = new GameObject("station");
        var consist = stationGo.AddComponent<TrainConsistRuntime>();
        var car = NewCar("hopper");
        car.transform.SetParent(stationGo.transform);
        consist.AddCar(car);
        car.containmentBays[0].kind = TrainCarBayKind.BulkCommodity;
        var silo = stationGo.AddComponent<GrainSiloStubRuntime>();
        silo.activeCar = car;
        silo.siloQuantity = 200f;
        var depot = stationGo.AddComponent<RailMaintenanceDepotStub>();
        depot.activeConsist = consist;
        depot.activeCar = car;
        var replacement = NewCar("shop_car");
        try
        {
            Assert.IsTrue(silo.LoadIntoCar(car, "deck", 40f));
            Assert.AreEqual(160f, silo.siloQuantity, 1e-3f);
            Assert.Greater(car.FindBay("deck").bulkQuantity, 0f);
            Assert.IsTrue(depot.PullCarIntoShop(car));
            Assert.IsTrue(depot.carsInShop.Contains(car));
            Assert.IsTrue(depot.Relash(car));
            Assert.IsTrue(depot.ReinsertCar(car, 0));
            Assert.IsTrue(consist.cars.Contains(car));
            Assert.IsTrue(depot.SwapCar(0, replacement));
            Assert.AreEqual(replacement, consist.cars[0]);
        }
        finally
        {
            Object.DestroyImmediate(stationGo);
            if (replacement != null)
                Object.DestroyImmediate(replacement.gameObject);
        }
    }

    [Test]
    public void RailLegMode_IsAdjustable_AndSnakeResamples()
    {
        Assert.AreEqual(PathingMode.Drive, TravelLegModeExtensions.ToPathingMode(TravelLegMode.Rail));
        var pts = new List<Vector3>
        {
            new Vector3(0, 0, 0),
            new Vector3(0, 0, 30),
            new Vector3(0, 0, 60)
        };
        var settings = new TravelAgentMultibodySettings
        {
            enableLinkedSegmentSnake = true,
            linkedSegmentSpacingM = 10f,
            linkedSegmentCarCountHint = 4
        };
        TravelMultibodyPathAdjuster.ApplyLinkedSegmentSnakeXZ(pts, settings, null);
        Assert.AreEqual(4, pts.Count);
        float d = Vector3.Distance(
            new Vector3(pts[0].x, 0, pts[0].z),
            new Vector3(pts[1].x, 0, pts[1].z));
        Assert.AreEqual(10f, d, 0.05f);
    }

    [Test]
    public void LemmaBinder_ImpossibleToken()
    {
        var car = NewCar("c");
        var binder = car.gameObject.AddComponent<TrainCarLemmaBinder>();
        binder.car = car;
        try
        {
            binder.ApplyToken("impossible_keep_stable");
            Assert.AreEqual(CargoStabilityMode.ImpossibleKeepStable, car.defaultStabilityMode);
            Assert.IsTrue(binder.QueryImpossibleKeepStable());
        }
        finally { Object.DestroyImmediate(car.gameObject); }
    }

    static TrainCarVehicleRagdoll NewCar(string id)
    {
        var go = new GameObject(id);
        var car = go.AddComponent<TrainCarVehicleRagdoll>();
        car.vehicleId = id;
        if (car.limbs.Count == 0)
            car.limbs.Add(new TrainCarAmbulationLimb { limbId = "main_crane", role = TrainCarLimbRole.Crane });
        if (car.containmentBays.Count == 0)
            car.containmentBays.Add(new TrainCarContainmentBay
            {
                bayId = "deck",
                capacity = 2,
                parkAnchor = go.transform,
                deckRoot = go.transform
            });
        if (car.coupling == null)
            car.coupling = go.GetComponent<TrainCouplingRuntime>() ?? go.AddComponent<TrainCouplingRuntime>();
        car.coupling.car = car;
        return car;
    }
}
