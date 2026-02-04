/// <summary>
/// Adapter for sound → ML: sound effect or respeaking. Model path from config; input audio path; mode SFX or respeaking; return output audio path.
/// </summary>
public interface ISoundToMLAdapter
{
    /// <summary>Process sound: SFX (generate/process sound effect) or Respeaking (dubbing, voice conversion, TTS).</summary>
    string Process(string inputAudioPath, string modelPath, bool isRespeaking, string optionalTextScript, out string error);
}
