using System.Collections.Generic;
using NUnit.Framework;
using SdfMax;
using UnityEngine;

public sealed class FlowerPlantTests
{
    [Test]
    public void FeatureBudgetIds_FlowersRegistered()
    {
        Assert.AreEqual("flowers", FeatureBudgetIds.Flowers);
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.Flowers));
    }

    [Test]
    public void FruitMap_IdentityUnlessCherryOrPear()
    {
        var map = ScriptableObject.CreateInstance<FlowerFruitMapping>();
        try
        {
            Assert.AreEqual("daisy", map.Resolve("daisy"));
            map.AddCherryPearDefaults();
            Assert.AreEqual("cherry", map.Resolve("cherry_blossom"));
            Assert.AreEqual("pear", map.Resolve("pear_blossom"));
            Assert.AreEqual("daisy", map.Resolve("daisy"));
        }
        finally
        {
            Object.DestroyImmediate(map);
        }
    }

    [Test]
    public void PieceTween_EndpointsMatchCentroids()
    {
        var map = new FlowerPieceMap
        {
            pieceId = "petal",
            fromCentroid = Vector3.zero,
            toCentroid = Vector3.up,
            tween = AnimationCurve.Linear(0f, 0f, 1f, 1f)
        };
        Assert.AreEqual(Vector3.zero, FlowerPieceMap.Evaluate(map, 0f));
        Assert.AreEqual(Vector3.up, FlowerPieceMap.Evaluate(map, 1f));
    }

    [Test]
    public void Diamond_CompositionScalesWaterOnly()
    {
        var go = new GameObject("flower");
        try
        {
            var agent = go.AddComponent<FlowerTravelAgent>();
            Assert.AreEqual(4, FlowerTravelAgent.DiamondAxes.Length);
            Assert.AreEqual(12, agent.steps.Count);
            var step = agent.SelectedStep;
            step.water01 = 0.8f;
            step.optimalWater01 = 0.7f;
            agent.compositionWaterScale01 = 0.5f;
            var white = agent.DashedWhiteActive01();
            var blue = agent.BlueOptimal01();
            Assert.AreEqual(4, white.Length);
            Assert.AreEqual(4, blue.Length);
            Assert.AreEqual(0.4f, white[0], 1e-4f);
            Assert.AreEqual(0.7f, blue[0], 1e-4f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Pollination_RisesWithForeignMass_ZeroOutsideCone()
    {
        float inside = FlowerPollination.Chance(0.2f, 0.5f, true, 1f);
        float more = FlowerPollination.Chance(0.8f, 0.5f, true, 1f);
        float outside = FlowerPollination.Chance(0.8f, 0.5f, false, 1f);
        Assert.Greater(more, inside);
        Assert.AreEqual(0f, outside, 1e-4f);

        Vector3 stigma = Vector3.zero;
        Vector3 forward = Vector3.forward;
        Assert.IsTrue(FlowerPollination.InsideIntakeCone(stigma, forward, new Vector3(0f, 0f, 0.02f), 48f, 0.05f));
        Assert.IsFalse(FlowerPollination.InsideIntakeCone(stigma, forward, new Vector3(0f, 0f, 0.2f), 48f, 0.05f));
    }

    [Test]
    public void TailoredBinding_CentroidSplitTackAndSeam()
    {
        var bolt = ScriptableObject.CreateInstance<ClothBoltSpec>();
        try
        {
            var left = bolt.AddPath(ClothSplineKind.Cut);
            left.controlPoints.Add(new Vector3(-1f, 0f, 0f));
            var right = bolt.AddPath(ClothSplineKind.Cut);
            right.controlPoints.Add(new Vector3(1f, 0f, 0f));

            var tack = bolt.AddPath(ClothSplineKind.Stitch);
            tack.controlPoints.Add(new Vector3(0f, 0.2f, 0f));
            tack.program = new SewingStitchProgram();
            tack.program.steps.Add(new SewingNeedleStep
            {
                phase = SewingNeedlePhase.Entry,
                connectingStrand = true
            });
            tack.program.steps.Add(new SewingNeedleStep
            {
                phase = SewingNeedlePhase.Exit,
                connectingStrand = true
            });

            var seam = bolt.AddPath(ClothSplineKind.Hem);
            seam.controlPoints.Add(Vector3.zero);
            seam.controlPoints.Add(new Vector3(0f, 0f, 1f));

            var organs = TailoredFlowerMapping.FromBolt(bolt, Vector3.zero);
            Assert.IsTrue(HasRoleAt(organs, TailoredOrganRole.Receptacle, Vector3.zero));
            Assert.IsTrue(HasRoleAt(organs, TailoredOrganRole.Sepal, Vector3.zero));
            Assert.IsTrue(HasRoleAt(organs, TailoredOrganRole.Stigma, new Vector3(-1f, 0f, 0f)));
            Assert.IsTrue(HasRoleAt(organs, TailoredOrganRole.Stigma, new Vector3(1f, 0f, 0f)));
            Assert.IsTrue(HasRoleAt(organs, TailoredOrganRole.Ovule, new Vector3(0f, 0.2f, 0f)));
            TailoredFlowerOrgan petal = null;
            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i].role == TailoredOrganRole.Petal)
                    petal = organs[i];
            }
            Assert.IsNotNull(petal);
            Assert.Greater(Vector3.Dot(petal.planeNormal, Vector3.forward), 0.99f);
        }
        finally
        {
            Object.DestroyImmediate(bolt);
        }
    }

    [Test]
    public void SealedCloth_YieldExcess_PaintsAndTracksFragment()
    {
        var go = new GameObject("petal");
        try
        {
            var skin = go.AddComponent<SdfSealedClothSkin>();
            skin.material = new DelicateClothlikeMaterial
            {
                yieldPressure = 0.2f,
                muddleDistance = 0.1f,
                paintYieldPerMeter = 0.01f
            };
            bool tore = skin.ApplyMuddle(1f, 0.5f, Vector3.zero, Vector3.up);
            Assert.IsTrue(tore);
            Assert.Greater(skin.lastPaintAmount, 0f);
            Assert.GreaterOrEqual(skin.fragments.Count, 1);
            Assert.IsTrue(skin.fragments[0].broken);
            Assert.GreaterOrEqual(skin.fragments[0].curve.Length, 3);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void RootBake_TwoSplinesUnion_ScoopAddsSoilNodes()
    {
        var roots = new List<PlantRootDef>
        {
            new PlantRootDef { localTip = new Vector3(-0.4f, -0.8f, 0.1f), radius = 0.04f },
            new PlantRootDef { localTip = new Vector3(0.5f, -0.6f, -0.2f), radius = 0.05f }
        };
        var expression = PlantRootSystemBake.BuildRootExpression(roots);
        var go = new GameObject("root_soil");
        try
        {
            Assert.AreEqual(SdfMaxOp.Min, expression.nodes[expression.ResolveRootIndex()].op);
            int splines = 0;
            for (int i = 0; i < expression.nodes.Count; i++)
            {
                var node = expression.nodes[i];
                if (node != null && node.op == SdfMaxOp.PrimitiveLeaf &&
                    node.primitiveType == SdfPrimitiveType.SplineExtrusion)
                    splines++;
            }
            Assert.AreEqual(2, splines);

            var volume = go.AddComponent<DiggableVolume>();
            PlantRootSystemBake.ConfigureSoilVolume(volume, roots, 0.1f, null, 8, 0.08f);
            Assert.AreEqual(DiggableVolumeKind.Soil, volume.volumeKind);
            Assert.AreEqual("loam", volume.materialClass);
            int subtracts = 0;
            for (int i = 0; i < volume.sdf.nodes.Count; i++)
            {
                if (volume.sdf.nodes[i] != null && volume.sdf.nodes[i].op == SdfMaxOp.Subtract)
                    subtracts++;
            }
            Assert.Greater(subtracts, 0);
            Assert.Greater(volume.sdf.nodes.Count, 1);
        }
        finally
        {
            if (expression != null)
                Object.DestroyImmediate(expression);
            var leftover = go.GetComponent<DiggableVolume>();
            if (leftover != null && leftover.sdf != null)
                Object.DestroyImmediate(leftover.sdf);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BranchBake_LengthOriginAndRibbonPoints()
    {
        var branch = new PlantBranchDef
        {
            branchTypeLabel = "leader",
            lengthMin = 1.5f,
            lengthMax = 1.5f,
            originRotation = Quaternion.Euler(0f, 90f, 0f),
            curvePoints = new List<Vector3> { Vector3.zero, Vector3.forward }
        };
        var bake = BranchPathBake.Bake(branch, 1);
        var go = new GameObject("branch_ribbon");
        try
        {
            Assert.AreEqual(1.5f, bake.length, 0.02f);
            Assert.IsNotNull(bake.tangents);
            Assert.Greater(bake.tangents.Length, 0);
            Assert.Greater(Vector3.Dot(bake.tangents[0].normalized, Vector3.right), 0.99f);
            Assert.AreEqual(bake.positions.Length, bake.hierarchicalPlaneIds.Length);
            for (int i = 0; i < bake.hierarchicalPlaneIds.Length; i++)
                Assert.AreEqual("leader_" + i, bake.hierarchicalPlaneIds[i]);
            var ribbon = go.AddComponent<PlanarSplinePathLocomotion>();
            bake.ApplyToRibbon(ribbon);
            Assert.AreEqual(bake.positions.Length, ribbon.controlPoints.Count);
            Assert.AreEqual(bake.positions[0], ribbon.controlPoints[0]);
            Assert.AreEqual(bake.positions[bake.positions.Length - 1], ribbon.controlPoints[ribbon.controlPoints.Count - 1]);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void HarvestIk_PrebakeGrab_WritesItemCountRange()
    {
        var set = HarvestIkAnimation.CreateSet();
        Assert.AreEqual(5, set.Length);
        Assert.AreEqual(HarvestIkKind.Pluck, set[0].kind);
        Assert.AreEqual(HarvestIkKind.Pull, set[1].kind);
        Assert.AreEqual(HarvestIkKind.Weed, set[2].kind);
        Assert.AreEqual(HarvestIkKind.Pick, set[3].kind);
        Assert.AreEqual(HarvestIkKind.Pinch, set[4].kind);

        var pinch = set[4];
        pinch.save.countMin = 1;
        pinch.save.countMax = 1;
        pinch.save.itemId = "bud";
        pinch.save.itemName = "bud";
        pinch.save.partName = "bud";
        var open = pinch.Evaluate(0f);
        var closed = pinch.Evaluate(1f);
        Assert.Less(open.fingerCurl01, closed.fingerCurl01);
        Assert.AreEqual("bud", pinch.save.itemId);
        Assert.AreEqual(1, pinch.Produce(1).count);

        var branch = new PlantBranchDef
        {
            branchTypeLabel = "leader",
            lengthMin = 1f,
            lengthMax = 1f,
            grabberT01 = 1f,
            grabberRotationDeg = 40f,
            harvest = HarvestIkKind.Pluck,
            curvePoints = new List<Vector3> { Vector3.zero, Vector3.forward },
            harvestSave = new HarvestInventorySave
            {
                itemId = "cherry",
                itemName = "cherry",
                partName = "fruit",
                countMin = 2,
                countMax = 2
            }
        };
        var path = BranchPathBake.Bake(branch, 1);
        var pluck = HarvestIkAnimation.CreateDefault(HarvestIkKind.Pluck, branch.harvestSave);
        var considerGo = new GameObject("consider_grab");
        var inventoryGo = new GameObject("inventory_grab");
        try
        {
            var consider = considerGo.AddComponent<Consider>();
            var hand = new Hand { maxFingerSpread = 90f, maxGripStrength = 100f, hemisphereRadius = 0.12f };
            var prebake = consider.PrebakeGrab(path, branch, hand, pluck);
            Assert.AreEqual(1, consider.grabPrebakes.Count);
            Assert.IsTrue(prebake.canGrab);
            Assert.AreEqual(path.GrabberPoint(1f), prebake.graspPoint);
            Assert.AreEqual("cherry", prebake.animation.save.itemId);
            Assert.AreEqual(2, prebake.animation.save.countMin);
            Assert.AreEqual(2, prebake.animation.save.countMax);
            Assert.Greater(prebake.endPose.fingerCurl01, 0.9f);

            var weak = new Hand { maxFingerSpread = 5f, maxGripStrength = 1f };
            var refused = ConsiderGrabPrebake.Bake(path, branch, weak, pluck);
            Assert.IsFalse(refused.canGrab);

            var inventory = inventoryGo.AddComponent<InventoryManager>();
            var item = consider.ProduceInventory(prebake, 3, inventory);
            Assert.AreEqual("cherry", item.name);
            Assert.AreEqual(2, item.count);
            Assert.AreEqual(2, inventory.FindByName("cherry").count);
        }
        finally
        {
            Object.DestroyImmediate(considerGo);
            Object.DestroyImmediate(inventoryGo);
        }
    }

    static bool HasRoleAt(System.Collections.Generic.List<TailoredFlowerOrgan> organs, TailoredOrganRole role, Vector3 at)
    {
        for (int i = 0; i < organs.Count; i++)
        {
            var organ = organs[i];
            if (organ.role != role) continue;
            if ((organ.centroid - at).sqrMagnitude < 1e-6f)
                return true;
        }
        return false;
    }
}
