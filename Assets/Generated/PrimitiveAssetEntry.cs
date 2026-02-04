using System;
using UnityEngine;

/// <summary>
/// Type of primitive asset stored for pipeline inputs.
/// </summary>
public enum PrimitiveAssetType
{
    Prompt,
    Image,
    Video,
    Sound
}

/// <summary>
/// Single entry in the primitive asset store: key, type, path/ref, optional thumbnail, timestamp.
/// Used so every pipeline has traceable inputs and reuse.
/// </summary>
[Serializable]
public class PrimitiveAssetEntry
{
    [Tooltip("Stable key for lookup (e.g. 'intro_prompt', 'ref_sketch').")]
    public string key = "";

    [Tooltip("Type of primitive.")]
    public PrimitiveAssetType type = PrimitiveAssetType.Prompt;

    [Tooltip("Asset path (e.g. Assets/Generated/Primitives/ref.png). For Prompt, may be path to .txt or empty if promptText is set.")]
    public string path = "";

    [Tooltip("For Prompt type: inline text. Otherwise empty.")]
    [TextArea(1, 4)]
    public string promptText = "";

    [Tooltip("Optional thumbnail bytes (e.g. PNG).")]
    public byte[] thumbnail;

    [Tooltip("Timestamp ticks (DateTime.UtcTicks) or 0.")]
    public long timestampTicks;

    public PrimitiveAssetEntry() { }

    public PrimitiveAssetEntry(string key, PrimitiveAssetType type, string path, string promptText = null, byte[] thumbnail = null)
    {
        this.key = key ?? "";
        this.type = type;
        this.path = path ?? "";
        this.promptText = promptText ?? "";
        this.thumbnail = thumbnail;
        this.timestampTicks = DateTime.UtcNow.Ticks;
    }
}
