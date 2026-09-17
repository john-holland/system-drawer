using NUnit.Framework;
using UnityEngine;

public sealed class ClothingTayloringTests
{
    [Test]
    public void FeatureBudgetIds_ClothingRegistered()
    {
        Assert.AreEqual("clothing", FeatureBudgetIds.Clothing);
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.Clothing));
    }

    [Test]
    public void KindFromBuildingType_ClothingStore()
    {
        Assert.AreEqual(CivilSystemKind.ClothingStore, CivilSystemLattice.KindFromBuildingType("clothing_store"));
        Assert.AreEqual(CivilSystemKind.ClothingStore, CivilSystemLattice.KindFromBuildingType("tailor_shop"));
        Assert.AreEqual(CivilSystemKind.ClothingStore, CivilSystemLattice.KindFromBuildingType("boutique"));
        Assert.AreEqual(CivilSystemKind.Factory, CivilSystemLattice.KindFromBuildingType("textile_mill"));
    }

    [Test]
    public void StorePrompt_Clothing()
    {
        Assert.IsTrue(StoreBase.DefaultPromptForStoreType("clothing_store").ToLowerInvariant().Contains("sewing"));
    }

    [Test]
    public void Diamond_OverLimit_AndBakeCompleteness()
    {
        var go = new GameObject("ta");
        try
        {
            var agent = go.AddComponent<TayloringTravelAgent>();
            agent.steps = TayloringTravelAgent.DefaultPipeline();
            Assert.AreEqual(10, agent.steps.Count);
            Assert.AreEqual(4, TayloringTravelAgent.DiamondAxes.Length);
            var s = agent.SelectedStep;
            s.tension01 = 0.2f;
            s.limitTension01 = 0.9f;
            Assert.IsFalse(agent.OverLimit());
            s.tension01 = 0.99f;
            Assert.IsTrue(agent.OverLimit());
            Assert.AreEqual(0f, agent.BakeCompleteness01(), 1e-4f);
            s.bakeComplete = true;
            Assert.Greater(agent.BakeCompleteness01(), 0f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void EmptyBoltShelf_BlocksCutCard()
    {
        var go = new GameObject("store");
        try
        {
            var ragdoll = go.AddComponent<ClothingStoreRagdoll>();
            var store = ragdoll.store ?? go.GetComponent<StoreBase>() ?? go.AddComponent<StoreBase>();
            ragdoll.store = store;
            store.storeType = "clothing_store";
            store.shelves.Clear();
            store.shelves.Add(new StoreShelfSlot
            {
                commodityKey = ClothingCommodities.Bolt,
                quantity = 0f
            });
            Assert.IsFalse(ragdoll.CanCutBolt());
            var card = TayloringCutCard.Generate(new DispatchRequest { kind = "cut" }, ragdoll);
            Assert.IsFalse(card.ShelfAllows());
            store.shelves[0].quantity = 3f;
            Assert.IsTrue(card.ShelfAllows());
            Assert.IsTrue(ragdoll.DebitBolt(1f));
            Assert.AreEqual(2f, store.ShelfQuantity(ClothingCommodities.Bolt), 1e-4f);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Bio_FacilitatesTayloringAndMillCards()
    {
        var storeGo = new GameObject("clothing");
        var millGo = new GameObject("mill");
        try
        {
            var bio = storeGo.AddComponent<TayloringBioRhythm>();
            var cut = bio.FacilitateCards(new DispatchRequest { kind = "cut" });
            Assert.IsTrue(cut.Exists(c => c is TayloringCutCard));
            var jam = bio.FacilitateCards(new DispatchRequest { kind = "jam" });
            Assert.IsTrue(jam.Exists(c => c is TayloringThreadJamCard));

            var mill = millGo.AddComponent<TextileMillBioRhythm>();
            var load = mill.FacilitateCards(new DispatchRequest { kind = "load" });
            Assert.IsTrue(load.Exists(c => c is FactoryLoadZoneCard));
            Assert.IsTrue(load.Exists(c => c is FactoryTruckUnloadCard));
            var ship = mill.FacilitateCards(new DispatchRequest { kind = "delivery" });
            Assert.IsTrue(ship.Exists(c => c is TAVehicleDeliveryCard));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(storeGo);
            UnityEngine.Object.DestroyImmediate(millGo);
        }
    }

    [Test]
    public void SvgPolyline_BecomesCutPath()
    {
        var path = ClothSvgPathParser.ToCutPath("M 0 0 L 10 0 L 10 5 Z", 1.2f, 2.4f);
        Assert.AreEqual(ClothSplineKind.Cut, path.kind);
        Assert.GreaterOrEqual(path.controlPoints.Count, 3);
        Assert.Greater(path.controlPoints[0].x, -0.01f);
    }

    [Test]
    public void Mandelbrot_AmplitudeGrowsWithIterations()
    {
        var uv = new Vector2(0.15f, 0.12f);
        float low = ClothMandelbrotScrunch.BunchAmplitude(uv, 4, 0f);
        float high = ClothMandelbrotScrunch.BunchAmplitude(uv, 48, 0f);
        Assert.Greater(high, low);
        var pinPath = new ClothSplinePath();
        pinPath.grabbers.Add(new ClothGrabberPin { uv = uv, weight01 = 1f });
        Assert.AreEqual(1f, ClothMandelbrotScrunch.NearestPinWeight(uv, pinPath, 0.25f), 1e-4f);
    }

    [Test]
    public void FoldBake_ThenInvert()
    {
        var bolt = ScriptableObject.CreateInstance<ClothBoltSpec>();
        bolt.widthM = 1f;
        bolt.lengthM = 1f;
        var step = new TayloringStep { kind = TayloringStepKind.Fold };
        Assert.IsTrue(ClothFoldBake.ApplyToStep(step, bolt));
        Assert.IsTrue(step.bakeComplete);
        Assert.IsNotNull(step.bakedMesh);
        Assert.Greater(step.bakedMesh.vertexCount, 0);
        var inv = ClothFoldBake.InvertWinding(step.bakedMesh);
        Assert.IsNotNull(inv);
        UnityEngine.Object.DestroyImmediate(inv);
        UnityEngine.Object.DestroyImmediate(step.bakedMesh);
        UnityEngine.Object.DestroyImmediate(bolt);
    }

    [Test]
    public void ThreadSpool_JamAndWindRate()
    {
        var go = new GameObject("thread");
        try
        {
            var spool = go.AddComponent<ThreadSpoolDriver>();
            Assert.AreEqual(0f, spool.WindRateMps, 1e-4f);
            spool.Pull(1f);
            Assert.IsTrue(spool.IsJammed);
            spool.TieKnot();
            Assert.IsTrue(spool.knotted);
            spool.ClearJam();
            Assert.IsFalse(spool.IsJammed);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Serger_JamAndDifferential()
    {
        var spec = ScriptableObject.CreateInstance<SergerSpec>();
        spec.needlePhase01 = 0.8f;
        spec.looperPhase01 = 0.2f;
        spec.threadTension01 = 0.99f;
        spec.tensionLimit01 = 0.5f;
        Assert.IsTrue(spec.IsJammed);
        Assert.Greater(spec.DifferentialFeed(), 0f);
        UnityEngine.Object.DestroyImmediate(spec);
    }

    [Test]
    public void FiberFarm_RecognizesSpecies()
    {
        var go = new GameObject("fiber");
        var def = ScriptableObject.CreateInstance<LotGrassPlantDef>();
        try
        {
            var farm = go.AddComponent<FiberFarmTravelAgent>();
            def.speciesId = "flax";
            farm.plantDef = def;
            Assert.IsTrue(farm.IsFiberSpecies(def));
            def.speciesId = "oak";
            Assert.IsFalse(farm.IsFiberSpecies(def));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(def);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Pleats_CountFromPath()
    {
        var path = new ClothSplinePath();
        path.controlPoints.Add(Vector3.zero);
        path.controlPoints.Add(new Vector3(0.4f, 0f, 0f));
        Assert.GreaterOrEqual(ClothPleatBaker.PleatCount(ClothPleatBaker.PathLength(path), 0.08f), 2);
    }

    [Test]
    public void Needle_IsCutToolKind()
    {
        Assert.AreEqual(CutToolKind.Needle, (CutToolKind)4);
    }

    [Test]
    public void ClothingStoreBootstrap_SeedsShelves()
    {
        var go = new GameObject("boot");
        try
        {
            var stub = go.AddComponent<CivilInstitutionStub>();
            var boot = go.AddComponent<ClothingStoreBootstrap>();
            boot.Ensure();
            var ragdoll = go.GetComponent<ClothingStoreRagdoll>();
            Assert.IsNotNull(ragdoll);
            Assert.AreEqual("clothing_store", ragdoll.store.storeType);
            Assert.AreEqual(CivilSystemKind.ClothingStore, stub.kind);
            Assert.Greater(ragdoll.store.shelves.Count, 0);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LockstitchProgram_HasLeftThenRightWithEntryExit()
    {
        var program = SewingStitchProgram.DefaultLockstitch();
        Assert.AreEqual(SewingStitchClass.Lockstitch, program.stitchClass);
        Assert.AreEqual(2, program.steps.Count);
        Assert.AreEqual(SewingFeedDirection.Left, program.steps[0].direction);
        Assert.AreEqual(SewingFeedDirection.Right, program.steps[1].direction);
        Assert.AreEqual(0, program.steps[0].index);
        Assert.AreEqual(1, program.steps[1].index);
        Assert.Greater(program.steps[0].entryAngleDeg, 0f);
        Assert.Greater(program.steps[0].exitAngleDeg, 0f);
        Assert.AreEqual(ClothThreadSide.Top, program.steps[0].clothSide);
        Assert.AreEqual(ClothThreadSide.Bottom, program.steps[1].clothSide);
    }

    [Test]
    public void OverlockProgram_HasNeedleAndLooperConnecting()
    {
        var spec = ScriptableObject.CreateInstance<SergerSpec>();
        spec.stitchProgram = SewingStitchProgram.DefaultOverlock();
        spec.needlePhase01 = 0.8f;
        spec.looperPhase01 = 0.2f;
        Assert.AreEqual(SewingStitchClass.Overlock, spec.stitchProgram.stitchClass);
        Assert.AreEqual(3, spec.stitchProgram.steps.Count);
        Assert.IsTrue(spec.stitchProgram.steps.Exists(s => s.connectingStrand));
        Assert.Greater(spec.DifferentialFeed(), 0f);
        UnityEngine.Object.DestroyImmediate(spec);
    }

    [Test]
    public void ToRopeConfig_ScalesWithGaugeAndConnectingSpan()
    {
        var sew = ScriptableObject.CreateInstance<SewingMachineSpec>();
        sew.stitchProgram = SewingStitchProgram.DefaultLockstitch();
        var thin = sew.ToRopeConfig(0.1f);
        var thick = sew.ToRopeConfig(0.9f);
        Assert.Greater(thick.ropeRadiusM, thin.ropeRadiusM);
        Assert.AreEqual(RopeMode.Spool, thick.mode);
        var serger = ScriptableObject.CreateInstance<SergerSpec>();
        serger.stitchProgram = SewingStitchProgram.DefaultOverlock();
        var over = serger.ToRopeConfig(0.5f);
        Assert.Greater(over.totalLengthM, 0.05f);
        Assert.Greater(over.ropeRadiusM, thin.ropeRadiusM * 0.5f);
        UnityEngine.Object.DestroyImmediate(sew);
        UnityEngine.Object.DestroyImmediate(serger);
    }

    [Test]
    public void HemBake_MarksStitchStepComplete()
    {
        var bolt = ScriptableObject.CreateInstance<ClothBoltSpec>();
        bolt.widthM = 1f;
        bolt.lengthM = 1f;
        var path = bolt.AddPath(ClothSplineKind.Hem);
        path.applyMode = HemSeamApplyMode.Bake;
        path.gauge01 = 0.6f;
        var step = new TayloringStep { kind = TayloringStepKind.Stitch };
        Assert.IsTrue(ClothHemSeamBake.ApplyToStep(step, bolt, path));
        Assert.IsTrue(step.bakeComplete);
        Assert.IsNotNull(step.bakedMesh);
        UnityEngine.Object.DestroyImmediate(step.bakedMesh);
        UnityEngine.Object.DestroyImmediate(bolt);
    }

    [Test]
    public void SewingSdf_HasRoot()
    {
        var sewn = SewingMachineSdfBuiltins.BuildSewing();
        Assert.GreaterOrEqual(sewn.nodes.Count, 1);
        Assert.GreaterOrEqual(sewn.ResolveRootIndex(), 0);
        var serge = SewingMachineSdfBuiltins.BuildSerger();
        Assert.GreaterOrEqual(serge.ResolveRootIndex(), 0);
        UnityEngine.Object.DestroyImmediate(sewn);
        UnityEngine.Object.DestroyImmediate(serge);
    }

    [Test]
    public void EnsureSewingAndLatheSlots_HaveHollowsAndDoors()
    {
        var catalog = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
        try
        {
            catalog.EnsureSewingMachineSlots();
            string needleThroat = FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.NeedleThroat);
            Assert.IsNotNull(catalog.FindSlot(needleThroat));
            Assert.AreEqual(
                PixelLightGridSlotKind.HollowSubtract,
                catalog.FindSlot(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.BobbinRace)).kind);
            var bobbinDoor = catalog.FindSlot(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorBobbin));
            Assert.AreEqual(PixelLightGridSlotKind.Door, bobbinDoor.kind);
            Assert.AreEqual(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.SewingShell), bobbinDoor.frameId);
            Assert.AreEqual(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorBobbin), bobbinDoor.doorId);
            Assert.AreEqual(FrameShellInclusionLemmaPropertyKeys.HingeLeft, bobbinDoor.hingeLabel);
            Assert.AreEqual(Bounds4SdfInclusionKind.Shell, bobbinDoor.inclusion);
            Assert.AreEqual(
                SewingLemmaPropertyKeys.NeedleThroat,
                FrameShellInclusionLemmaPropertyKeys.FromSlotId(needleThroat));
            catalog.EnsureLatheSlots();
            Assert.AreEqual(
                PixelLightGridSlotKind.HollowSubtract,
                catalog.FindSlot(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.SpindleBore)).kind);
            var head = catalog.FindSlot(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.DoorHeadstock));
            Assert.AreEqual(FrameShellInclusionLemmaPropertyKeys.ToSlotId(SewingLemmaPropertyKeys.LatheShellCover), head.frameId);
            Assert.AreEqual(FrameShellInclusionLemmaPropertyKeys.HingeLeft, head.hingeLabel);
            Assert.AreEqual(Bounds4SdfInclusionKind.Frame, Bounds4SdfInclusionLemmas.FromLemma("frame-inclusion"));
            Assert.AreEqual(FrameShellInclusionLemmaPropertyKeys.ShellInclusion,
                Bounds4SdfInclusionLemmas.ToInclusionLemma(Bounds4SdfInclusionKind.Shell));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void SlotsAtCell_AndMoveSlotZ()
    {
        var catalog = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
        try
        {
            var a = catalog.AddSlot("A");
            a.cellX = 2;
            a.cellY = 2;
            var b = catalog.AddSlot("B");
            b.cellX = 2;
            b.cellY = 2;
            var at = catalog.SlotsAtCell(2, 2);
            Assert.AreEqual(2, at.Count);
            int zA = a.zIndex;
            int zB = b.zIndex;
            Assert.Greater(zB, zA);
            Assert.IsTrue(catalog.MoveSlotZ(b.slotId, -1));
            Assert.Less(b.zIndex, a.zIndex);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void StackPreview_OffsetsIncreaseWithZ_IdentityWhenOff()
    {
        var off0 = PixelLightStackPreview.CellRect(1, 2, 0, 18f, 6f, 0.2f, false);
        var off3 = PixelLightStackPreview.CellRect(1, 2, 3, 18f, 6f, 0.2f, false);
        Assert.AreEqual(off0.x, off3.x, 0.01f);
        Assert.AreEqual(off0.y, off3.y, 0.01f);
        var on0 = PixelLightStackPreview.CellRect(1, 2, 0, 18f, 6f, 0.2f, true);
        var on3 = PixelLightStackPreview.CellRect(1, 2, 3, 18f, 6f, 0.2f, true);
        Assert.Greater(on3.x, on0.x);
        Assert.Greater(on3.y, on0.y);
    }

    [Test]
    public void ThreadBinder_CopiesRopeConfig()
    {
        var dest = new RopeConfig { ropeRadiusM = 0.01f, totalLengthM = 1f };
        var src = new RopeConfig { ropeRadiusM = 0.002f, totalLengthM = 0.3f, mode = RopeMode.Spool };
        SewingThreadStrandBinder.CopyConfig(dest, src);
        Assert.AreEqual(0.002f, dest.ropeRadiusM, 1e-5f);
        Assert.AreEqual(0.3f, dest.totalLengthM, 1e-5f);
        Assert.AreEqual(RopeMode.Spool, dest.mode);
    }
}
