using System;
using NUnit.Framework;
using UnityEngine;

public sealed class PoliceVehicleRepairTests
{
    [Test]
    public void TAMaintenance_RepairsIntegrity()
    {
        var go = new GameObject("v");
        var v = go.AddComponent<VehicleRagdoll>();
        v.integrity01 = 0.4f;
        var card = TAMaintenanceCard.GenerateRepair(v);
        card.ApplyMaintenance();
        Assert.Greater(v.integrity01, 0.4f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void VehicleRepairCenter_BootstrapAndCards()
    {
        var go = new GameObject("shop");
        var stub = go.AddComponent<CivilInstitutionStub>();
        stub.kind = CivilSystemKind.CarRepair;
        go.AddComponent<VehicleRepairCenterBootstrap>().Ensure();
        var rt = go.GetComponent<VehicleRepairCenterRuntime>();
        Assert.IsNotNull(rt);
        Assert.IsNotNull(rt.company);
        var vGo = new GameObject("car");
        var v = vGo.AddComponent<VehicleRagdoll>();
        rt.AcceptVehicle(v);
        Assert.IsNotNull(rt.Repair(v));
        UnityEngine.Object.DestroyImmediate(vGo);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void PoliceStation_DispatchAndCards()
    {
        var hubGo = new GameObject("hub");
        hubGo.AddComponent<CentralDispatchHub>();
        var go = new GameObject("pd");
        go.AddComponent<PoliceStationBootstrap>().Ensure();
        var station = go.GetComponent<PoliceStationBuildingRagdoll>();
        Assert.IsNotNull(station.dispatchBio);
        Assert.AreEqual("police", station.dispatchBio.serviceId);

        var cards = station.dispatchBio.FacilitateCards(new DispatchRequest
        {
            kind = "route",
            worldTarget = Vector3.one,
            notes = "violence"
        });
        Assert.IsTrue(cards.Exists(c => c is CopCard));
        Assert.IsTrue(cards.Exists(c => c is CopPullOverCard));

        UnityEngine.Object.DestroyImmediate(go);
        UnityEngine.Object.DestroyImmediate(hubGo);
    }

    [Test]
    public void PoliceCar_LightsAndWeaponsChest()
    {
        var go = new GameObject("cruiser");
        var car = go.AddComponent<PoliceCarVehicleRagdoll>();
        car.EnsureDefaultLights();
        car.SetLights(true);
        Assert.IsTrue(car.lightsOn);

        car.requiresTelecomForWeapons = false;
        Assert.IsTrue(car.TryOpenWeaponsChest(out _));
        Assert.IsTrue(car.weaponsChestUnlocked);

        car.weaponsChestUnlocked = false;
        car.requiresTelecomForWeapons = true;
        Assert.IsFalse(car.TryOpenWeaponsChest(out var code));
        Assert.IsFalse(string.IsNullOrEmpty(code));
        Assert.IsTrue(car.ConfirmWeaponCode(code));

        var lights = CopLightsCard.Generate(car, false);
        lights.Apply();
        Assert.IsFalse(car.lightsOn);

        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void CuffsAndSpikeTrap()
    {
        var cuffGo = new GameObject("cuffs");
        var cuffs = cuffGo.AddComponent<PoliceCuffItem>();
        Assert.IsFalse(cuffs.TryUnlock("wrong"));
        Assert.IsTrue(cuffs.TryUnlock(cuffs.keyId));

        var trapGo = new GameObject("spikes");
        var trap = trapGo.AddComponent<SpikeTrapItem>();
        trap.Deploy(Vector3.forward * 2f);
        Assert.IsTrue(trap.deployed);

        UnityEngine.Object.DestroyImmediate(cuffGo);
        UnityEngine.Object.DestroyImmediate(trapGo);
    }

    [Test]
    public void JailAndInterrogateCards()
    {
        var civ = new GameObject("civ");
        var jail = PoliceJailCivilianCard.Generate(civ);
        Assert.IsNotNull(jail.wrestlingCompose);
        var room = new PoliceInterrogationRoom { roomId = "i1", enableDialog = true };
        var inter = PoliceInterrogateCard.Generate(room, civ);
        Assert.IsTrue(inter.bindDialog);
        UnityEngine.Object.DestroyImmediate(civ);
    }

    [Test]
    public void VehicleInteriorSize()
    {
        var go = new GameObject("v");
        var v = go.AddComponent<VehicleRagdoll>();
        v.interiors.Clear();
        v.interiors.Add(new VehicleInventorySection { sectionName = "a", capacity = 10 });
        v.interiors.Add(new VehicleInventorySection { sectionName = "b", capacity = 15 });
        v.totalInteriorSize = 0;
        v.RecalculateTotalInteriorSize();
        Assert.AreEqual(25f, v.totalInteriorSize, 0.01f);
        var dto = v.ToDto();
        Assert.AreEqual(25f, (float)dto["totalSize"], 0.01f);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void BuildingRequirement_PoliceAndRepairSlots()
    {
        var police = BuildingRequirementSpec.DefaultSlotsFor("police_station");
        Assert.IsTrue(police.Exists(s => s.slotId == "interrogation"));
        Assert.IsTrue(police.Exists(s => s.slotId == "holding"));
        var repair = BuildingRequirementSpec.DefaultSlotsFor("car_repair");
        Assert.IsTrue(repair.Exists(s => s.slotId == "maintenance_bay"));
    }
}
