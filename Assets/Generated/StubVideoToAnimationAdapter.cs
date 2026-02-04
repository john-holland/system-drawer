using UnityEngine;

/// <summary>
/// Stub video-to-animation adapter. Replace with real side-loadable tool (e.g. DWPose, EDGE) via config path.
/// </summary>
[CreateAssetMenu(fileName = "StubVideoToAnimationAdapter", menuName = "Generated/Adapters/Stub Video to Animation Adapter", order = 2)]
public class StubVideoToAnimationAdapter : ScriptableObject, IVideoToAnimationAdapter
{
    public string Generate(string videoPath, string modelPath, out string error)
    {
        error = null;
        return null;
    }
}
