using System;
using UnityEngine;

/// <summary>JSON payload stored on USC library type_metadata (no new SQL columns).</summary>
[Serializable]
public sealed class WebcamAnimTypeMetadata
{
    public const string KindValue = "webcam_anim_recording";

    public string kind = KindValue;
    public string webcamAnimKind = "ambulatory";
    public string model_spec = "";
    public string subsection = "";
    public int animationListIndex;
    public double timelineStartMs;
    public double timelineEndMs;
    public string granularity = "millisecond";
    public string targetHint = "ragdoll";
    public string species = "";
    public string poseTrackPath = "";

    public static WebcamAnimTypeMetadata FromRecording(WebcamAnimRecordingAsset asset)
    {
        if (asset == null)
            return new WebcamAnimTypeMetadata();
        return new WebcamAnimTypeMetadata
        {
            kind = KindValue,
            webcamAnimKind = asset.kind.ToString().ToLowerInvariant(),
            model_spec = asset.modelSpec ?? "",
            subsection = asset.subsectionId ?? "",
            animationListIndex = asset.animationListIndex,
            timelineStartMs = asset.startMs,
            timelineEndMs = asset.endMs,
            granularity = WebcamAnimTimelineGranularityUtil.JsonName(asset.granularity),
            targetHint = asset.targetHint ?? "ragdoll",
            species = asset.species ?? "",
            poseTrackPath = asset.poseTrackPath ?? ""
        };
    }

    public string ToJson() => JsonUtility.ToJson(this);

    public static WebcamAnimTypeMetadata FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return new WebcamAnimTypeMetadata();
        try
        {
            var m = JsonUtility.FromJson<WebcamAnimTypeMetadata>(json);
            return m ?? new WebcamAnimTypeMetadata();
        }
        catch
        {
            return new WebcamAnimTypeMetadata();
        }
    }
}
