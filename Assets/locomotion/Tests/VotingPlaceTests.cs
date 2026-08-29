#if UNITY_EDITOR
using System.Collections.Generic;
using Locomotion.Narrative;
using NUnit.Framework;
using UnityEngine;

public sealed class VotingPlaceTests
{
    [Test]
    public void LaneGrid_EnqueueAndOccupied()
    {
        var go = new GameObject("lanes");
        var actorGo = new GameObject("actor");
        try
        {
            var grid = go.AddComponent<LaneGrid>();
            grid.width = 2;
            grid.height = 2;
            grid.EnsureCells();
            var actor = actorGo.AddComponent<BaseAmbulatingActor>();
            Assert.IsTrue(grid.TryEnqueue(actor));
            Assert.AreEqual(1, grid.OccupiedCount);
            var card = new VotingPlaceCard { laneGrid = grid, developerInpaint = true };
            Assert.IsFalse(card.EnqueueVoter(actor));
            Assert.AreSame(actor, grid.TryDequeueToBooth());
            Assert.AreEqual(0, grid.OccupiedCount);
        }
        finally
        {
            Object.DestroyImmediate(actorGo);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Demographics_RenormalizeToWhole()
    {
        var d = ElectorateDemographics.DefaultTwoParty();
        Assert.AreEqual(1f, d.Whole01(), 0.001f);
        d.AddSlice("race", "example", 0.5f);
        Assert.AreEqual(1f, d.Whole01(), 0.001f);
        var fromGov = ElectorateDemographics.FromSocietyFeatures(new Dictionary<string, float>
        {
            { "congressStability", 0.8f },
            { "lobbyistActivity", 0.2f }
        });
        Assert.AreEqual(1f, fromGov.Whole01(), 0.001f);
    }

    [Test]
    public void Demographics_ChangedShareSplitsRemainderEvenly()
    {
        var d = new ElectorateDemographics();
        d.slices = new List<ElectorateSlice>
        {
            new ElectorateSlice { sliceId = "a", share01 = 0.33f },
            new ElectorateSlice { sliceId = "b", share01 = 0.4f },
            new ElectorateSlice { sliceId = "c", share01 = 0.4f }
        };
        d.ReconcileChanged(0);
        Assert.AreEqual(0.33f, d.slices[0].share01, 0.001f);
        Assert.AreEqual(0.33f, d.slices[1].share01, 0.001f);
        Assert.AreEqual(0.34f, d.slices[2].share01, 0.001f);
        Assert.AreEqual(1f, d.Whole01(), 0.001f);
        var two = ElectorateDemographics.DefaultTwoParty();
        two.AddSlice("race", "example", 0.5f);
        Assert.AreEqual(0.25f, two.slices[0].share01, 0.001f);
        Assert.AreEqual(0.25f, two.slices[1].share01, 0.001f);
        Assert.AreEqual(0.5f, two.slices[2].share01, 0.001f);
    }

    [Test]
    public void Inpaint_BlocksVotePredicate()
    {
        var go = new GameObject("vote");
        try
        {
            var node = go.AddComponent<VoteBehaviorTreeNode>();
            node.voter = new VoterCard { place = new VotingPlaceCard { developerInpaint = true } };
            Assert.IsFalse(node.Predicate(null));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Causality_FailsUntilEvent()
    {
        var go = new GameObject("vote-c");
        try
        {
            var node = go.AddComponent<VoteBehaviorTreeNode>();
            var spec = ScriptableObject.CreateInstance<BallotSpec>();
            spec.causalityGates.Add(new VoteCausalityGate { requiredEventId = "read-candidate" });
            node.ballot = spec;
            node.executor = go.AddComponent<NarrativeExecutor>();
            Assert.IsFalse(node.Predicate(null));
            node.executor.GetRuntimeState().triggeredEventIds.Add("read-candidate");
            Assert.IsTrue(node.Predicate(null));
            Object.DestroyImmediate(spec);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Certify_AppliesWinLoseProperties()
    {
        var go = new GameObject("ledger");
        try
        {
            var ledger = go.AddComponent<VoteLedger>();
            var spec = ScriptableObject.CreateInstance<BallotSpec>();
            spec.ballotId = "gov";
            spec.kind = BallotKind.Candidate;
            var alice = new BallotOption { optionId = "alice", displayName = "Alice" };
            alice.win.Add(new VotePropertyAssignment("governor", "alice"));
            alice.win.Add(new VotePropertyAssignment("law.state.25b", "true"));
            var bob = new BallotOption { optionId = "bob", displayName = "Bob" };
            bob.win.Add(new VotePropertyAssignment("governor", "bob"));
            spec.options = new List<BallotOption> { alice, bob };
            var run = ledger.StartRun("gs1", spec);
            Assert.AreEqual("gs1", run.gameSessionId);
            ledger.Cast(run, "a1", "alice", "dem");
            ledger.Cast(run, "a2", "alice", "dem");
            ledger.Cast(run, "a3", "bob", "rep");
            ledger.Certify(run, spec);
            Assert.AreEqual("alice", ledger.certified.Get("governor"));
            Assert.AreEqual("true", ledger.certified.Get("law.state.25b"));
            var recount = ledger.Recount(run);
            Assert.AreNotEqual(run.runId, recount.runId);
            Assert.AreEqual(run.result.tallyHash, recount.Tally().tallyHash);
            Object.DestroyImmediate(spec);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void LegalBuildingSlots_AndBootstrap()
    {
        var go = new GameObject("polls");
        try
        {
            var stub = go.AddComponent<CivilInstitutionStub>();
            stub.kind = CivilSystemKind.VotingPlace;
            var boot = go.AddComponent<VotingPlaceBootstrap>();
            boot.stub = stub;
            boot.Ensure();
            Assert.IsNotNull(go.GetComponent<LaneGrid>());
            Assert.IsNotNull(go.GetComponent<VotingQueueHub>());
            Assert.IsNotNull(go.GetComponent<VotingPlaceBioRhythm>());
            Assert.IsNotNull(go.GetComponent<VoteLedger>());
            Assert.AreEqual(CivilSystemKind.VotingPlace, CivilSystemLattice.KindFromBuildingType("polling_station"));
            var slots = BuildingRequirementSpec.DefaultSlotsFor("voting_place");
            Assert.IsTrue(slots.Exists(s => s.slotId == "booth"));
            Assert.IsTrue(slots.Exists(s => s.slotId == "lane_grid"));
            Assert.IsTrue(slots.Exists(s => s.slotId == "feeder_queue"));
            var hub = go.GetComponent<VotingQueueHub>();
            Assert.GreaterOrEqual(hub.feeders.Count, 1);
            Assert.GreaterOrEqual(hub.booths.Count, 1);
            Assert.AreEqual(VoteLemmaPropertyKeys.DefaultInpaintPrompt, hub.inpaintPrompt);
            Assert.AreEqual(VoteLemmaPropertyKeys.DefaultInpaintPrompt, hub.ExecuteInpaintPrompt());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void FeatureBudget_VotingRank37()
    {
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        FeatureBudgetEntry voting = null;
        for (int i = 0; i < entries.Count; i++)
            if (entries[i].featureId == FeatureBudgetIds.Voting) voting = entries[i];
        Assert.IsNotNull(voting);
        Assert.AreEqual(37, voting.importanceRank);
        Assert.IsTrue(System.Array.IndexOf(voting.perfScopePrefixes, "LaneGrid") >= 0);
        Assert.IsTrue(System.Array.IndexOf(voting.perfScopePrefixes, "GameSession") >= 0);
    }

    [Test]
    public void VoteLemmas_Present()
    {
        Assert.AreEqual("vote", VoteLemmaPropertyKeys.Vote);
        Assert.AreEqual("recount", VoteLemmaPropertyKeys.Recount);
        Assert.AreEqual("game-session", GameSessionLemmaPropertyKeys.GameSession);
        Assert.AreEqual("local-save", GameSessionLemmaPropertyKeys.LocalSave);
        Assert.AreEqual("save-server-to-local", GameSessionLemmaPropertyKeys.SaveServerToLocal);
        Assert.AreEqual("local-server", GameSessionLemmaPropertyKeys.LocalServer);
        Assert.AreEqual("game-session", BuiltInSynonyms.CanonicalizeToken("game session"));
        Assert.AreEqual("local-save", BuiltInSynonyms.CanonicalizeToken("local save"));
        Assert.AreEqual("queue", VoteLemmaPropertyKeys.Queue);
        Assert.AreEqual("queued", VoteLemmaPropertyKeys.Queued);
        Assert.AreEqual("home-address", VoteLemmaPropertyKeys.HomeAddress);
        Assert.AreEqual("if-so", VoteLemmaPropertyKeys.IfSo);
        Assert.AreEqual("queued by address, or randomly, if so", VoteLemmaPropertyKeys.DefaultInpaintPrompt);
        Assert.AreEqual("home-address", BuiltInSynonyms.CanonicalizeToken("home address"));
        Assert.AreEqual(VotingQueueHub.DefaultInpaintPrompt, VoteLemmaPropertyKeys.DefaultInpaintPrompt);
    }

    [Test]
    public void BallotGovFold_MeasureQuestionCandidate()
    {
        Assert.AreEqual(BallotGovFold.RoleLaw, BallotGovFold.RoleFor(BallotKind.Measure));
        Assert.AreEqual(BallotGovFold.RoleJurisdiction, BallotGovFold.RoleFor(BallotKind.Question));
        Assert.AreEqual(BallotGovFold.RoleElectoral, BallotGovFold.RoleFor(BallotKind.Candidate));
        var junta = new GovernmentFlavorMix { junta01 = 1f };
        var electErrors = BallotGovFold.ErrorsFor(BallotKind.Candidate, junta);
        Assert.IsTrue(electErrors.Count > 0);
        Assert.AreEqual(0, BallotGovFold.ErrorsFor(BallotKind.Measure, junta).Count);
        var spec = ScriptableObject.CreateInstance<BallotSpec>();
        spec.ballotId = "item";
        spec.kind = BallotKind.Measure;
        BallotGovFold.EnsureKindDefaults(spec);
        Assert.AreEqual(2, spec.options.Count);
        Assert.AreEqual("law.item", spec.options[0].win[0].propertyName);
        Object.DestroyImmediate(spec);
        var rankedErr = BallotGovFold.ErrorsFor(BallotKind.Measure, null, BallotTallyMethod.Irv);
        Assert.IsTrue(rankedErr.Count > 0);
    }

    [Test]
    public void RankedTally_IrvTransfersAfterEliminate()
    {
        var rankings = new List<IList<string>>();
        for (int i = 0; i < 4; i++)
            rankings.Add(new List<string> { "a", "b" });
        for (int i = 0; i < 3; i++)
            rankings.Add(new List<string> { "b", "c" });
        for (int i = 0; i < 2; i++)
            rankings.Add(new List<string> { "c", "b" });
        var result = RankedTally.Run(rankings, BallotTallyMethod.Irv, 1, new[] { "a", "b", "c" });
        Assert.AreEqual(1, result.winners.Count);
        Assert.AreEqual("b", result.winners[0]);
        Assert.AreEqual(4, result.firstPreferences["a"]);
        bool eliminatedC = false;
        for (int i = 0; i < result.rounds.Count; i++)
            if (result.rounds[i].eliminated == "c")
                eliminatedC = true;
        Assert.IsTrue(eliminatedC);
    }

    [Test]
    public void RankedTally_StvTwoSeatsDroop()
    {
        var rankings = new List<IList<string>>
        {
            new List<string> { "a", "b" },
            new List<string> { "a", "b" },
            new List<string> { "a", "b" },
            new List<string> { "b", "c" },
            new List<string> { "c", "b" }
        };
        var result = RankedTally.Run(rankings, BallotTallyMethod.Stv, 2, new[] { "a", "b", "c" });
        Assert.AreEqual(2, result.quota);
        Assert.AreEqual(2, result.winners.Count);
        Assert.Contains("a", result.winners);
        Assert.Contains("b", result.winners);
    }

    [Test]
    public void QueueHub_AddressOrRandomAndInpaint()
    {
        var go = new GameObject("place");
        var aGo = new GameObject("a");
        try
        {
            var central = go.AddComponent<LaneGrid>();
            var hub = go.AddComponent<VotingQueueHub>();
            hub.centralQueue = central;
            hub.defaultFeederCount = 2;
            hub.assignSeed = 3;
            hub.CollectFeeders();
            Assert.AreEqual(2, hub.feeders.Count);
            Assert.AreEqual(
                hub.AssignFeederIndex("12 Main St", 1),
                hub.AssignFeederIndex("12 Main St", 99));
            int none = hub.AssignFeederIndex(null, 7);
            Assert.GreaterOrEqual(none, 0);
            Assert.Less(none, hub.feeders.Count);
            hub.placeCard = new VotingPlaceCard { developerInpaint = true, hub = hub, laneGrid = central };
            Assert.IsFalse(hub.TryAdvance());
            Assert.AreEqual(2, hub.feeders.Count);
            hub.placeCard.developerInpaint = false;
            var actor = aGo.AddComponent<BaseAmbulatingActor>();
            Assert.IsTrue(central.TryEnqueue(actor));
            hub.SetHomeAddress(actor, "12 Main St");
            Assert.IsTrue(hub.TryAdvance());
            Assert.AreEqual(0, central.queue.Count);
            Assert.AreEqual(1, hub.AssignFeeder(actor).queue.Count);
            var bag = new VotePropertyBag();
            bag.Set(VoteLemmaPropertyKeys.HomeAddress, "9 Elm");
            hub.propertyBag = bag;
            Assert.AreEqual("9 Elm", hub.HomeAddressFor(null));
        }
        finally
        {
            Object.DestroyImmediate(aGo);
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void BoothStation_LayoutsAndOccupy()
    {
        var fourGo = new GameObject("booth4");
        var twoGo = new GameObject("booth2");
        var actorGo = new GameObject("voter");
        try
        {
            var four = fourGo.AddComponent<VotingBoothStation>();
            four.layout = VotingBoothQueueLayout.FourSectionDivided;
            four.EnsureSections();
            Assert.AreEqual(4, four.sections.Count);
            var two = twoGo.AddComponent<VotingBoothStation>();
            two.layout = VotingBoothQueueLayout.TwoSectionBackToBack;
            two.EnsureSections();
            Assert.AreEqual(2, two.sections.Count);
            Assert.AreEqual(1, VotingBoothStation.SectionCountFor(VotingBoothQueueLayout.Single));
            var actor = actorGo.AddComponent<BaseAmbulatingActor>();
            Assert.IsTrue(two.TryAccept(actor));
            Assert.IsTrue(two.TryOccupyHead());
            Assert.IsTrue(two.Occupies(actor));
            Assert.AreEqual("homeAddress", VotePropertyBag.HomeAddressKey);
        }
        finally
        {
            Object.DestroyImmediate(actorGo);
            Object.DestroyImmediate(fourGo);
            Object.DestroyImmediate(twoGo);
        }
    }
}
#endif
