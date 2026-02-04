using UnityEngine;

/// <summary>
/// Side-load paths per adapter type. Artists can point to chosen free models without code changes.
/// No built-in vendor; defaults can point to open models users download and attach to the project.
/// </summary>
public class GenerationAdapterConfig : ScriptableObject
{
    [Header("3D generation (prompt → 3D object/character)")]
    [Tooltip("Model or tool path (directory, Hugging Face model id, or local server URL).")]
    public string modelPath3D = "";

    [Header("Image → 3D character")]
    [Tooltip("Model or tool path for image-to-3D-character.")]
    public string modelPathImageTo3DCharacter = "";

    [Header("Video → animation")]
    [Tooltip("Model or tool path for video-to-motion.")]
    public string modelPathVideoToAnimation = "";

    [Header("Prompt → motion (text-to-motion)")]
    [Tooltip("Model or tool path for text-to-motion.")]
    public string modelPathPromptToMotion = "";

    [Header("Sound → ML (SFX or respeaking)")]
    [Tooltip("Model or tool path for sound effect generation/processing.")]
    public string modelPathSoundEffect = "";
    [Tooltip("Model or tool path for respeaking / TTS / voice.")]
    public string modelPathRespeaking = "";
}
