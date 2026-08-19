using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class PrisonVenueTests
{
    [Test]
    public void KindFromBuildingType_Prison()
    {
        Assert.AreEqual(CivilSystemKind.Prison, CivilSystemLattice.KindFromBuildingType("prison"));
        Assert.AreEqual(CivilSystemKind.Prison, CivilSystemLattice.KindFromBuildingType("county_jail"));
        Assert.AreEqual(CivilSystemKind.Prison, CivilSystemLattice.KindFromBuildingType("corrections_center"));
    }

    [Test]
    public void DefaultSlots_IncludeCellsAndYard()
    {
        var slots = BuildingRequirementSpec.DefaultSlotsFor("prison");
        Assert.IsTrue(slots.Exists(s => s.slotId == "cells"));
        Assert.IsTrue(slots.Exists(s => s.slotId == "yard"));
        Assert.IsTrue(slots.Exists(s => s.slotId == "cafeteria"));
        Assert.IsTrue(slots.Exists(s => s.slotId == "parole_board"));
        Assert.IsTrue(slots.Exists(s => s.slotId == "warden_office"));
    }

    [Test]
    public void Bootstrap_WiresBioDispatchAndKeycard()
    {
        var hubGo = new GameObject("hub");
        hubGo.AddComponent<CentralDispatchHub>();
        var go = new GameObject("prison");
        var stub = go.AddComponent<CivilInstitutionStub>();
        stub.kind = CivilSystemKind.Prison;
        go.AddComponent<PrisonBootstrap>().Ensure();
        var ragdoll = go.GetComponent<PrisonBuildingRagdoll>();
        Assert.IsNotNull(ragdoll);
        Assert.IsNotNull(ragdoll.dispatchBio);
        Assert.AreEqual("corrections", ragdoll.dispatchBio.serviceId);
        Assert.IsNotNull(go.GetComponent<KeycardLock>());
        Assert.IsNotNull(go.GetComponent<AuthWarden>());
        Assert.IsNotNull(go.GetComponent<PrisonWarden>());

        var cards = ragdoll.dispatchBio.FacilitateCards(new DispatchRequest
        {
            kind = "yard",
            worldTarget = Vector3.one
        });
        Assert.IsTrue(cards.Exists(c => c is PrisonerCard));
        Assert.IsTrue(cards.Exists(c => c is PrisonGuardCard));

        Object.DestroyImmediate(go);
        Object.DestroyImmediate(hubGo);
    }

    [Test]
    public void SwitcherooCatalog_AppliesPackId()
    {
        var cat = ScriptableObject.CreateInstance<PrisonerSwitcherooCatalog>();
        cat.packs = new List<PrisonerSwitcherooPack>
        {
            new PrisonerSwitcherooPack { packId = "orange", label = "Orange" }
        };
        var rec = new PrisonerRecord { switcherooPackId = "orange" };
        var pack = cat.ApplyAtSpawn(rec);
        Assert.AreEqual("orange", pack.packId);
        Object.DestroyImmediate(cat);
    }
}
