#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public class TrainDispatchElevatorInspectorTests
{
    [Test]
    public void DispatchMC_Facilitate_EngineerStart_YieldsCards()
    {
        var root = new GameObject("tdmc");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var mc = root.AddComponent<TrainDispatchMissionControlBioRhythm>();
            var cards = mc.FacilitateCards(new DispatchRequest { kind = TrainDispatchKinds.EngineerStart });
            Assert.IsTrue(cards.Exists(c => c is TrainEngineerStartCard));
            Assert.IsTrue(cards.Exists(c => c is TrainDispatchStartCard));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void StationBio_Facilitate_Attendant()
    {
        var root = new GameObject("ts");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var bio = root.AddComponent<TrainStationBioRhythm>();
            var cards = bio.FacilitateCards(new DispatchRequest { kind = TrainDispatchKinds.Attendant });
            Assert.IsTrue(cards.Exists(c => c is TSATrainEngineerAttendant));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void RailTrack_FindBySegmentId_AndSample()
    {
        var go = new GameObject("track");
        try
        {
            var track = go.AddComponent<RailTrackStructure>();
            track.railSegmentId = "seg_test";
            track.controlPoints = new System.Collections.Generic.List<Vector3>
            {
                Vector3.zero, new Vector3(0f, 0f, 40f)
            };
            track.EnsureSplinePoints();
            Assert.AreSame(track, RailTrackStructure.FindBySegmentId("seg_test"));
            Assert.Greater(Vector3.Distance(track.SamplePosition(0f), track.SamplePosition(1f)), 1f);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void Elevator_CallFloor_AndButtonPress()
    {
        var go = new GameObject("elev");
        try
        {
            var elev = go.AddComponent<ElevatorVehicleRagdoll>();
            elev.minFloor = 0;
            elev.maxFloor = 5;
            var panel = go.GetComponent<ElevatorButtonPanel>();
            Assert.IsNotNull(panel);
            Assert.IsTrue(elev.CallFloor(3));
            Assert.AreEqual(3, elev.currentFloor);
            Assert.IsTrue(panel.TryPressCell(0, 0));
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void OpinionFor_AndPetWarden_Judge()
    {
        var petGo = new GameObject("pet");
        var actorGo = new GameObject("actor");
        try
        {
            var ragdoll = petGo.AddComponent<RagdollSystem>();
            var warden = petGo.AddComponent<PetWarden>();
            var op = ragdoll.OpinionFor(actorGo);
            op.ownership01 = 0.9f;
            op.like01 = 0.8f;
            Assert.AreEqual(PetJudgment.Allow, warden.Judge(ragdoll, actorGo, PetInteractionKind.Pet));
            op.fear01 = 0.95f;
            Assert.AreEqual(PetJudgment.EscalateThreat, warden.Judge(ragdoll, actorGo, PetInteractionKind.Pet));
        }
        finally
        {
            Object.DestroyImmediate(petGo);
            Object.DestroyImmediate(actorGo);
        }
    }

    [Test]
    public void InspectorBio_Facilitate_KnockAndTravel()
    {
        var root = new GameObject("insp");
        try
        {
            root.AddComponent<CentralDispatchHub>();
            var bio = root.AddComponent<InspectorBioRhythm>();
            var cards = bio.FacilitateCards(new DispatchRequest { kind = "inspect", notes = "train" });
            Assert.IsTrue(cards.Exists(c => c is InspectorKnockCard));
            Assert.IsTrue(cards.Exists(c => c is InspectorTravelOptionCard));
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void Bus_EnsureSharedHolds_FromGrabBars()
    {
        var go = new GameObject("bus");
        try
        {
            var bar = new GameObject("grab_bar");
            bar.transform.SetParent(go.transform);
            var bus = go.AddComponent<BusVehicleRagdoll>();
            bus.grabBars.Add(bar.transform);
            bus.EnsureSharedHolds();
            Assert.Greater(bus.grabHolds.Count, 0);
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void SeatTicket_Apply_SetsAisleWidth()
    {
        var go = new GameObject("train");
        try
        {
            var train = go.AddComponent<TrainVehicleRagdoll>();
            var aisle = go.AddComponent<PlanarSplinePathLocomotion>();
            aisle.controlPoints = new System.Collections.Generic.List<Vector3>
            {
                Vector3.zero, new Vector3(0f, 0f, 10f)
            };
            train.aislePath = aisle;
            train.seatTicket = new TrainSeatTicketConfig { leftGridWidth = 3, rightGridWidth = 3 };
            train.seatTicket.ApplyTo(train);
            Assert.Less(aisle.defaultWidth, 1.2f);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
#endif
