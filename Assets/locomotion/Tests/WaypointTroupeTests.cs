#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Locomotion.Narrative;

public sealed class WaypointTroupeTests
{
    [Test]
    public void Route_AddAndCycleFormation()
    {
        var route = new WaypointRoute { defaultFormationId = "triangle" };
        route.Add(Vector3.zero, "A", "triangle");
        route.Add(Vector3.one, "B", "triangle");
        Assert.AreEqual(2, route.Count);
        var ids = new List<string> { "triangle", "pineapple", "divide_and_conquer" };
        route.CycleFormationNext(ids);
        Assert.AreEqual("pineapple", route.Active.formationId);
        route.CycleFormationPrev(ids);
        Assert.AreEqual("triangle", route.Active.formationId);
    }

    [Test]
    public void FeatureCoeffs_DisableStuntman()
    {
        var coeffs = new TravelFeatureCoefficients { stuntman = 0.2f, safetyWarden = 1f };
        Assert.IsFalse(coeffs.AllowStuntman);
        Assert.IsTrue(coeffs.AllowSafetyWarden);
    }

    [Test]
    public void CallToArms_RespectsRange()
    {
        var root = new GameObject("Facilitator");
        var near = new GameObject("Near");
        var far = new GameObject("Far");
        near.transform.position = Vector3.zero;
        far.transform.position = Vector3.right * 100f;
        try
        {
            var fac = root.AddComponent<CombatRulesFacilitatorService>();
            fac.troupes.Add(new TroupeParameters
            {
                troupeId = "alpha",
                callToArmsRangeMeters = 10f,
                members = new List<TroupeMember>
                {
                    new TroupeMember { actor = near, guidanceMode = TravelGuidanceMode.NpcFull },
                    new TroupeMember { actor = far, guidanceMode = TravelGuidanceMode.NpcFull }
                }
            });
            int joined = fac.CallToArms("alpha", Vector3.zero);
            Assert.GreaterOrEqual(joined, 1);
            Assert.IsFalse(fac.InCommsRange(near, far, "alpha"));
            Assert.IsTrue(fac.InCommsRange(near, far, "alpha", dialogOverrideIgnoreRange: true));
        }
        finally
        {
            Object.DestroyImmediate(root);
            Object.DestroyImmediate(near);
            Object.DestroyImmediate(far);
        }
    }

    [Test]
    public void WaypointLemma_ParsesXYZ_AndVec3()
    {
        Assert.IsTrue(WaypointLemmaResolver.TryParseVec3("(1,2,3)", out var v));
        Assert.AreEqual(1f, v.x, 1e-4f);
        Assert.AreEqual(2f, v.y, 1e-4f);
        Assert.AreEqual(3f, v.z, 1e-4f);

        var route = new WaypointRoute();
        var segs = new List<PromptSegment>
        {
            new PromptSegment
            {
                isPlaceholder = true,
                placeholderName = "waypoint",
                placeholderParams = new Dictionary<string, string>
                {
                    { "name", "A" }, { "x", "1" }, { "y", "2" }, { "z", "3" }
                }
            },
            new PromptSegment
            {
                isPlaceholder = true,
                placeholderName = "waypoint",
                placeholderParams = new Dictionary<string, string>
                {
                    { "from", "A" }, { "to", "B" }, { "formation", "pineapple" }
                }
            }
        };
        WaypointLemmaResolver.Execute(route, segs);
        Assert.GreaterOrEqual(route.Count, 2);
        Assert.AreEqual(new Vector3(1, 2, 3), route.markers[0].worldPosition);
    }

    [Test]
    public void SpatialProjector_MapPins()
    {
        var go = new GameObject("Proj");
        try
        {
            var proj = go.AddComponent<WaypointSpatialProjector>();
            proj.route = new WaypointRoute();
            proj.route.Add(new Vector3(3, 0, 4), "P1");
            proj.Project();
            var pins = proj.ToMapPins();
            Assert.AreEqual(1, pins.Count);
            Assert.AreEqual(3f, pins[0].xz.x, 1e-4f);
            Assert.AreEqual(4f, pins[0].xz.y, 1e-4f);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
#endif
