#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class LegalCourtGovernmentTests
{
    [Test]
    public void CourtKind_Kangaroo_BypassesRightsAndConstitution()
    {
        var go = new GameObject("court");
        try
        {
            var constitution = go.AddComponent<ConstitutionWarden>();
            constitution.courtKind = CourtKind.Kangaroo;
            Assert.IsTrue(constitution.Suspended());
            Assert.AreEqual(0f, constitution.Allow01(), 0.001f);

            var rights = go.AddComponent<RightsWarden>();
            rights.courtKind = CourtKind.Kangaroo;
            rights.constitutionWarden = constitution;
            Assert.IsTrue(rights.Suspended());
            Assert.AreEqual(0f, rights.Allow01(), 0.001f);
            Assert.AreEqual(1f, CourtKindCoeffs.Kangaroo01(CourtKind.Kangaroo));
            Assert.IsFalse(CourtKindCoeffs.JuryRequired(CourtKind.English));
            Assert.IsTrue(CourtKindCoeffs.Adversarial(CourtKind.American));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Junta_SuspendsConstitution()
    {
        var go = new GameObject("junta");
        try
        {
            var junta = go.AddComponent<JuntaRuntime>();
            junta.canSuspendConstitution = true;
            var constitution = go.AddComponent<ConstitutionWarden>();
            constitution.junta = junta;
            Assert.IsTrue(constitution.Suspended());
            Assert.AreEqual(0f, constitution.Allow01());
            var rights = go.AddComponent<RightsWarden>();
            rights.constitutionWarden = constitution;
            rights.junta = junta;
            Assert.IsTrue(rights.Suspended());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void GovernmentRagdoll_Mix_ParliamentaryPlusTheocracy()
    {
        var go = new GameObject("gov");
        try
        {
            var ragdoll = go.AddComponent<GovernmentModelRagdoll>();
            ragdoll.mix = new GovernmentFlavorMix
            {
                republic01 = 0.1f,
                parliamentary01 = 0.6f,
                theocracy01 = 0.5f,
                parliamentarySenateEnablesTheocracy = true
            };
            float through = ragdoll.ThroughLine01();
            Assert.Greater(through, 0.2f);
            Assert.LessOrEqual(through, 1f);
            var law = go.AddComponent<LawWarden>();
            law.governmentRagdoll = ragdoll;
            law.lawCard = ScriptableObject.CreateInstance<LawCard>();
            law.lawCard.lastScore01 = 1f;
            Assert.Greater(law.Allow01(), 0.3f);
            Object.DestroyImmediate(law.lawCard);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LawConversationCard_PaperDollSet()
    {
        var card = ScriptableObject.CreateInstance<LawConversationCard>();
        try
        {
            card.civilians.Add(ScriptableObject.CreateInstance<CivilianPaperDoll>());
            card.senators.Add(ScriptableObject.CreateInstance<SenatePersonPaperDoll>());
            card.congresspeople.Add(ScriptableObject.CreateInstance<CongressPersonPaperDoll>());
            card.parliament.Add(ScriptableObject.CreateInstance<ParliamentPersonPaperDoll>());
            card.monarchs.Add(ScriptableObject.CreateInstance<MonarchPaperDoll>());
            Assert.AreEqual(1, card.civilians.Count);
            Assert.AreEqual(1, card.senators.Count);
            Assert.AreEqual(1, card.congresspeople.Count);
            Assert.AreEqual(1, card.parliament.Count);
            Assert.AreEqual(1, card.monarchs.Count);
            Object.DestroyImmediate(card.civilians[0]);
            Object.DestroyImmediate(card.senators[0]);
            Object.DestroyImmediate(card.congresspeople[0]);
            Object.DestroyImmediate(card.parliament[0]);
            Object.DestroyImmediate(card.monarchs[0]);
        }
        finally
        {
            Object.DestroyImmediate(card);
        }
    }

    [Test]
    public void ConversationBus_NewPlusKv_AndUnassignedDiamondIsHalf()
    {
        var go = new GameObject("bus");
        try
        {
            var bus = go.AddComponent<ConversationBusTravelAgent>();
            var row = bus.AddDefaultLimit();
            Assert.IsNotNull(row);
            Assert.AreEqual(1, bus.limits.Count);
            Assert.AreEqual(0.5f, row.value01, 0.001f);
            var diamond = bus.DiamondActual01();
            Assert.AreEqual(4, diamond.Length);
            Assert.AreEqual(0.5f, diamond[0], 0.001f);
            Assert.AreEqual(0.5f, diamond[1], 0.001f);
            Assert.AreEqual(0.5f, diamond[2], 0.001f);
            Assert.AreEqual(0.5f, diamond[3], 0.001f);
            var step = bus.AddSection(ConversationSectionType.Law);
            Assert.AreEqual(ConversationSectionType.Law, step.sectionType);
            Assert.IsNotNull(step.lawConversationCard);
            var stepKv = step.AddDefaultLimit();
            Assert.IsNotNull(stepKv);
            if (step.lawCard != null)
                Object.DestroyImmediate(step.lawCard);
            if (step.lawConversationCard != null)
                Object.DestroyImmediate(step.lawConversationCard);
            if (step.conversationCard != null)
                Object.DestroyImmediate(step.conversationCard);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LawTravelAgent_StageAddRemove()
    {
        var go = new GameObject("law-ta");
        try
        {
            var agent = go.AddComponent<LawTravelAgent>();
            agent.AddStage(LawStageKind.Draft);
            agent.AddStage(LawStageKind.Filibuster);
            agent.AddStage(LawStageKind.Veto);
            Assert.AreEqual(3, agent.stages.Count);
            Assert.IsTrue(agent.RemoveStageAt(1));
            Assert.AreEqual(2, agent.stages.Count);
            Assert.AreEqual(LawStageKind.Veto, agent.stages[1].kind);
            var white = agent.DiamondWhite01();
            Assert.AreEqual(4, white.Length);
            Assert.AreEqual(0.5f, white[0], 0.001f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LegalBuilding_BootstrapRooms()
    {
        var go = new GameObject("courthouse");
        try
        {
            var stub = go.AddComponent<CivilInstitutionStub>();
            stub.kind = CivilSystemKind.CourtHouse;
            var boot = go.AddComponent<LegalBuildingBootstrap>();
            boot.stub = stub;
            boot.Ensure();
            var building = go.GetComponent<LegalBuilding>();
            Assert.IsNotNull(building);
            Assert.GreaterOrEqual(building.rooms.Count, 6);
            Assert.IsNotNull(building.FindRoom("courtroom"));
            Assert.IsNotNull(go.GetComponent<CourtWarden>());
            Assert.IsNotNull(go.GetComponent<GenevaConventionWarden>());
            Assert.IsNotNull(go.GetComponent<LegalSystemTravelAgent>());
            Assert.GreaterOrEqual(go.GetComponent<LegalSystemTravelAgent>().steps.Count, 4);
            Assert.AreEqual(CivilSystemKind.CourtHouse, CivilSystemLattice.KindFromBuildingType("courthouse"));
            Assert.AreEqual(CivilSystemKind.CourtHouse, CivilSystemLattice.KindFromBuildingType("legal"));
            var slots = BuildingRequirementSpec.DefaultSlotsFor("courthouse");
            Assert.IsTrue(slots.Exists(s => s.slotId == "judges_chambers"));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void AngleBase3D_GalleryOrientation()
    {
        var go = new GameObject("gallery");
        try
        {
            var angle = go.AddComponent<AngleBase3D>();
            angle.yawDeg = 45f;
            angle.pitchDeg = 10f;
            var q = angle.Orientation();
            Assert.AreEqual(Quaternion.Euler(10f, 45f, 0f).eulerAngles.y, q.eulerAngles.y, 0.1f);
            var seat = go.AddComponent<CourtroomSeatBt>();
            seat.RebuildAnchors();
            Assert.AreEqual(1, seat.occupantAnchors.Length);
            Assert.AreSame(go.transform, seat.occupantAnchors[0]);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void RightsWarden_ReadsConstitution_JusticeReadsRights()
    {
        var go = new GameObject("stack");
        try
        {
            var constitution = go.AddComponent<ConstitutionWarden>();
            constitution.lastScore01 = 0.8f;
            var rights = go.AddComponent<RightsWarden>();
            rights.constitutionWarden = constitution;
            Assert.AreEqual(constitution.Allow01(), rights.Allow01(), 0.001f);
            var justice = go.AddComponent<JusticeWarden>();
            justice.rightsWarden = rights;
            float j = justice.Allow01();
            Assert.Greater(j, 0f);
            Assert.LessOrEqual(j, 1f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void CourtroomLayers_ExportClusters()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        try
        {
            grid.width = 8;
            grid.height = 8;
            grid.EnsureCourtroomLayers();
            Assert.IsTrue(grid.layers.Exists(l => l != null && l.kind == CityPixelLayerKind.CourtBench));
            Assert.IsTrue(grid.layers.Exists(l => l != null && l.kind == CityPixelLayerKind.CourtGallery));
            grid.PaintLayerCell(CityPixelLayerKind.CourtBench, 0, 1, 1);
            var vols = grid.ExportCourtroomClustersToBounds4(0);
            Assert.GreaterOrEqual(vols.Count, 1);
            var catalog = ScriptableObject.CreateInstance<PixelLightMultiSlotCatalog>();
            catalog.EnsureCourtroomSlots();
            Assert.IsTrue(catalog.gridSlots.Exists(s => s != null && s.slotId == "court_gallery"));
            Object.DestroyImmediate(catalog);
        }
        finally
        {
            Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void FeatureBudget_LegalCourt_IncludesTravelAgents()
    {
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        FeatureBudgetEntry legal = null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].featureId == FeatureBudgetIds.LegalCourt) legal = entries[i];
        Assert.IsNotNull(legal);
        Assert.AreEqual(35, legal.importanceRank);
        Assert.IsTrue(System.Array.IndexOf(legal.perfScopePrefixes, "LawTravelAgent") >= 0);
        Assert.IsTrue(System.Array.IndexOf(legal.perfScopePrefixes, "ConversationBusTravelAgent") >= 0);
        Assert.IsTrue(System.Array.IndexOf(legal.perfScopePrefixes, "LegalBuilding") >= 0);
        Assert.IsTrue(System.Array.IndexOf(legal.perfScopePrefixes, "GenevaConventionWarden") >= 0);
    }

    [Test]
    public void Junta_RespectsGenevaConventions_DefaultTrue()
    {
        var go = new GameObject("junta-geneva");
        try
        {
            var junta = go.AddComponent<JuntaRuntime>();
            Assert.IsTrue(junta.respectsGenevaConventions);
            var threat = go.AddComponent<ThreatWarden>();
            var geneva = go.AddComponent<GenevaConventionWarden>();
            geneva.junta = junta;
            geneva.threatWarden = threat;
            Assert.AreEqual(1f, geneva.Allow01());
            Assert.IsFalse(geneva.lastIsTorture);

            threat.RaiseThreat(ThreatKind.Torture);
            Assert.AreEqual(0f, geneva.Allow01());
            Assert.IsTrue(geneva.lastIsTorture);

            junta.respectsGenevaConventions = false;
            threat.ClearAgency(ThreatAgencyId.Security);
            threat.lastKind = ThreatKind.Generic;
            Assert.AreEqual(0f, geneva.Allow01());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Constitution_AnnounceRightRemoved()
    {
        var go = new GameObject("const-cal");
        try
        {
            var cal = go.AddComponent<NarrativeCalendarAsset>();
            var constitution = go.AddComponent<ConstitutionWarden>();
            constitution.calendar = cal;
            constitution.constitution = ScriptableObject.CreateInstance<ConstitutionAsset>();
            constitution.constitution.articles.Add(new BillOfRightsArticle
            {
                articleId = "speech",
                enabled = true
            });
            var evt = constitution.AnnounceRightRemoved("speech");
            Assert.IsNotNull(evt);
            Assert.IsTrue(evt.tags.Contains(ConstitutionWarden.RightRemovedEvent));
            Assert.IsFalse(constitution.constitution.ArticleEnabled("speech"));
            Object.DestroyImmediate(constitution.constitution);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Constitution_AnnounceRightsReturned()
    {
        var go = new GameObject("const-rights-returned");
        try
        {
            var cal = go.AddComponent<NarrativeCalendarAsset>();
            var junta = go.AddComponent<JuntaRuntime>();
            junta.canSuspendConstitution = true;
            var constitution = go.AddComponent<ConstitutionWarden>();
            constitution.calendar = cal;
            constitution.junta = junta;
            constitution.articlesEnabled = false;
            constitution.constitution = ScriptableObject.CreateInstance<ConstitutionAsset>();
            constitution.constitution.articles.Add(new BillOfRightsArticle
            {
                articleId = "speech",
                enabled = false
            });
            constitution.constitution.articles.Add(new BillOfRightsArticle
            {
                articleId = "assembly",
                enabled = false
            });

            var one = constitution.AnnounceRightsReturned("speech");
            Assert.IsNotNull(one);
            Assert.IsTrue(one.tags.Contains(ConstitutionWarden.RightsReturnedEvent));
            Assert.IsTrue(one.tags.Contains(LegalLemmaPropertyKeys.AnnounceRightsReturned));
            Assert.IsTrue(one.tags.Contains(LegalLemmaPropertyKeys.RightsReturned));
            Assert.IsTrue(constitution.articlesEnabled);
            Assert.IsFalse(junta.canSuspendConstitution);
            Assert.IsTrue(constitution.constitution.ArticleEnabled("speech"));
            Assert.IsFalse(constitution.constitution.ArticleEnabled("assembly"));

            junta.canSuspendConstitution = true;
            var all = constitution.AnnounceRightsReturned();
            Assert.IsNotNull(all);
            Assert.IsTrue(all.tags.Contains(ConstitutionWarden.RightsReturnedEvent));
            Assert.IsTrue(constitution.constitution.ArticleEnabled("assembly"));
            Assert.IsFalse(junta.canSuspendConstitution);
            Object.DestroyImmediate(constitution.constitution);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
