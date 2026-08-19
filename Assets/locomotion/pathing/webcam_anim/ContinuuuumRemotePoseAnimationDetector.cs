using System.IO;
using UnityEngine;

/// <summary>
/// Loads a Continuuuum PoseTrack JSON (local path or recording poseTrackPath). No local MediaPipe weights.
/// </summary>
public sealed class ContinuuuumRemotePoseAnimationDetector : IPoseAnimationDetector
{
    public const string DetectorId = "continuuuum-remote";

    public string Id => DetectorId;

    public string ApiBaseUrl;

    public PoseTrack Detect(string sourcePathOrUrl, string modelSpec)
    {
        var track = TryLoadJson(sourcePathOrUrl);
        if (track != null && track.Count > 0)
        {
            if (string.IsNullOrEmpty(track.modelSpec))
                track.modelSpec = modelSpec ?? "";
            return track;
        }

        Debug.Log(
            $"[ContinuuuumRemotePoseAnimationDetector] no PoseTrack JSON at '{sourcePathOrUrl}'. " +
            $"modelSpec={modelSpec} api={ApiBaseUrl}");
        return new PoseTrack { modelSpec = modelSpec ?? "" };
    }

    public static PoseTrack TryLoadJson(string pathOrUrl)
    {
        if (string.IsNullOrEmpty(pathOrUrl))
            return null;
        string path = pathOrUrl;
        if (path.StartsWith("file://"))
            path = path.Substring(7);
        if (!File.Exists(path))
        {
            if (File.Exists(path + ".posetrack.json"))
                path += ".posetrack.json";
            else if (File.Exists(Path.ChangeExtension(path, ".posetrack.json")))
                path = Path.ChangeExtension(path, ".posetrack.json");
            else
                return null;
        }
        if (!path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
            return null;
        return PoseTrack.FromJson(File.ReadAllText(path));
    }
}
