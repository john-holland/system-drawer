using Locomotion.Narrative;
using UnityEngine;

/// <summary>
/// Dynamic generator for music. Output: AudioClip.
/// </summary>
[CreateAssetMenu(fileName = "DynamicMusicGenerator", menuName = "Generated/Dynamic Music Generator", order = 4)]
public class DynamicMusicGenerator : DynamicGeneratorBase, IProceduralAudioSource
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

    [Header("Section assembly")]
    [Tooltip("Stem role for RogueScroll-style layering.")]
    public int stemRole;
    [Range(0, 11)] public int harmonicHue;
    public int bars = 4;
    [Tooltip("Canonical key for transposition (default C).")]
    public string canonicalKey = "C";

    public AudioClip ResolveAudioClip()
    {
        var entry = GetCurrentResult();
        return entry?.generatedAsset as AudioClip;
    }

    public override string GeneratorTypeName => "Music";
}
