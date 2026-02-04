using UnityEngine;

/// <summary>
/// Dynamic generator for audio. Output: AudioClip.
/// </summary>
[CreateAssetMenu(fileName = "DynamicAudioGenerator", menuName = "Generated/Dynamic Audio Generator", order = 3)]
public class DynamicAudioGenerator : DynamicGeneratorBase
{
    [Header("Audio params")]
    [Tooltip("Length in seconds.")]
    public float lengthSeconds = 5f;
    [Tooltip("Sample rate.")]
    public int sampleRate = 44100;
    [Tooltip("Channels (1 = mono, 2 = stereo).")]
    public int channels = 2;

    public override string GeneratorTypeName => "Audio";
}
