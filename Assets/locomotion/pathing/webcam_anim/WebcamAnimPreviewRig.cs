using System.Collections.Generic;
using Locomotion.Camera;
using Locomotion.Rig;
using UnityEngine;

/// <summary>Optional test-scene host: overlay camera, camera list, vehicle ragdoll sync for recording preview.</summary>
[AddComponentMenu("Locomotion/Animation/Webcam Anim Preview Rig")]
public sealed class WebcamAnimPreviewRig : MonoBehaviour
{
    public WebcamAnimRecordingAsset recording;
    public UnityEngine.Camera overlayCamera;
    public List<UnityEngine.Camera> cameras = new List<UnityEngine.Camera>();
    public VehicleActor vehicle;
    public Transform occupant;
    public RagdollIKAnimationManager ik;
    public CameraPathingRig pathingRig;
    public CameraTransitionController transition;
    public bool driveInPlayMode;

    public readonly WebcamAnimTimeScrubber scrubber = new WebcamAnimTimeScrubber();

    int _lastShot = -1;
    VehicleProjectionResult _projection;

    public UnityEngine.Camera[] CameraArray()
    {
        if (cameras == null || cameras.Count == 0) return System.Array.Empty<UnityEngine.Camera>();
        return cameras.ToArray();
    }

    public void SetProjection(VehicleProjectionResult projection) => _projection = projection;

    public BoneMap ResolveMap()
    {
        if (ik == null) return null;
        var actor = ik.GetRagdollActorTransform();
        if (actor == null) return null;
        return actor.GetComponent<BoneMap>() ?? actor.GetComponentInChildren<BoneMap>();
    }

    public void TickPlayback(float deltaTime)
    {
        if (recording == null) return;
        scrubber.Bind(recording);
        scrubber.Tick(deltaTime);
        if (recording.syncVehicleRagdoll)
        {
            WebcamAnimVehicleRagdollSync.Apply(
                recording, (float)scrubber.playheadMs, vehicle, occupant, ResolveMap(), _projection);
        }
        WebcamAnimCameraDirector.Apply(
            recording.cameraShots,
            CameraArray(),
            overlayCamera,
            pathingRig,
            transition,
            scrubber.playheadMs,
            deltaTime,
            ref _lastShot);
    }

    void LateUpdate()
    {
        if (driveInPlayMode)
            TickPlayback(Time.deltaTime);
    }
}
