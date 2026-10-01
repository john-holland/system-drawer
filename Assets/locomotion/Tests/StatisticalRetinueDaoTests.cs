#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class StatisticalRetinueDaoTests
{
    [Test]
    public void MapSample_SocietyFeatures()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var seed = StatSeed.ForCity("c1", new Dictionary<string, float> { ["healthcareCoverage"] = 0.8f });
            dao.EnsureModel("society.features", seed);
            Assert.IsTrue(dao.TryGet("society.features",
                new StatQuery { featureKey = "healthcareCoverage" }, out float v));
            Assert.AreEqual(0.8f, v, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void PersonaPin_BeatsMapSample()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var seed = StatSeed.ForCity("c1", new Dictionary<string, float> { ["morale"] = 0.4f });
            dao.EnsureModel("society.features", seed);
            dao.PutIndividual("society.features", SpecificityKey.Persona("alice"),
                StatOverride.Channel("morale", 0.9f));
            Assert.IsTrue(dao.TryGet("society.features",
                new StatQuery { featureKey = "morale", personaKey = "alice" }, out float v));
            Assert.AreEqual(0.9f, v, 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ApplyGovGloveBias_StaysInSoftBand()
    {
        var go = new GameObject("dao");
        var actor = new GameObject("actor");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var sheet = actor.AddComponent<LifeSystemsSheet>();
            sheet.EnsureDefaults();
            var bundle = PersonaRequestBundle.CreateDefault("p", CivilSystemKind.Generic);
            bundle.societyFeatures["healthcareCoverage"] = 1f;
            bundle.needSatisfied01["need_physiological"] = 1f;
            dao.ApplyGovGloveBias(sheet, bundle);
            Assert.IsTrue(LifeSystemsChannelCatalog.TryGet(LifeSystemsChannelCatalog.Immune, out var def));
            float u = sheet.Get01(def.id);
            Assert.GreaterOrEqual(u, def.softBandMin01 - 1e-3f);
            Assert.LessOrEqual(u, def.softBandMax01 + 1e-3f);
        }
        finally
        {
            Object.DestroyImmediate(actor);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void TradeEdge_GateAndStandingTick()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            dao.UpsertTradeEdge(new TradeEdge
            {
                fromRetinueId = "kitchen",
                toRetinueId = "sanitation",
                commodityKey = "food_waste",
                volumeShare01 = 0.2f,
                affinity01 = 0.8f,
                settlement = TradeSettlementMode.ContractStanding
            });
            Assert.IsTrue(dao.TryResolveTrade(new TradeDealRequest
            {
                fromRetinueId = "kitchen",
                toRetinueId = "sanitation",
                commodityKey = "food_waste"
            }, out var res));
            Assert.IsTrue(res.allowed);

            var from = dao.EnsureEconomicOverlay("kitchen", default);
            EconomicOverlay.UpsertBalance(from, "food_waste", 10f);
            dao.TickEconomy("kitchen", 1f);
            Assert.IsTrue(from.TryGetBalance("food_waste", out float left));
            Assert.Less(left, 10f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Economy_VelocityDecay()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var o = dao.EnsureEconomicOverlay("shop", default);
            o.velocity01 = 0.9f;
            dao.TickEconomy("shop", 2f);
            Assert.Less(o.velocity01, 0.9f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Chemical_StrictReject_AndPlayableFudgeLogged()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var overlay = dao.EnsureEconomicOverlay("lab", new StatSeed { retinueId = "lab" });
            EconomicOverlay.UpsertBalance(overlay, "acid", 5f);
            EconomicOverlay.UpsertBalance(overlay, "base", 5f);

            dao.SetAuditMode("chemical.trade", AuditMode.Strict);
            // Force measured far from expected by using Playable first to see trail, then Strict with tiny epsilon
            var ctx = new RuleFireContext
            {
                Dao = dao,
                fromRetinueId = "lab",
                toRetinueId = "lab",
                tempK = 298.15f,
                dtHours = 1f,
                lodScale = 1f
            };
            // Playable always commits and logs
            dao.SetAuditMode("chemical.trade", AuditMode.Playable);
            Assert.IsTrue(dao.TryFireRule("chemical.trade", "acid_base_neutralize", ctx, out var playable));
            Assert.Greater(playable.committed, 0f);
            var trail = dao.GetAuditTrail("chemical.trade", 8);
            Assert.Greater(trail.Count, 0);
            Assert.AreEqual(playable.measured, trail[trail.Count - 1].measured, 0.001f);

            // Top up reagents and Strict with reject band via correction unit test
            float rejected = dao.ApplyAuditCorrection("chemical.trade", "audit_reject_band", 1f, 0f,
                new AuditCorrectionParams { epsilon = 0.05f });
            Assert.IsTrue(float.IsNaN(rejected));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Culture_MonodTick_AndFungusEmpower()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var bact = dao.EnsureCulture("petri-1", CultureKind.Bacterial,
                new StatSeed { retinueId = "lab", cultureSpeciesId = "e_coli" });
            bact.biomass01 = 0.1f;
            EconomicOverlay.UpsertBalance(dao.EnsureEconomicOverlay("lab", default), "media", 20f);
            dao.TickCulture("petri-1", 1f);
            Assert.Greater(bact.biomass01, 0.1f);

            var fungus = dao.EnsureCulture("bed-1", CultureKind.Fungal,
                new StatSeed { retinueId = "lab", cultureSpeciesId = "oyster" });
            fungus.biomass01 = 0.2f;
            EconomicOverlay.UpsertBalance(dao.EnsureEconomicOverlay("lab", default), "substrate", 20f);
            dao.ArmGrowthEvent(GrowthEvent.EmpowerFungus("fungus_bloom", 2f, 3f, "oyster"));
            Assert.IsTrue(dao.TryFireGrowthEvent("fungus_bloom", out var gr));
            Assert.Greater(gr.culturesTouched, 0);
            Assert.Greater(fungus.empowerMult, 1f);
            float before = fungus.biomass01;
            dao.TickCulture("bed-1", 1f);
            Assert.GreaterOrEqual(fungus.biomass01, before);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void StatLemma_SampleAndTradePut()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            dao.EnsureModel("society.features",
                StatSeed.ForCity("c", new Dictionary<string, float> { ["water"] = 0.7f }));
            string r = StatLemmaResolver.ExecuteFromScript(
                "{P:stat|op=sample|model=society.features|key=water}", dao);
            Assert.IsTrue(r.Contains("0.7"));
            StatLemmaResolver.ExecuteFromScript(
                "{P:stat|op=put|model=trade.demographics|from=a|to=b|commodity=gas|share=0.2}", dao);
            Assert.IsTrue(dao.TryResolveTrade(new TradeDealRequest
            {
                fromRetinueId = "a",
                toRetinueId = "b",
                commodityKey = "gas"
            }, out var deal));
            Assert.IsTrue(deal.allowed);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void CareerWarden_UsesDaoDemographics()
    {
        var go = new GameObject("warden");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var warden = go.AddComponent<CareerWarden>();
            warden.ApplySocietyFeatures(new Dictionary<string, float> { ["unemploymentRate"] = 0.12f }, 50);
            Assert.AreEqual(0.12f, warden.demographics.unemploymentRate01, 0.001f);
            Assert.IsNotNull(dao.GetModel("civ.demographics"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SpatialFalloff_NearAnchorStrongerThanFar()
    {
        float near = StatCurveCatalog.Get("spatial_falloff")(0.1f, default);
        float far = StatCurveCatalog.Get("spatial_falloff")(3f, default);
        Assert.Greater(near, far);
        Assert.AreEqual(1f / (1f + 0.01f), near, 0.001f);
    }

    [Test]
    public void SpatialSampleBounds_LookupDiffersFromSpawn_QuotaUnchanged()
    {
        var demo = new CivilianDemographics
        {
            cityPopulation = 100,
            unemploymentRate01 = 0.1f,
            spatialGradient = new DemographicSpatialGradient
            {
                strength01 = 1f,
                anchors = new List<DemographicSpatialAnchor>
                {
                    new DemographicSpatialAnchor
                    {
                        worldPos = new Vector3(100, 0, 0),
                        radiusM = 50f,
                        unemploymentDelta = 0.3f,
                        ageChildDelta = 0.4f
                    }
                },
                sampleBounds = new List<DemographicSpatialSampleBound>
                {
                    new DemographicSpatialSampleBound
                    {
                        center = new Vector3(100, 0, 0),
                        halfExtents = new Vector3(5, 1, 5)
                    }
                }
            }
        };
        int quota = demo.UnemployedQuota;
        Vector3 spawn = Vector3.zero;
        Vector3 samplePos = demo.spatialGradient.SamplePosForDemographics(spawn, 42);
        Assert.Greater(Vector3.Distance(samplePos, spawn), 50f);
        var resolved = demo.ResolveAt(samplePos);
        Assert.Greater(resolved.unemploymentRate01, demo.unemploymentRate01);
        Assert.AreEqual(quota, demo.UnemployedQuota);
        var doll = demo.SampleUnemployed("civ", new List<CivilianPaperDoll>(), 7, spawn);
        Assert.IsNotNull(doll);
        Object.DestroyImmediate(doll);
    }

    [Test]
    public void CellularRetinue_SampleRespectsSpatialAllergy()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var ret = dao.EnsureCellularRetinue("host", "tissue", default);
            ret.demographics.spatialGradient = new DemographicSpatialGradient
            {
                strength01 = 1f,
                anchors = new List<DemographicSpatialAnchor>
                {
                    new DemographicSpatialAnchor
                    {
                        worldPos = Vector3.zero,
                        radiusM = 20f,
                        allergyPrevalenceDelta = 0.5f,
                        sensorShareDelta = 0.3f
                    }
                }
            };
            var member = dao.SampleCellularMember("host", "mast", 11, Vector3.zero);
            Assert.IsNotNull(member);
            var local = ret.demographics.ResolveAt(Vector3.zero);
            Assert.Greater(local.allergyPrevalence01, 0.12f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void AllergyAvoid_DeniesTrade()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            dao.UpsertTradeEdge(new TradeEdge
            {
                fromRetinueId = "cell.sensor",
                toRetinueId = "allergen.toad",
                commodityKey = "toad",
                relationKind = TradeRelationKind.AllergyAvoid,
                affinity01 = 1f
            });
            bool ok = dao.TryResolveTrade(new TradeDealRequest
            {
                fromRetinueId = "cell.sensor",
                toRetinueId = "allergen.toad",
                commodityKey = "toad"
            }, out var res);
            Assert.IsFalse(ok);
            Assert.IsTrue(res.reason.IndexOf("allergy", System.StringComparison.OrdinalIgnoreCase) >= 0);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ChemicalBond_GlueCureRuleFires()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            var overlay = dao.EnsureEconomicOverlay("lab", default);
            EconomicOverlay.UpsertBalance(overlay, "adhesive", 5f);
            EconomicOverlay.UpsertBalance(overlay, "substrate", 5f);
            bool fired = dao.TryFireRule("chemical.trade", "glue_cure",
                new RuleFireContext { Dao = dao, fromRetinueId = "lab", toRetinueId = "lab", tempK = 298f, dtHours = 1f },
                out var result);
            Assert.IsTrue(fired);
            Assert.AreEqual("glue_cure", result.ruleId);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ManifoldLerp_ColdAdhesionGate()
    {
        var sel = new PhysicsManifoldLerpGradientSelector
        {
            coldStickC = -5f,
            strength01 = 1f,
            bands = new List<ManifoldLerpBand>
            {
                new ManifoldLerpBand
                {
                    channel = ManifoldSelectChannel.Temperature,
                    sampleA = 20f,
                    sampleB = -20f,
                    weightAtA = 0f,
                    weightAtB = 1f
                }
            }
        };
        // Without manifold, SampleAt returns 20C ambient — gate low
        float warmGate = sel.EvaluateAdhesionGate(Vector3.zero);
        Assert.Less(warmGate, 0.5f);
        float curve = StatCurveCatalog.Get("manifold_temp_lerp")(-10f,
            new StatCurveParams { softMin = -20f, softMax = 10f });
        Assert.Greater(curve, 0.4f);
    }

    [Test]
    public void HealthInpaint_RespectsDisablePartSpecific()
    {
        var actor = new GameObject("actor");
        try
        {
            var settings = actor.AddComponent<ActorHealthInpaintSettings>();
            settings.disablePartSpecificHealthInpainting = true;
            var req = ActorHealthInpaintBridge.Request(new HealthInpaintRequest
            {
                actor = actor,
                kind = HealthInpaintKind.Hives,
                partId = "LeftForearm",
                allergenOrBondKey = "toad",
                intensity01 = 0.6f
            });
            Assert.IsNotNull(req);
            Assert.IsFalse(req.partSpecific);
            Assert.IsTrue(string.IsNullOrEmpty(req.partId));

            settings.disablePartSpecificHealthInpainting = false;
            var req2 = ActorHealthInpaintBridge.Request(new HealthInpaintRequest
            {
                actor = actor,
                kind = HealthInpaintKind.Hives,
                partId = "LeftForearm",
                allergenOrBondKey = "toad"
            });
            Assert.IsTrue(req2.partSpecific);
            Assert.AreEqual("LeftForearm", req2.partId);
        }
        finally
        {
            Object.DestroyImmediate(actor);
        }
    }

    [Test]
    public void HealthInpaintEventCatalog_HasTongueStuckDefault()
    {
        var catalog = HealthInpaintEventCatalog.CreateDefaultRuntime();
        Assert.IsTrue(catalog.TryGet("tongue_stuck_flagpole", out var spec));
        Assert.AreEqual("Tongue", spec.defaultPartId);
        Assert.IsNotNull(catalog.MatchRule("tongue_freeze_bond"));
        var runnerGo = new GameObject("runner");
        try
        {
            var runner = runnerGo.AddComponent<HealthInpaintEventRunner>();
            runner.catalog = catalog;
            var fired = runner.Fire("hives_from_toad", null, "LeftForearm", "toad", "wetland");
            Assert.IsNotNull(fired);
            Assert.AreEqual(HealthInpaintKind.Hives, fired.kind);
            Assert.IsFalse(string.IsNullOrEmpty(runner.lastPrompt));
        }
        finally
        {
            Object.DestroyImmediate(runnerGo);
            Object.DestroyImmediate(catalog);
        }
    }

    [Test]
    public void Lemma_SpatialSampleAndAllergyPut()
    {
        var go = new GameObject("dao");
        try
        {
            var dao = go.AddComponent<StatisticalRetinueDao>();
            string put = StatLemmaResolver.Execute(new StatLemmaProperties
            {
                op = StatLemmaOp.Put,
                model = "trade.demographics",
                relation = "allergy_avoid",
                from = "cell.sensor",
                to = "allergen.pollen",
                commodity = "pollen"
            }, dao);
            Assert.IsTrue(put.IndexOf("AllergyAvoid", System.StringComparison.OrdinalIgnoreCase) >= 0);
            string health = StatLemmaResolver.Execute(new StatLemmaProperties
            {
                op = StatLemmaOp.HealthInpaint,
                kind = "Hives",
                part = "LeftForearm",
                allergen = "toad"
            }, dao);
            Assert.IsTrue(health.StartsWith("health_inpaint"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
