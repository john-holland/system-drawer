using UnityEngine;

/// <summary>Authorable webcam / video IK interpretation take (ambulatory, vehicle, dance, misc).</summary>
[CreateAssetMenu(fileName = "WebcamAnimRecording", menuName = "Locomotion/Animation/Webcam Anim Recording")]
public sealed class WebcamAnimRecordingAsset : ScriptableObject
{
    public string recordingId;
    public string displayName = "New Recording";
    public WebcamAnimKind kind = WebcamAnimKind.Ambulatory;
    public GameObject actorPrefab;
    public int animationListIndex;
    public string modelSpec = "";
    public string subsectionId = "";
    public string localClipPath = "";
    public string libraryDocId = "";
    public string mediaJobId = "";
    public double startMs;
    public double endMs = 1000;
    public WebcamAnimTimelineGranularity granularity = WebcamAnimTimelineGranularity.Millisecond;
    public string targetHint = "ragdoll";
    public string species = "";
    public string poseTrackPath = "";
    public string vehicleTrackPath = "";
    public string polarVelocityPath = "";
    [Range(0f, 360f)] public float facingYawDegrees;
    public bool cabinCamera;
    public bool inferShoulderShifts;
    public PoseTrack lastTrack;
    public VehicleTrack lastVehicleTrack;
    public CabinPolarVelocity lastPolarVelocity;

    [Header("Preview overlay")]
    [Range(0f, 1f)] public float previewVideoOpacity = WebcamAnimPreviewOverlay.DefaultVideoOpacity;
    public float previewVideoScale = 1f;
    public Vector2 previewVideoOffset01;
    public bool syncVehicleRagdoll = true;
    public WebcamAnimCameraShot[] cameraShots = System.Array.Empty<WebcamAnimCameraShot>();

    void OnValidate()
    {
        if (string.IsNullOrEmpty(recordingId))
            recordingId = name;
        if (string.IsNullOrEmpty(displayName))
            displayName = name;
        if (endMs < startMs)
            endMs = startMs;
        startMs = WebcamAnimTimelineGranularityUtil.SnapMs(startMs, granularity);
        endMs = WebcamAnimTimelineGranularityUtil.SnapMs(endMs, granularity);
    }

    public WebcamAnimTypeMetadata ToTypeMetadata() => WebcamAnimTypeMetadata.FromRecording(this);

    public void ApplyTypeMetadata(WebcamAnimTypeMetadata meta)
    {
        if (meta == null)
            return;
        modelSpec = meta.model_spec ?? "";
        subsectionId = meta.subsection ?? "";
        animationListIndex = meta.animationListIndex;
        startMs = meta.timelineStartMs;
        endMs = meta.timelineEndMs;
        targetHint = meta.targetHint ?? targetHint;
        if (!string.IsNullOrEmpty(meta.species))
            species = meta.species;
        if (!string.IsNullOrEmpty(meta.poseTrackPath))
            poseTrackPath = meta.poseTrackPath;
        if (!string.IsNullOrEmpty(meta.vehicleTrackPath))
            vehicleTrackPath = meta.vehicleTrackPath;
        if (!string.IsNullOrEmpty(meta.polarVelocityPath))
            polarVelocityPath = meta.polarVelocityPath;
        facingYawDegrees = meta.facingYawDegrees;
        cabinCamera = meta.cabinCamera;
        inferShoulderShifts = meta.inferShoulderShifts;
        if (System.Enum.TryParse(meta.webcamAnimKind, true, out WebcamAnimKind parsedKind))
            kind = parsedKind;
        if (!string.IsNullOrEmpty(meta.granularity) &&
            System.Enum.TryParse(meta.granularity, true, out WebcamAnimTimelineGranularity parsedG))
            granularity = parsedG;
    }
}
