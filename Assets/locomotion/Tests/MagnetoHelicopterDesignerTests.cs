#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MagnetoHelicopterDesignerTests
{
    [Test]
    public void TipCache_Validate_AfterRecompute()
    {
        var root = new GameObject("m");
        try
        {
            var m = new MagnetoLiftParams { spanLength = 10f };
            m.RecomputeTipEndCache(root.transform);
            Assert.IsTrue(m.ValidateTipCache(root.transform, 0.01f));
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void Requirements_Apply_WritesMins_WithoutSilentEditBeforeApply()
    {
        var props = new MagnetoLiftParams { spanLength = 4f, rpmMax = 200f };
        var req = new MagnetoLiftRequirements { minLiftN = 50000f, minClimbMs = 3f };
        float before = props.spanLength;
        Assert.IsFalse(req.SatisfiedBy(props));
        Assert.AreEqual(before, props.spanLength);
        req.ApplyMinimumsTo(props);
        Assert.Greater(props.spanLength, before);
        Assert.Greater(props.lastAppliedMinLiftN, 0f);
        props.spanLength *= 0.5f;
        props.RefreshEfficacyFromLastApplied();
        Assert.IsTrue(props.IsEfficacyLowered());
    }

    [Test]
    public void GridSlot_PlaceMagneto_AndTelecomGps()
    {
        var go = new GameObject("heli");
        try
        {
            var heli = go.AddComponent<HelicopterVehicleRagdoll>();
            var slot = go.AddComponent<HelicoptorGridSlotGameObject>();
            slot.helicopter = heli;
            slot.PlaceMagneto(new MagnetoLiftParams { magnetoId = "main" });
            Assert.AreEqual(HelicoptorGridSlotGameObject.SlotContents.Magneto, slot.contents);
            Assert.Greater(heli.magnetos.Count, 0);
            slot.PlaceTelecomGpsWebtop();
            Assert.AreEqual(HelicoptorGridSlotGameObject.SlotContents.TelecomGpsWebtop, slot.contents);
            Assert.IsNotNull(heli.gpsWebtopMount);
            Assert.IsNotNull(heli.gpsHud);
            Assert.IsNotNull(heli.renderPortal);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void PortalBounds2_UpdatesOverlayRect()
    {
        var go = new GameObject("portal");
        try
        {
            var portal = go.AddComponent<UnityRenderPortal>();
            portal.portalId = "gps";
            portal.ApplyBounds(new UnityRenderPortal.PortalBounds2Entry
            {
                portalId = "gps",
                x = 10, y = 20, width = 640, height = 360,
                nx = 0.1f, ny = 0.2f, nw = 0.5f, nh = 0.4f
            });
            Assert.IsTrue(portal.hasBounds);
            Assert.AreEqual(0.5f, portal.lastBoundsNormalized.width, 0.001f);
            Assert.IsNotNull(portal.overlayRenderer);
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void RoadLot_WallSections_SumMustBeOne()
    {
        var go = new GameObject("lot");
        try
        {
            var boundary = go.AddComponent<RoadLotBoundarySpline>();
            boundary.EnsureClosedLoopDefault();
            Assert.IsTrue(boundary.TryValidateWallSections(out _));
            boundary.wallSections.Clear();
            boundary.wallSections.Add(new RoadLotWallSection { startT01 = 0f, endT01 = 0.4f });
            boundary.wallSections.Add(new RoadLotWallSection { startT01 = 0.4f, endT01 = 0.7f });
            Assert.IsFalse(boundary.TryValidateWallSections(out string err));
            Assert.IsNotNull(err);
            Assert.Throws<InvalidOperationException>(() => boundary.ValidateWallSections());
            boundary.wallSections[1].endT01 = 1f;
            Assert.IsTrue(boundary.TryValidateWallSections(out _));
            boundary.SplitAt(0.5f);
            Assert.IsTrue(boundary.TryValidateWallSections(out _));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void RoadLot_ZeroAndManyOutlets_FindConnected()
    {
        var a = new GameObject("lotA");
        var b = new GameObject("lotB");
        try
        {
            a.SetActive(false);
            b.SetActive(false);
            var lotA = a.AddComponent<RoadLot>();
            var lotB = b.AddComponent<RoadLot>();
            lotA.lotId = "a";
            lotB.lotId = "b";
            lotB.roadOutlets.Add(new RoadLotOutlet { roadSegmentId = "road_1" });
            a.SetActive(true);
            b.SetActive(true);
            Assert.AreEqual(0, lotA.roadOutlets.Count);
            Assert.AreEqual(lotB, RoadLot.FindConnectedToRoad("road_1", Vector3.zero));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(a);
            UnityEngine.Object.DestroyImmediate(b);
        }
    }

    [Test]
    public void Grass_CutSeverity_BlocksAndForgetAbove()
    {
        var go = new GameObject("grass");
        try
        {
            var grass = go.AddComponent<LotGrassGrowthController>();
            grass.plantDef = ScriptableObject.CreateInstance<LotGrassPlantDef>();
            grass.plantDef.grownStages.Add(new LotGrassGrowthStage { stageId = "s0" });
            grass.plantDef.grownStages.Add(new LotGrassGrowthStage { stageId = "s1" });
            grass.sectionParent.Add(-1);
            grass.sectionParent.Add(0);
            grass.stageIndex = 0;
            grass.ApplyCut(Vector3.up, 0.5f, 0f, 1);
            Assert.AreEqual(0, grass.cuts.Count);
            grass.ApplyCut(Vector3.up, 1f, 1f, 1);
            Assert.Greater(grass.cuts.Count, 0);
            Assert.IsFalse(grass.TryAdvanceStage());
            grass.cuts.Clear();
            grass.nextSectionSpawnChance = 1f;
            grass.ApplyCut(Vector3.up, 0.2f, 0.3f, 1);
            grass.cuts.Add(new LotGrassCutMemory { sectionId = 5, severity01 = 0.2f });
            grass.ForgetCutsAbove(0);
            Assert.IsTrue(grass.cuts.Exists(c => c.sectionId == 5 && c.forgotten));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void HelicopterSolver_AndRouteMerge_Idempotent()
    {
        var root = new GameObject("heli_leg");
        try
        {
            var heli = root.AddComponent<HelicopterVehicleRagdoll>();
            heli.magnetos.Add(new MagnetoLiftParams { spanLength = 12f });
            var solved = HelicopterDirectionSolver.Solve(heli, Vector3.zero, Vector3.forward * 50f + Vector3.up * 10f);
            Assert.Greater(solved.efficacy01, 0f);
            var leg = root.AddComponent<TravelLegSequenceNode>();
            leg.children = new List<BehaviorTreeNode>();
            var seg = new MultiModalSegment { mode = TravelLegMode.Fly };
            HelicopterTravelRouteMerger.MergeIntoLeg(leg, heli, seg);
            HelicopterTravelRouteMerger.MergeIntoLeg(leg, heli, seg);
            int takeoffs = 0, landings = 0;
            for (int i = 0; i < leg.children.Count; i++)
            {
                if (leg.children[i] is HelicopterTakeoffPlanNode) takeoffs++;
                if (leg.children[i] is HelicopterLandingPlanNode) landings++;
            }
            Assert.AreEqual(1, takeoffs);
            Assert.AreEqual(1, landings);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    [Test]
    public void GpsBake_ProducesWaypoints_AndLemmaKeys()
    {
        var go = new GameObject("ta");
        try
        {
            var ta = go.AddComponent<TravelAgent>();
            ta.previewStartWorld = Vector3.zero;
            ta.previewGoalWorld = Vector3.forward * 20f;
            var plan = new GenericMultiModalPathPlan();
            plan.segments = new List<MultiModalSegment>
            {
                new MultiModalSegment
                {
                    mode = TravelLegMode.Fly,
                    waypoints = new List<Vector3> { Vector3.zero, Vector3.forward * 10f, Vector3.forward * 20f }
                }
            };
            var field = typeof(TravelAgent).GetField("cachedPlan",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Public);
            Assert.IsNotNull(field);
            field.SetValue(ta, plan);
            var cache = PilotGpsHudWebtop.BakeFromTravelAgent(ta, 64, 64);
            Assert.GreaterOrEqual(cache.waypoints.Count, 2);
            Assert.IsTrue(cache.hasBounds);
            Assert.IsTrue(HelicopterLemmaPropertyKeys.IsHelicopterLemma(HelicopterLemmaPropertyKeys.GpsHud));
            Assert.IsTrue(HelicopterLemmaPropertyKeys.IsHelicopterLemma(HelicopterLemmaPropertyKeys.PortalBounds2));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); }
    }

    [Test]
    public void RoadTravelBinding_EnrichSetsRoadLotId()
    {
        var lotGo = new GameObject("lot");
        var bindGo = new GameObject("bind");
        try
        {
            lotGo.SetActive(false);
            var lot = lotGo.AddComponent<RoadLot>();
            lot.lotId = "pad_1";
            lotGo.transform.position = Vector3.forward * 5f;
            lotGo.SetActive(true);
            var binding = bindGo.AddComponent<RoadTravelBinding>();
            var seg = new MultiModalSegment
            {
                mode = TravelLegMode.Drive,
                waypoints = new List<Vector3> { Vector3.zero, Vector3.forward * 5f }
            };
            binding.EnrichDriveSegmentWithRoadLot(seg);
            Assert.AreEqual("pad_1", seg.roadLotId);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lotGo);
            UnityEngine.Object.DestroyImmediate(bindGo);
        }
    }
}
#endif
