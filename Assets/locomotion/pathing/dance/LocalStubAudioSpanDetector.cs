/// <summary>Fast local iteration stub — two synthetic spans, no model weights.</summary>
public sealed class LocalStubAudioSpanDetector : IAudioSpanDetector
{
    public const string DetectorId = "local-stub-audio";

    public string Id => DetectorId;

    public DanceMediaSpan[] Detect(string sourcePathOrUrl, string modelSpec)
    {
        _ = sourcePathOrUrl;
        string tag = string.IsNullOrEmpty(modelSpec) ? "stub" : modelSpec;
        return new[]
        {
            new DanceMediaSpan
            {
                startMs = 0,
                endMs = 2000,
                label = tag + " a",
                audioRef = sourcePathOrUrl ?? ""
            },
            new DanceMediaSpan
            {
                startMs = 2000,
                endMs = 4000,
                label = tag + " b",
                audioRef = sourcePathOrUrl ?? ""
            }
        };
    }
}
