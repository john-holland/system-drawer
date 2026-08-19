/// <summary>Pluggable pose / motion detector. Real models later via model_spec.</summary>
public interface IPoseAnimationDetector
{
    string Id { get; }

    PoseTrack Detect(string sourcePathOrUrl, string modelSpec);
}
