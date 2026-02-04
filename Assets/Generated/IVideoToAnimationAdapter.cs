/// <summary>
/// Adapter for video → animation. Model path from config; input video path; return clip path or motion file path.
/// </summary>
public interface IVideoToAnimationAdapter
{
    /// <summary>Extract animation from video. modelPath from config; videoPath from primitive store. Returns path to AnimationClip or motion file.</summary>
    string Generate(string videoPath, string modelPath, out string error);
}
