#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class VehicleInstrumentPhysicsProxyTests
{
    [Test]
    public void Proxy_ResolvesLocalId_ToRemoteSurface()
    {
        var source = new GameObject("source");
        var remote = new GameObject("remote");
        var remoteBody = remote.AddComponent<Rigidbody>();
        remoteBody.isKinematic = true;
        var remoteActor = remote.AddComponent<VehicleActor>();

        var proxy = source.AddComponent<VehicleInstrumentPhysicsProxy>();
        var map = ScriptableObject.CreateInstance<VehicleInstrumentMap>();
        map.ReplaceSlots(new List<VehicleInstrumentSlot>
        {
            new VehicleInstrumentSlot { id = "jet.pitch", impulseChannelKey = "jet_pitch" }
        });
        proxy.sourceMap = map;
        proxy.bindings = new List<VehicleInstrumentBinding>
        {
            new VehicleInstrumentBinding
            {
                localSurfaceId = "jet.pitch",
                remoteVehicle = remoteActor,
                remoteSurfaceId = "ladder.yaw",
                maxForceNewtons = 100f,
                applyAsTorque = true,
                localForceAxis = Vector3.up
            }
        };

        Assert.IsTrue(proxy.TryResolve("jet.pitch", out var surface));
        Assert.AreEqual("ladder.yaw", surface.Id);
        Assert.AreSame(remoteActor, surface.Owner);

        Object.DestroyImmediate(map);
        Object.DestroyImmediate(source);
        Object.DestroyImmediate(remote);
    }

    [Test]
    public void Proxy_RouteCard_RejectedWhenChannelUnbound()
    {
        var source = new GameObject("source");
        var proxy = source.AddComponent<VehicleInstrumentPhysicsProxy>();
        var map = ScriptableObject.CreateInstance<VehicleInstrumentMap>();
        map.ReplaceSlots(new List<VehicleInstrumentSlot>
        {
            new VehicleInstrumentSlot { id = "steer", impulseChannelKey = "vehicle_steering" }
        });
        proxy.sourceMap = map;
        proxy.bindings = new List<VehicleInstrumentBinding>();

        var card = new GoodSection
        {
            impulseStack = new List<ImpulseAction>
            {
                new ImpulseAction { muscleGroup = "vehicle_steering", activation = 1f }
            }
        };
        Assert.IsFalse(proxy.RouteCard(card, 0.016f));

        Object.DestroyImmediate(map);
        Object.DestroyImmediate(source);
    }

    [Test]
    public void Proxy_RouteCard_AppliesForceToRemoteRigidbody()
    {
        var source = new GameObject("source");
        var remote = new GameObject("remote");
        remote.transform.position = Vector3.zero;
        var remoteBody = remote.AddComponent<Rigidbody>();
        remoteBody.useGravity = false;
        var remoteActor = remote.AddComponent<VehicleActor>();

        var proxy = source.AddComponent<VehicleInstrumentPhysicsProxy>();
        var map = ScriptableObject.CreateInstance<VehicleInstrumentMap>();
        map.ReplaceSlots(new List<VehicleInstrumentSlot>
        {
            new VehicleInstrumentSlot { id = "jet.yaw", impulseChannelKey = "jet_yaw" }
        });
        proxy.sourceMap = map;
        proxy.bindings = new List<VehicleInstrumentBinding>
        {
            new VehicleInstrumentBinding
            {
                localSurfaceId = "jet.yaw",
                remoteVehicle = remoteActor,
                remoteSurfaceId = "truck.thrust",
                maxForceNewtons = 500f,
                localForceAxis = Vector3.forward
            }
        };

        var card = new GoodSection
        {
            impulseStack = new List<ImpulseAction>
            {
                new ImpulseAction { muscleGroup = "jet_yaw", activation = 1f }
            }
        };
        Assert.IsTrue(proxy.RouteCard(card, 0.02f));
        // ForceMode.Force accumulates; step physics once for non-kinematic body.
        var prevSim = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        try { Physics.Simulate(0.02f); }
        finally { Physics.simulationMode = prevSim; }
        Assert.Greater(remoteBody.linearVelocity.sqrMagnitude + remoteBody.angularVelocity.sqrMagnitude, 0f);

        Object.DestroyImmediate(map);
        Object.DestroyImmediate(source);
        Object.DestroyImmediate(remote);
    }

    [Test]
    public void ProxiedSolver_FiltersUnboundChannels()
    {
        var go = new GameObject("proxied");
        var solver = go.AddComponent<ProxiedDrivingPhysicsCardSolver>();
        var proxy = go.AddComponent<VehicleInstrumentPhysicsProxy>();
        var map = ScriptableObject.CreateInstance<VehicleInstrumentMap>();
        map.ReplaceSlots(new List<VehicleInstrumentSlot>
        {
            new VehicleInstrumentSlot { id = "jet.pitch", impulseChannelKey = "jet_pitch" }
        });
        solver.instrumentMap = map;
        solver.physicsProxy = proxy;
        solver.requireProxyBinding = true;
        solver.activeDrivePhaseMask = DriveAnimationPhase.Aux;
        solver.availableDriveCards = new List<GoodSection>
        {
            new GoodSection
            {
                impulseStack = new List<ImpulseAction>
                {
                    new ImpulseAction { muscleGroup = "jet_pitch", activation = 1f }
                },
                driveAnimationPhase = DriveAnimationPhase.Aux
            }
        };
        proxy.sourceMap = map;

        Assert.AreEqual(0, solver.FindApplicableCards(new RagdollState()).Count);

        var remote = new GameObject("remote");
        remote.AddComponent<Rigidbody>().isKinematic = true;
        var actor = remote.AddComponent<VehicleActor>();
        proxy.bindings = new List<VehicleInstrumentBinding>
        {
            new VehicleInstrumentBinding
            {
                localSurfaceId = "jet.pitch",
                remoteVehicle = actor,
                remoteSurfaceId = "ladder.pitch",
                maxForceNewtons = 10f
            }
        };
        Assert.AreEqual(1, solver.FindApplicableCards(new RagdollState()).Count);

        Object.DestroyImmediate(map);
        Object.DestroyImmediate(go);
        Object.DestroyImmediate(remote);
    }
}
#endif
