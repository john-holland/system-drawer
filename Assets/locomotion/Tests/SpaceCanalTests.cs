using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class SpaceCanalTests
{
    [Test]
    public void GerBus_DarkDeniesGate()
    {
        var go = new GameObject("space");
        try
        {
            var dispatcher = go.AddComponent<SpaceCannalDispatcher>();
            dispatcher.ger = new GerPowerBus { live = true, charge01 = 1f };
            Assert.IsTrue(dispatcher.GateOpen());
            dispatcher.ger.live = false;
            dispatcher.ger.charge01 = 0f;
            Assert.IsFalse(dispatcher.GateOpen());

            var bio = go.AddComponent<SpaceCannalBioRhythm>();
            bio.dispatcher = dispatcher;
            bio.ger = dispatcher.ger;
            Assert.IsFalse(bio.GateAllowsTransit());
            var cards = bio.FacilitateCards(new DispatchRequest { kind = "lock" });
            Assert.IsFalse(cards.Exists(c => c is SpaceCannalTransitCard));

            dispatcher.ger.live = true;
            dispatcher.ger.charge01 = 1f;
            Assert.IsTrue(bio.GateAllowsTransit());
            var liveCards = bio.FacilitateCards(new DispatchRequest { kind = "lock" });
            Assert.IsTrue(liveCards.Exists(c => c is SpaceCannalTransitCard));
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void SpaceAgent_OverLimitWhenGateDark()
    {
        var go = new GameObject("space_ta");
        try
        {
            var dispatcher = go.AddComponent<SpaceCannalDispatcher>();
            dispatcher.ger = new GerPowerBus { live = false, charge01 = 0f };
            var agent = go.AddComponent<SpaceCannalTravelAgent>();
            agent.dispatcher = dispatcher;
            Assert.AreEqual("Speed", SpaceCannalTravelAgent.DiamondAxes[0]);
            Assert.AreEqual(4, agent.BlueOptimal01().Length);
            Assert.IsTrue(agent.GateDenied());
            Assert.IsTrue(agent.OverLimit());
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void ApartmentLayer_AndMonarchHousing()
    {
        var grid = ScriptableObject.CreateInstance<CityPixelGrid>();
        var go = new GameObject("housing");
        try
        {
            var house = go.AddComponent<HousingBuildingRagdoll>();
            house.gerBus = new GerPowerBus { live = false, charge01 = 0f };
            var zone = go.AddComponent<SpaceCannalHousing>();
            zone.cityGrid = grid;
            zone.apartments = house;
            zone.EnsureApartmentLayer();
            Assert.IsTrue(grid.layers.Exists(l => l != null && l.kind == CityPixelLayerKind.Apartment));
            var card = zone.AudienceCard();
            Assert.AreEqual("audience", card.decorum);
            CityUtilityGridSeeder.SeedGerFromHouses(grid, new List<HousingBuildingRagdoll> { house });
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(grid);
        }
    }

    [Test]
    public void SlipstreamSolver_ReturnsPathWhenContextPresent()
    {
        var go = new GameObject("path");
        try
        {
            var dispatcher = go.AddComponent<SpaceCannalDispatcher>();
            List<Vector3> path;
            Assert.IsFalse(dispatcher.TrySlipPath(null, Vector3.zero, Vector3.one, out path));
            var solver = go.AddComponent<HierarchicalPathingSolver>();
            Assert.IsTrue(dispatcher.TrySlipPath(solver, Vector3.zero, Vector3.right * 10f, out path));
            Assert.Greater(path.Count, 1);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
