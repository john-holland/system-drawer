using UnityEngine;

/// <summary>Music-section analysis. V1 stub; model_spec reserved for a later Continuuuum hop.</summary>
public sealed class MusicAnalysisSpanDetector : IAudioSpanDetector
{
    public const string DetectorId = "music-analysis";
    public const string DefaultModelSpec = "music_analysis@stub";

    public string Id => DetectorId;

    public DanceMediaSpan[] Detect(string sourcePathOrUrl, string modelSpec)
    {
        Debug.Log(
            $"[MusicAnalysisSpanDetector] v1 stub. modelSpec={modelSpec} src={sourcePathOrUrl}");
        var stub = new LocalStubAudioSpanDetector();
        var spans = stub.Detect(sourcePathOrUrl, string.IsNullOrEmpty(modelSpec) ? DefaultModelSpec : modelSpec);
        for (int i = 0; i < spans.Length; i++)
            spans[i].label = "song " + spans[i].label;
        return spans;
    }
}
