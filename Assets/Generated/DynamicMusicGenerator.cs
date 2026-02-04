using UnityEngine;

/// <summary>
/// Dynamic generator for music. Output: AudioClip.
/// </summary>
[CreateAssetMenu(fileName = "DynamicMusicGenerator", menuName = "Generated/Dynamic Music Generator", order = 4)]
public class DynamicMusicGenerator : DynamicGeneratorBase
{
    [Header("Music params")]
    [Tooltip("BPM.")]
    public float bpm = 120f;
    [Tooltip("Key (e.g. C major).")]
    public string key = "C";
    [Tooltip("Length in seconds.")]
    public float lengthSeconds = 30f;
    [Tooltip("Style tag for model.")]
    public string style = "";

    public override string GeneratorTypeName => "Music";
}
