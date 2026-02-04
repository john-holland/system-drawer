using UnityEngine;

/// <summary>
/// Stub sound-to-ML adapter. Replace with real side-loadable tool (e.g. OpenVoice, AudioLDM) via config path.
/// </summary>
[CreateAssetMenu(fileName = "StubSoundToMLAdapter", menuName = "Generated/Adapters/Stub Sound to ML Adapter", order = 3)]
public class StubSoundToMLAdapter : ScriptableObject, ISoundToMLAdapter
{
    public string Process(string inputAudioPath, string modelPath, bool isRespeaking, string optionalTextScript, out string error)
    {
        error = null;
        return null;
    }
}
