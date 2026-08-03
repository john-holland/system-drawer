using NUnit.Framework;
using UnityEngine;

public sealed class BuildingRagdollTests
{
    [Test]
    public void ImpulseMemory_WoodBuildsFasterThanMetal()
    {
        var go = new GameObject("bldg");
        var ragdoll = go.AddComponent<BuildingRagdoll>();
        ragdoll.buildingStableId = "test-bldg";
        var queue = go.AddComponent<DamagedObjectQueue>();
        ragdoll.damageQueue = queue;
        ragdoll.enqueueDamageThreshold01 = 0.01f;

        var woodGo = new GameObject("wood");
        woodGo.transform.SetParent(go.transform);
        var wood = woodGo.AddComponent<ImpulseMaterialMemory>();
        wood.materialClass = BuildingMaterialClass.Wood;
        wood.memoryTau = 4f;
        wood.buildingRagdoll = ragdoll;

        var metalGo = new GameObject("metal");
        metalGo.transform.SetParent(go.transform);
        var metal = metalGo.AddComponent<ImpulseMaterialMemory>();
        metal.materialClass = BuildingMaterialClass.Metal;
        metal.memoryTau = 40f;
        metal.buildingRagdoll = ragdoll;

        wood.ApplyImpulse(800f, Vector3.zero, false);
        metal.ApplyImpulse(800f, Vector3.zero, true);

        Assert.Greater(wood.memory01, metal.memory01);
        Assert.Less(ragdoll.Health.integrity01, 1f);
        Assert.Greater(queue.OpenCount, 0);

        Object.DestroyImmediate(go);
    }

    [Test]
    public void CivicAndCivilCards_FlagsAndSolver()
    {
        var civic = CivicCard.Generate(CivicDutyKind.Repair, null);
        Assert.IsTrue(civic.isCivicGoal);
        var civil = CivilCard.Generate(CivilianDutyKind.WorkShift);
        Assert.IsTrue(civil.isCivilGoal);

        var solverGo = new GameObject("solver");
        var solver = solverGo.AddComponent<PhysicsCardSolver>();
        var civicCards = solver.SolveForGoal(new BehaviorTreeGoal { type = GoalType.Civic }, new RagdollState());
        Assert.IsTrue(civicCards.Count > 0);
        Assert.IsTrue(civicCards[0].isCivicGoal || civicCards[0] is CivicCard);

        var civilCards = solver.SolveForGoal(new BehaviorTreeGoal { type = GoalType.Civil }, new RagdollState());
        Assert.IsTrue(civilCards.Count > 0);

        Object.DestroyImmediate(solverGo);
    }

    [Test]
    public void Justice_ViolenceThreshold_DefaultFlee()
    {
        var j = JusticeCard.Generate(JusticeAction.SecureArea, null);
        j.violenceThreshold01 = 0.7f;
        j.snapDepression01 = 0f;
        Assert.IsFalse(j.ShouldRespondPhysically(null, 0.4f));
        Assert.IsTrue(j.ShouldRespondPhysically(null, 0.8f));
        j.ApplySnap(0.5f);
        Assert.IsTrue(j.ShouldRespondPhysically(null, 0.4f));
    }

    [Test]
    public void KindFromBuildingType_Expanded()
    {
        Assert.AreEqual(CivilSystemKind.SoupKitchen, CivilSystemLattice.KindFromBuildingType("soup_kitchen"));
        Assert.AreEqual(CivilSystemKind.PoliceStation, CivilSystemLattice.KindFromBuildingType("police_station"));
        Assert.AreEqual(CivilSystemKind.LiquorStore, CivilSystemLattice.KindFromBuildingType("liquor_store"));
        Assert.AreEqual(CivilSystemKind.Church, CivilSystemLattice.KindFromBuildingType("church_small"));
    }

    [Test]
    public void BuildingBeast_IsStubOnly()
    {
        var go = new GameObject("beast");
        var beast = go.AddComponent<BuildingBeast>();
        Assert.IsTrue(beast.stubOnly);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void StoreBase_FillShelves()
    {
        var go = new GameObject("store");
        var store = go.AddComponent<StoreBase>();
        store.FillShelvesFromCatalog(new[] { "beer", "wine" }, 5);
        Assert.AreEqual(5, store.shelves.Count);
        Assert.IsTrue(store.ResolveShelfPrompt().Length > 0);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void BuildingRequirementSpec_Validate()
    {
        var spec = BuildingRequirementSpec.CreateDefault("house", CivilSystemKind.House);
        Assert.IsFalse(spec.Validate(out _));
        for (int i = 0; i < spec.slots.Count; i++)
        {
            if (spec.slots[i].required)
                spec.slots[i].referenceObject = new GameObject("ref");
        }
        Assert.IsTrue(spec.Validate(out _));
        for (int i = 0; i < spec.slots.Count; i++)
            if (spec.slots[i].referenceObject != null)
                Object.DestroyImmediate(spec.slots[i].referenceObject);
        Object.DestroyImmediate(spec);
    }
}
