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
    public PoseTrack lastTrack;

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
        if (System.Enum.TryParse(meta.webcamAnimKind, true, out WebcamAnimKind parsedKind))
            kind = parsedKind;
        if (!string.IsNullOrEmpty(meta.granularity) &&
            System.Enum.TryParse(meta.granularity, true, out WebcamAnimTimelineGranularity parsedG))
            granularity = parsedG;
    }
}
