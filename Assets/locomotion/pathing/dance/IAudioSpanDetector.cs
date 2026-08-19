/// <summary>Pluggable dialog/music span detector. Real Whisper/analysis later via model_spec.</summary>
public interface IAudioSpanDetector
{
    string Id { get; }

    DanceMediaSpan[] Detect(string sourcePathOrUrl, string modelSpec);
}
