#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class PhysicalPathingAndDriveTests
{
    [Test]
    public void PhysicalPathingSolverRegistry_Air_IsRegistered()
    {
        Assert.IsTrue(PhysicalPathingSolverRegistry.TryGetSolver(PhysicalPathingMedium.Air, out var s));
        Assert.IsNotNull(s);
        Assert.AreEqual(PhysicalPathingMedium.Air, s.Medium);
    }

    [Test]
    public void InstrumentImpulseValidator_AllowsMappedChannelOnly()
    {
        var map = ScriptableObject.CreateInstance<VehicleInstrumentMap>();
        map.ReplaceSlots(new List<VehicleInstrumentSlot>
        {
            new VehicleInstrumentSlot { id = "steer", impulseChannelKey = "vehicle_steering" }
        });

        var okStack = new List<ImpulseAction>
        {
            new ImpulseAction { muscleGroup = "vehicle_steering", activation = 1f }
        };
        var badStack = new List<ImpulseAction>
        {
            new ImpulseAction { muscleGroup = "left_leg", activation = 1f }
        };

        Assert.IsTrue(InstrumentImpulseValidator.ValidateImpulseStack(okStack, map));
        Assert.IsFalse(InstrumentImpulseValidator.ValidateImpulseStack(badStack, map));

        Object.DestroyImmediate(map);
    }

    [Test]
    public void PhysicalPathingGoodSectionStubs_Air_HasMedium()
    {
        var g = PhysicalPathingGoodSectionStubs.CreateAirGlideStub();
        Assert.AreEqual(PhysicalPathingMedium.Air, g.physicalPathingMedium);
    }

    [Test]
    public void DrivingPhysicsCardSolver_PhaseGate_FiltersCard()
    {
        var go = new GameObject("dpcs");
        var solver = go.AddComponent<DrivingPhysicsCardSolver>();
        var map = ScriptableObject.CreateInstance<VehicleInstrumentMap>();
        map.ReplaceSlots(new List<VehicleInstrumentSlot>
        {
            new VehicleInstrumentSlot { id = "t", impulseChannelKey = "thr" }
        });

        solver.instrumentMap = map;
        solver.activeDrivePhaseMask = DriveAnimationPhase.Steer;
        solver.availableDriveCards = new List<GoodSection>
        {
            new GoodSection
            {
                sectionName = "throttle_only",
                impulseStack = new List<ImpulseAction> { new ImpulseAction { muscleGroup = "thr", activation = 1f } },
                driveAnimationPhase = DriveAnimationPhase.Throttle
            }
        };

        var state = new RagdollState();
        var applicable = solver.FindApplicableCards(state);
        Assert.AreEqual(0, applicable.Count);

        solver.availableDriveCards[0].driveAnimationPhase = DriveAnimationPhase.Steer;
        applicable = solver.FindApplicableCards(state);
        Assert.AreEqual(1, applicable.Count);

        Object.DestroyImmediate(map);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void PhysicalMediumVolumeIndex_WaterBlocksDriveMode()
    {
        Assert.IsFalse(PhysicalMediumVolumeRules.MediumAllowsMode(PhysicalPathingMedium.Water, TravelLegMode.Drive));
    }

    [Test]
    public void TerminalGoodSectionStubs_HaveTerminalLegMode()
    {
        Assert.AreEqual(TravelLegMode.ParkWater, PhysicalPathingGoodSectionStubs.CreateParkWaterStub().terminalLegMode);
        Assert.AreEqual(TravelLegMode.LandWater, PhysicalPathingGoodSectionStubs.CreateLandWaterStub().terminalLegMode);
        Assert.AreEqual(PhysicalPathingMedium.Water, PhysicalPathingGoodSectionStubs.CreateMoorStub().physicalPathingMedium);
    }

    [Test]
    public void MediumAllowsTerminalLeg_Beach_AllowsGroundAndWater()
    {
        Assert.IsTrue(PhysicalMediumVolumeRules.MediumAllowsTerminalLeg(
            PhysicalPathingMedium.Ground, TravelLegMode.Beach));
        Assert.IsTrue(PhysicalMediumVolumeRules.MediumAllowsTerminalLeg(
            PhysicalPathingMedium.Water, TravelLegMode.Beach));
    }
}
#endif
