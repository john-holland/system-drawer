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
    [Tooltip("When > 0, animation length is this many milliseconds. 0 = video length, then loaded animation/pose duration.")]
    public double userDurationLimitMs;
    [Tooltip("True after In/Out/Duration are typed; auto-fill from video or clip will not overwrite.")]
    public bool userSetTimeline;
    [Tooltip("Last prepared video length in milliseconds (0 if unknown).")]
    public double cachedVideoDurationMs;
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
        if (userDurationLimitMs < 0.0)
            userDurationLimitMs = 0.0;
        startMs = WebcamAnimTimelineGranularityUtil.SnapMs(startMs, granularity);
        endMs = WebcamAnimTimelineGranularityUtil.SnapMs(endMs, granularity);
        if (userDurationLimitMs > 0.0)
            userDurationLimitMs = WebcamAnimTimelineGranularityUtil.SnapMs(userDurationLimitMs, granularity);
    }

    public double DurationMs => System.Math.Max(1.0, endMs - startMs);

    public bool TimelineLooksDefault =>
        AnimationSpanDuration.LooksLikeDefaultRecordingSpan(startMs, endMs);

    /// <summary>
    /// Fill the take span from user limit, then video, then animation/pose file.
    /// Does not overwrite a user-typed In/Out/Duration.
    /// </summary>
    public void ApplyAutoDurationFromSources(double videoMs, double animationFileMs = 0)
    {
        if (userSetTimeline)
            return;
        bool hasVideo = videoMs > 0.0;
        if (hasVideo)
            cachedVideoDurationMs = videoMs;
        if (!TimelineLooksDefault && userDurationLimitMs <= 0.0 && !hasVideo)
            return;
        double resolved = AnimationSpanDuration.ResolveMs(
            userDurationLimitMs, videoMs, animationFileMs, DurationMs);
        endMs = startMs + System.Math.Max(1.0, resolved);
    }

    public void ApplyLoadedTrackDuration(double videoMs = 0)
    {
        double file = lastTrack != null ? lastTrack.LatestTimeMs() : 0.0;
        if (file <= 0.0 && lastVehicleTrack?.frames != null && lastVehicleTrack.frames.Length > 0)
            file = lastVehicleTrack.frames[lastVehicleTrack.frames.Length - 1].tMs;
        ApplyAutoDurationFromSources(videoMs > 0.0 ? videoMs : cachedVideoDurationMs, file);
    }

    public void SetUserInMs(double ms)
    {
        userSetTimeline = true;
        startMs = ms;
        if (endMs < startMs)
            endMs = startMs + 1.0;
    }

    public void SetUserOutMs(double ms)
    {
        userSetTimeline = true;
        endMs = ms;
        if (endMs < startMs)
            endMs = startMs;
    }

    public void SetUserDurationMs(double durationMs)
    {
        userSetTimeline = true;
        endMs = startMs + System.Math.Max(1.0, durationMs);
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
