using System;
using UnityEngine;

/// <summary>
/// Single entry in a generator's history: prompt, reference to generated asset, timestamp, optional metadata.
/// Serializable for persistence on generator ScriptableObjects.
/// </summary>
[Serializable]
public class GeneratedResultEntry
{
    [Tooltip("Prompt used for this generation.")]
    public string prompt = "";

    [Tooltip("Asset path (e.g. Assets/Generated/Texture/out.png) for persistence. Used when generatedAsset is null after load.")]
    public string generatedAssetPath = "";

    [Tooltip("Reference to the generated asset (Texture2D, AudioClip, GameObject prefab, etc.). May be null; use path then.")]
    public UnityEngine.Object generatedAsset;

    [Tooltip("Timestamp ticks (DateTime.UtcTicks) or 0 for legacy.")]
    public long timestampTicks;

    [Tooltip("Model or pipeline used (e.g. LM Studio model id, Barracuda path).")]
    public string modelUsed = "";

    [Tooltip("Optional thumbnail bytes (e.g. PNG). For list preview.")]
    public byte[] thumbnail;

    [Tooltip("Keys of source primitives that produced this result (e.g. prompt key, image key).")]
    public System.Collections.Generic.List<string> sourcePrimitiveKeys = new System.Collections.Generic.List<string>();

    [Tooltip("ORM/asset keys this result depends on (e.g. albedo map key, normal map key). Used for shaders to list required texture maps.")]
    public System.Collections.Generic.List<string> assetDependencyKeys = new System.Collections.Generic.List<string>();

    /// <summary>Display timestamp string from timestampTicks.</summary>
    public string TimestampString
    {
        get
        {
            if (timestampTicks == 0) return "";
            try
            {
                var dt = new DateTime(timestampTicks, DateTimeKind.Utc);
                return dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch { return ""; }
        }
    }

    public GeneratedResultEntry() { }

    public GeneratedResultEntry(string prompt, string assetPath, UnityEngine.Object asset, string modelUsed = null)
    {
        this.prompt = prompt ?? "";
        this.generatedAssetPath = assetPath ?? "";
        this.generatedAsset = asset;
        this.timestampTicks = DateTime.UtcNow.Ticks;
        this.modelUsed = modelUsed ?? "";
    }
}
