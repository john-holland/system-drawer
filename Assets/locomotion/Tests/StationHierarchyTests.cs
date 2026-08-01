using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class StationHierarchyTests
{
    [Test]
    public void OrderedHierarchy_ParentsBeforeChildren()
    {
        var rootGo = new GameObject("StnRoot");
        var childGo = new GameObject("StnChild");
        var regGo = new GameObject("Reg");
        try
        {
            var reg = regGo.AddComponent<StationRegistry>();
            var parent = rootGo.AddComponent<StationHierarchyNode>();
            parent.stableId = "parent";
            parent.kind = StationKind.Train;
            var child = childGo.AddComponent<StationHierarchyNode>();
            child.stableId = "child";
            child.parentStableId = "parent";
            child.kind = StationKind.Bus;
            reg.RefreshFromScene();
            var ordered = reg.OrderedHierarchy();
            Assert.GreaterOrEqual(ordered.Count, 2);
            int pi = ordered.FindIndex(n => n.stableId == "parent");
            int ci = ordered.FindIndex(n => n.stableId == "child");
            Assert.Greater(ci, pi);
        }
        finally
        {
            Object.DestroyImmediate(rootGo);
            Object.DestroyImmediate(childGo);
            Object.DestroyImmediate(regGo);
        }
    }

    [Test]
    public void BuildLevelStats_CountsKindsAndCommodities()
    {
        var go = new GameObject("Desk");
        var regGo = new GameObject("Reg");
        try
        {
            var reg = regGo.AddComponent<StationRegistry>();
            var node = go.AddComponent<StationHierarchyNode>();
            node.stableId = "desk-1";
            node.kind = StationKind.Computer;
            node.config = new StationConfig
            {
                staffingWeight = 2f,
                commodities = new List<StationCommodityEntry>
                {
                    new StationCommodityEntry { commodityKey = "power", quantity = 4f }
                },
                assignments = new List<StationAssignmentEntry>
                {
                    new StationAssignmentEntry { assignType = "persona", refId = "clerk", role = "ops" }
                }
            };
            reg.RefreshFromScene();
            var stats = reg.BuildLevelStatsPayload();
            Assert.AreEqual(1, stats["stationCount"]);
            var byKind = stats["countsByKind"] as Dictionary<string, int>;
            Assert.IsNotNull(byKind);
            Assert.AreEqual(1, byKind["computer"]);
            Assert.AreEqual(4f, (float)stats["commodityQuantityTotal"], 0.01f);
            Assert.AreEqual(1, stats["assignmentCount"]);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(regGo);
        }
    }

    [Test]
    public void Cooking_BridgesCivilVenue()
    {
        var pdmGo = new GameObject("PDM");
        var cookGo = new GameObject("Cook");
        try
        {
            var pdm = pdmGo.AddComponent<PersonaDayManager>();
            pdm.tickIntervalSeconds = 999f;
            cookGo.AddComponent<RestaurantVenueRuntime>();
            var node = cookGo.AddComponent<StationHierarchyNode>();
            node.stableId = "cook-bridge";
            node.kind = StationKind.Cooking;
            node.TryBridge();
            Assert.IsNotNull(pdm.lattice.Get("cook-bridge"));
            Assert.AreEqual(CivilSystemKind.Kitchen, pdm.lattice.Get("cook-bridge").kind);
        }
        finally
        {
            Object.DestroyImmediate(pdmGo);
            Object.DestroyImmediate(cookGo);
        }
    }

    [Test]
    public void KindToApi_Lowercase()
    {
        Assert.AreEqual("cooking", StationHierarchyNode.KindToApi(StationKind.Cooking));
        Assert.AreEqual("computer", StationHierarchyNode.KindToApi(StationKind.Computer));
    }

    [Test]
    public void ToPlacardDto_IncludesAssignments()
    {
        var go = new GameObject("P");
        try
        {
            var n = go.AddComponent<StationHierarchyNode>();
            n.stableId = "p1";
            n.displayName = "Platform";
            n.kind = StationKind.Train;
            n.config.vehicleId = "train-9";
            n.config.assignments.Add(new StationAssignmentEntry
            {
                assignType = "vehicle",
                refId = "train-9",
                role = "operator",
                peckingOrder = 5
            });
            var dto = n.ToPlacardDto();
            Assert.AreEqual("train", dto["kind"]);
            Assert.AreEqual("train-9", dto["vehicleId"]);
            var assigns = dto["assignments"] as List<object>;
            Assert.IsNotNull(assigns);
            Assert.AreEqual(1, assigns.Count);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
