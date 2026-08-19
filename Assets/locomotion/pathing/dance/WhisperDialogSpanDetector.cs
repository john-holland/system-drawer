using UnityEngine;

/// <summary>
/// Dialog ASR via Whisper. V1 no-ops (model_spec ready); USC /api/media transcript is the future hop.
/// </summary>
public sealed class WhisperDialogSpanDetector : IAudioSpanDetector
{
    public const string DetectorId = "whisper-dialog";
    public const string DefaultModelSpec = "whisper@base";

    public string Id => DetectorId;

    public DanceMediaSpan[] Detect(string sourcePathOrUrl, string modelSpec)
    {
        Debug.Log(
            $"[WhisperDialogSpanDetector] Unity stub (no Whisper weights). Continuuuum hops USC /api/media for whisper@base. modelSpec={modelSpec} src={sourcePathOrUrl}");
        if (string.IsNullOrEmpty(sourcePathOrUrl))
            return System.Array.Empty<DanceMediaSpan>();
        var stub = new LocalStubAudioSpanDetector();
        var spans = stub.Detect(sourcePathOrUrl, string.IsNullOrEmpty(modelSpec) ? DefaultModelSpec : modelSpec);
        for (int i = 0; i < spans.Length; i++)
            spans[i].label = "dialog " + spans[i].label;
        return spans;
    }
}
