#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class VehicleVideoSteeringTests
{
    [Test]
    public void FeatureBudget_HasVehicleDetect()
    {
        Assert.AreEqual("vehicle_detect", FeatureBudgetIds.VehicleDetect);
        var entries = FeatureBudgetDefaults.CreateDefaultEntries();
        Assert.IsTrue(entries.Exists(e => e.featureId == FeatureBudgetIds.VehicleDetect));
    }

    [Test]
    public void Metadata_RoundTripsVehicleTrackAndFacing()
    {
        var asset = ScriptableObject.CreateInstance<WebcamAnimRecordingAsset>();
        try
        {
            asset.kind = WebcamAnimKind.Vehicle;
            asset.modelSpec = VehicleVideoSteeringIds.Yolo26IntelSpec;
            asset.vehicleTrackPath = "/tmp/car.vehicletrack.json";
            asset.facingYawDegrees = 90f;
            var meta = asset.ToTypeMetadata();
            Assert.AreEqual("/tmp/car.vehicletrack.json", meta.vehicleTrackPath);
            Assert.AreEqual(90f, meta.facingYawDegrees, 0.01f);

            var other = ScriptableObject.CreateInstance<WebcamAnimRecordingAsset>();
            try
            {
                other.ApplyTypeMetadata(meta);
                Assert.AreEqual(VehicleVideoSteeringIds.Yolo26IntelSpec, other.modelSpec);
                Assert.AreEqual("/tmp/car.vehicletrack.json", other.vehicleTrackPath);
                Assert.AreEqual(90f, other.facingYawDegrees, 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void Projector_KeepsTrackIdAcrossCut()
    {
        var splineGo = NewSpline();
        try
        {
            var spline = splineGo.GetComponent<VehicleRoadCenterSpline>();
            var track = new VehicleTrack
            {
                modelSpec = VehicleVideoSteeringIds.Yolo26IntelSpec,
                frames = new[]
                {
                    Frame(0, 7, 2, "car", 0.4f, 0.5f, 0.2f),
                    Frame(80, 7, 2, "car", 0.42f, 0.5f, 0.2f),
                    Frame(160, 7, 2, "car", 0.45f, 0.5f, 0.2f)
                },
                segments = new[]
                {
                    Seg(0, 80, 7, 2),
                    Seg(160, 160, 7, 2)
                }
            };
            var result = VehicleTrackProjector.Project(track, spline, 0f);
            Assert.AreEqual(7, result.subjectTrackId);
            Assert.Greater(result.waypoints.Count, 0);
            for (int i = 0; i < result.waypoints.Count; i++)
                Assert.AreEqual(7, result.waypoints[i].trackId);
        }
        finally
        {
            Object.DestroyImmediate(splineGo);
        }
    }

    [Test]
    public void Projector_NearestSameClassBeatsCloserOtherType()
    {
        var splineGo = NewSpline();
        try
        {
            var spline = splineGo.GetComponent<VehicleRoadCenterSpline>();
            var prev = Frame(0, 1, 2, "car", 0.5f, 0.5f, 0.3f);
            var motorcycleCloser = Frame(100, 99, 3, "motorcycle", 0.51f, 0.5f, 0.4f);
            var carFarther = Frame(100, 8, 2, "car", 0.2f, 0.5f, 0.15f);
            var bound = VehicleTrackProjector.ContinueIdentity(
                1, 2, 8f, new Vector2(0.5f, 0.5f),
                new[] { motorcycleCloser, carFarther },
                spline, 90f);
            Assert.IsNotNull(bound);
            Assert.AreEqual(8, bound.trackId);
            Assert.AreEqual(2, bound.classId);
            Assert.AreNotEqual(3, bound.classId);

            var track = new VehicleTrack
            {
                frames = new[] { prev, motorcycleCloser, carFarther },
                segments = new[] { Seg(0, 0, 1, 2), Seg(100, 100, 8, 2) }
            };
            var result = VehicleTrackProjector.Project(track, spline, 0f);
            Assert.AreEqual(2, result.subjectClassId);
            Assert.AreEqual(8, result.waypoints[result.waypoints.Count - 1].trackId);
        }
        finally
        {
            Object.DestroyImmediate(splineGo);
        }
    }

    [Test]
    public void Projector_FacingAppliedAfterIdentity()
    {
        var splineGo = NewSpline();
        try
        {
            var spline = splineGo.GetComponent<VehicleRoadCenterSpline>();
            var car = Frame(0, 4, 2, "car", 0.7f, 0.4f, 0.25f);
            var bike = Frame(0, 5, 3, "motorcycle", 0.3f, 0.4f, 0.2f);
            var bound0 = VehicleTrackProjector.ContinueIdentity(
                4, 2, 0f, new Vector2(0.7f, 0.4f), new[] { car, bike }, spline, 0f);
            var bound90 = VehicleTrackProjector.ContinueIdentity(
                4, 2, 0f, new Vector2(0.7f, 0.4f), new[] { car, bike }, spline, 180f);
            Assert.AreEqual(4, bound0.trackId);
            Assert.AreEqual(4, bound90.trackId);

            Vector3 a = VehicleTrackProjector.UnprojectCentroid(car.cx, car.cy, spline, 0f);
            Vector3 b = VehicleTrackProjector.UnprojectCentroid(car.cx, car.cy, spline, 90f);
            Assert.Greater((a - b).sqrMagnitude, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(splineGo);
        }
    }

    [Test]
    public void Baker_EmitsSeedAndDriveWaypoint()
    {
        var host = new GameObject("vehHost");
        var splineGo = NewSpline();
        try
        {
            var vehicle = host.AddComponent<VehicleActor>();
            var spline = splineGo.GetComponent<VehicleRoadCenterSpline>();
            var track = LocalStubVehicleTrackDetector.Detect("", VehicleVideoSteeringIds.Yolo26IntelSpec);
            var proj = VehicleTrackProjector.Project(track, spline, 0f);
            Assert.Greater(proj.waypoints.Count, 0);
            var baked = VehicleSteeringBtBaker.Bake(host.transform, proj, vehicle, null);
            Assert.IsNotNull(baked.tree);
            Assert.IsNotNull(baked.seed);
            Assert.GreaterOrEqual(baked.driveWaypointCount, 1);
            Assert.IsNotNull(baked.root);
            Assert.IsTrue(baked.root.children.Exists(c => c is SeedVehicleVelocityNode));
            Assert.IsTrue(baked.root.children.Exists(c => c is TravelLegDriveNode));
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(splineGo);
        }
    }

    [Test]
    public void SeedVelocity_AppliesToRigidbody()
    {
        var go = new GameObject("rbSeed");
        try
        {
            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            var node = go.AddComponent<SeedVehicleVelocityNode>();
            node.body = rb;
            node.linearVelocity = new Vector3(0f, 0f, 6f);
            node.angularVelocity = new Vector3(0f, 0.2f, 0f);
            Assert.AreEqual(BehaviorTreeStatus.Success, node.Execute(null));
            Assert.AreEqual(6f, rb.linearVelocity.z, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void DriveNode_RoutesViaProxyWhenBound()
    {
        var source = new GameObject("src");
        var remote = new GameObject("remote");
        try
        {
            remote.transform.position = Vector3.zero;
            var remoteBody = remote.AddComponent<Rigidbody>();
            remoteBody.useGravity = false;
            var remoteActor = remote.AddComponent<VehicleActor>();

            var proxy = source.AddComponent<VehicleInstrumentPhysicsProxy>();
            var map = ScriptableObject.CreateInstance<VehicleInstrumentMap>();
            map.ReplaceSlots(new List<VehicleInstrumentSlot>
            {
                new VehicleInstrumentSlot { id = "steer", impulseChannelKey = "vehicle_steering" }
            });
            proxy.sourceMap = map;
            proxy.bindings = new List<VehicleInstrumentBinding>
            {
                new VehicleInstrumentBinding
                {
                    localSurfaceId = "steer",
                    remoteVehicle = remoteActor,
                    remoteSurfaceId = "steer",
                    maxForceNewtons = 400f,
                    localForceAxis = Vector3.right
                }
            };

            var node = source.AddComponent<TravelLegDriveNode>();
            node.instrumentProxy = proxy;
            node.steerHintSigned01 = 1f;
            node.speedHint = 4f;
            node.waypoint = Vector3.forward * 4f;
            Assert.IsTrue(node.TryRouteInstrumentProxy(null, 0.02f));
            Physics.Simulate(0.02f);
            Assert.Greater(remoteBody.linearVelocity.sqrMagnitude + remoteBody.angularVelocity.sqrMagnitude, 0f);

            Object.DestroyImmediate(map);
        }
        finally
        {
            Object.DestroyImmediate(source);
            Object.DestroyImmediate(remote);
        }
    }

    [Test]
    public void Metadata_RoundTripsCabinCameraAndPolarPath()
    {
        var asset = ScriptableObject.CreateInstance<WebcamAnimRecordingAsset>();
        try
        {
            asset.kind = WebcamAnimKind.Vehicle;
            asset.cabinCamera = true;
            asset.inferShoulderShifts = true;
            asset.polarVelocityPath = "/tmp/cabin.polar.json";
            asset.modelSpec = VehicleVideoSteeringIds.CabinCompositeSpec;
            var meta = asset.ToTypeMetadata();
            Assert.IsTrue(meta.cabinCamera);
            Assert.IsTrue(meta.inferShoulderShifts);
            Assert.AreEqual("/tmp/cabin.polar.json", meta.polarVelocityPath);

            var other = ScriptableObject.CreateInstance<WebcamAnimRecordingAsset>();
            try
            {
                other.ApplyTypeMetadata(meta);
                Assert.IsTrue(other.cabinCamera);
                Assert.IsTrue(other.inferShoulderShifts);
                Assert.AreEqual(VehicleVideoSteeringIds.CabinCompositeSpec, other.modelSpec);
                Assert.AreEqual("/tmp/cabin.polar.json", other.polarVelocityPath);
            }
            finally
            {
                Object.DestroyImmediate(other);
            }
        }
        finally
        {
            Object.DestroyImmediate(asset);
        }
    }

    [Test]
    public void Solver_ForwardResidualMapsToThrottleStub()
    {
        var pose = CabinPose(hipsZ: 0f, shoulderZ: -0.08f, handX: 0f);
        var polar = SteadyPolar(5f);
        var hints = CabinPoseInstrumentSolver.Evaluate(pose, 0f, polar, inferShoulderShifts: true);
        Assert.AreEqual(CabinPedalIntent.Throttle, hints.pedal);
        Assert.Greater(hints.throttle01, 0.1f);
        Assert.Greater(hints.residualLean, CabinPoseInstrumentSolver.ResidualDeadzone);
        var card = CabinPoseInstrumentSolver.CardFor(hints);
        Assert.AreEqual("stub_drive_throttle", card.sectionName);
    }

    [Test]
    public void Solver_ShoulderCheckboxSubtractsPolarAccel()
    {
        var pose = CabinPose(hipsZ: 0f, shoulderZ: -0.08f, handX: 0f);
        var polar = new CabinPolarVelocity
        {
            frames = new[]
            {
                new CabinPolarFrame { tMs = 0, speedHint = 2f },
                new CabinPolarFrame { tMs = 200, speedHint = 6f }
            }
        };
        var withShoulder = CabinPoseInstrumentSolver.Evaluate(pose, 0f, polar, inferShoulderShifts: true);
        var envelope = CabinPoseInstrumentSolver.Evaluate(pose, 0f, polar, inferShoulderShifts: false);
        Assert.AreEqual(CabinPedalIntent.Brake, withShoulder.pedal);
        Assert.AreEqual(CabinPedalIntent.Throttle, envelope.pedal);
        Assert.Less(withShoulder.residualLean, -CabinPoseInstrumentSolver.ResidualDeadzone);
    }

    [Test]
    public void Solver_FeetOverrideInferredPedals()
    {
        var pose = new PoseTrack { modelSpec = "mediapipe_holistic@v1" };
        pose.samples.Add(Bone("Human:Hips", 0f, 0f, 0f));
        pose.samples.Add(Bone("Human:LeftShoulder", 0f, 0.4f, 0.12f));
        pose.samples.Add(Bone("Human:RightShoulder", 0f, 0.4f, 0.12f));
        pose.samples.Add(Bone("Human:RightFoot", 0.1f, 0f, -0.16f));
        var polar = SteadyPolar(5f);
        var hints = CabinPoseInstrumentSolver.Evaluate(pose, 0f, polar, inferShoulderShifts: true);
        Assert.IsTrue(hints.footOverride);
        Assert.AreEqual(CabinPedalIntent.Throttle, hints.pedal);
    }

    [Test]
    public void Projector_CabinCameraDoesNotPickYoloAsEgo()
    {
        var splineGo = NewSpline();
        try
        {
            var spline = splineGo.GetComponent<VehicleRoadCenterSpline>();
            var track = LocalStubVehicleTrackDetector.Detect("", VehicleVideoSteeringIds.Yolo26IntelSpec);
            var result = VehicleTrackProjector.Project(track, spline, 0f, cabinCamera: true);
            Assert.AreEqual(0, result.waypoints.Count);
            Assert.AreEqual(0, result.subjectTrackId);
        }
        finally
        {
            Object.DestroyImmediate(splineGo);
        }
    }

    [Test]
    public void Baker_CabinSeedsVelocityFromPolarNotYoloBbox()
    {
        var host = new GameObject("cabinHost");
        var splineGo = NewSpline();
        try
        {
            var vehicle = host.AddComponent<VehicleActor>();
            var spline = splineGo.GetComponent<VehicleRoadCenterSpline>();
            var yoloProj = VehicleTrackProjector.Project(
                LocalStubVehicleTrackDetector.Detect("", VehicleVideoSteeringIds.Yolo26IntelSpec), spline, 0f);
            Assert.Greater(yoloProj.waypoints.Count, 0);
            var polar = new CabinPolarVelocity
            {
                frames = new[]
                {
                    new CabinPolarFrame { tMs = 0, speedHint = 11f, yawRateHint = 0.3f },
                    new CabinPolarFrame { tMs = 200, speedHint = 11f, yawRateHint = 0.3f }
                }
            };
            var baked = VehicleSteeringBtBaker.Bake(host.transform, yoloProj, vehicle, null, polar);
            Assert.IsNotNull(baked.seed);
            Assert.AreEqual(11f, baked.seed.linearVelocity.z, 0.05f);
            Assert.AreEqual(0.3f, baked.seed.angularVelocity.y, 0.01f);
            Assert.Greater(Mathf.Abs(yoloProj.seedVelocity.z - baked.seed.linearVelocity.z), 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(splineGo);
        }
    }

    [Test]
    public void Baker_SeatsOccupantOnVehicleAnchor()
    {
        var host = new GameObject("seatHost");
        var occ = new GameObject("occupant");
        try
        {
            var vehicle = host.AddComponent<VehicleActor>();
            var seating = host.AddComponent<VehicleSeating>();
            var anchorGo = new GameObject("anchor");
            anchorGo.transform.SetParent(host.transform, false);
            anchorGo.transform.position = new Vector3(1.5f, 0.8f, 0.4f);
            seating.occupantAnchors = new[] { anchorGo.transform };
            occ.transform.position = Vector3.zero;
            var polar = CabinPolarVelocity.Stub();
            var proj = VehicleTrackProjector.ProjectPolar(polar, Vector3.zero, Vector3.forward);
            VehicleSteeringBtBaker.Bake(host.transform, proj, vehicle, null, polar, occ.transform);
            Assert.AreEqual(anchorGo.transform.position, occ.transform.position);
        }
        finally
        {
            Object.DestroyImmediate(host);
            Object.DestroyImmediate(occ);
        }
    }

    [Test]
    public void Polar_ToSeedSlotUsesForwardSpeedHint()
    {
        var polar = new CabinPolarVelocity
        {
            frames = new[] { new CabinPolarFrame { tMs = 0, speedHint = 8f, yawRateHint = 0.1f } }
        };
        var slot = polar.ToSeedSlot(Vector3.forward, Vector3.up);
        Assert.IsTrue(slot.hasVelocity);
        Assert.AreEqual(8f, slot.linearVelocity.z, 0.01f);
        Assert.AreEqual(0.1f, slot.angularVelocity.y, 0.01f);
    }

    [Test]
    public void ApplyToTravelAgent_WritesDriveWaypoints()
    {
        var splineGo = NewSpline();
        var agentGo = new GameObject("ta");
        try
        {
            var spline = splineGo.GetComponent<VehicleRoadCenterSpline>();
            var agent = agentGo.AddComponent<TravelAgent>();
            var track = LocalStubVehicleTrackDetector.Detect("", VehicleVideoSteeringIds.Yolo26IntelSpec);
            var proj = VehicleTrackProjector.Project(track, spline, 0f);
            VehicleTrackProjector.ApplyToTravelAgent(proj, agent);
            Assert.Greater(agent.authoringRows.Count, 0);
            Assert.IsFalse(agent.CachedPlan.IsEmpty);
            Assert.AreEqual(TravelLegMode.Drive, agent.CachedPlan.segments[0].mode);
        }
        finally
        {
            Object.DestroyImmediate(splineGo);
            Object.DestroyImmediate(agentGo);
        }
    }

    static GameObject NewSpline()
    {
        var go = new GameObject("spline");
        var spline = go.AddComponent<VehicleRoadCenterSpline>();
        spline.controlPoints = new List<Vector3>
        {
            Vector3.zero,
            new Vector3(0f, 0f, 8f),
            new Vector3(0f, 0f, 16f)
        };
        spline.RebuildLengthTable();
        return go;
    }

    static VehicleTrackFrame Frame(double t, int id, int classId, string name, float cx, float cy, float half)
    {
        return new VehicleTrackFrame
        {
            tMs = t,
            trackId = id,
            classId = classId,
            className = name,
            conf = 0.9f,
            cx = cx,
            cy = cy,
            bbox = new VehicleTrackBBox
            {
                x1 = cx - half,
                y1 = cy - half * 0.7f,
                x2 = cx + half,
                y2 = cy + half * 0.7f
            }
        };
    }

    static VehicleTrackSegment Seg(double a, double b, int id, int classId)
    {
        return new VehicleTrackSegment
        {
            startMs = a,
            endMs = b,
            subjectTrackId = id,
            subjectClassId = classId
        };
    }

    static PoseTrack CabinPose(float hipsZ, float shoulderZ, float handX)
    {
        var pose = new PoseTrack { modelSpec = "mediapipe_holistic@v1" };
        pose.samples.Add(Bone("Human:Hips", 0f, 0f, hipsZ));
        pose.samples.Add(Bone("Human:LeftShoulder", -0.15f, 0.4f, shoulderZ));
        pose.samples.Add(Bone("Human:RightShoulder", 0.15f, 0.4f, shoulderZ));
        pose.samples.Add(Bone("Human:LeftHand", handX - 0.1f, 0.3f, 0.1f));
        pose.samples.Add(Bone("Human:RightHand", handX + 0.1f, 0.3f, 0.1f));
        return pose;
    }

    static PoseBoneSample Bone(string id, float x, float y, float z)
    {
        return new PoseBoneSample
        {
            traitId = id,
            timeMs = 0f,
            localPosition = new Vector3(x, y, z),
            localRotation = Quaternion.identity
        };
    }

    static CabinPolarVelocity SteadyPolar(float speed)
    {
        return new CabinPolarVelocity
        {
            frames = new[]
            {
                new CabinPolarFrame { tMs = 0, speedHint = speed },
                new CabinPolarFrame { tMs = 200, speedHint = speed }
            }
        };
    }

    [Test]
    public void TrySample_LerpsWaypointAtPlayhead()
    {
        var result = new VehicleProjectionResult();
        result.waypoints.Add(new VehicleProjectedWaypoint
        {
            world = Vector3.zero, tMs = 0, tangent = Vector3.forward, s = 0
        });
        result.waypoints.Add(new VehicleProjectedWaypoint
        {
            world = new Vector3(0, 0, 10), tMs = 1000, tangent = Vector3.forward, s = 10
        });
        Assert.IsTrue(VehicleTrackProjector.TrySample(result, 500, out var wp));
        Assert.AreEqual(5f, wp.world.z, 0.01f);
        Assert.AreEqual(5f, wp.s, 0.01f);
    }

    [Test]
    public void VehicleRagdollSync_MovesChassisToSample()
    {
        var go = new GameObject("veh");
        var vehicle = go.AddComponent<VehicleActor>();
        try
        {
            var result = new VehicleProjectionResult();
            result.waypoints.Add(new VehicleProjectedWaypoint
            {
                world = new Vector3(3, 0, 4), tMs = 0, tangent = Vector3.forward
            });
            int n = WebcamAnimVehicleRagdollSync.Apply(null, 0f, vehicle, null, null, result);
            Assert.Greater(n, 0);
            Assert.AreEqual(3f, vehicle.transform.position.x, 0.01f);
            Assert.AreEqual(4f, vehicle.transform.position.z, 0.01f);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }
}
#endif
