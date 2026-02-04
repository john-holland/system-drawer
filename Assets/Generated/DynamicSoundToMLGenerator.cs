using UnityEngine;

/// <summary>
/// Generator for sound → ML: sound effect or respeaking. Uses soundKey from primitive store.
/// For respeaking (dubbing, TTS), include a text script.
/// </summary>
[CreateAssetMenu(fileName = "DynamicSoundToMLGenerator", menuName = "Generated/Dynamic Sound to ML Generator", order = 8)]
public class DynamicSoundToMLGenerator : DynamicGeneratorBase
{
    public enum SoundMLMode
    {
        SoundEffect,
        Respeaking
    }

    [Header("ML mode")]
    [Tooltip("Sound effect (generate/process SFX) or Respeaking (dubbing, voice conversion, TTS).")]
    public SoundMLMode mode = SoundMLMode.SoundEffect;

    [Tooltip("For respeaking: text script to use (e.g. dialogue to speak).")]
    [TextArea(2, 6)]
    public string textScript = "";

    [Header("Output")]
    [Tooltip("Length in seconds for generated audio.")]
    public float lengthSeconds = 5f;

    public override string GeneratorTypeName => "Sound to ML";
}
