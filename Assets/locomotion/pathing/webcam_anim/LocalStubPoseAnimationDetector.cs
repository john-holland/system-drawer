using UnityEngine;

/// <summary>Fast local iteration stub — synthetic identity track, no model weights.</summary>
public sealed class LocalStubPoseAnimationDetector : IPoseAnimationDetector
{
    public const string DetectorId = "local-stub";

    public string Id => DetectorId;

    public PoseTrack Detect(string sourcePathOrUrl, string modelSpec)
    {
        var track = new PoseTrack { modelSpec = modelSpec ?? "" };
        string[] traits = { "Human:Hips", "Human:Head", "Vehicle:Root", "Misc:Root" };
        for (int i = 0; i < traits.Length; i++)
        {
            track.samples.Add(new PoseBoneSample
            {
                traitId = traits[i],
                timeMs = 0f,
                localPosition = Vector3.zero,
                localRotation = Quaternion.identity
            });
        }
        _ = sourcePathOrUrl;
        return track;
    }
}
