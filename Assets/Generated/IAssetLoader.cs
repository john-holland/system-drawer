using UnityEngine;

/// <summary>
/// Unified asset loader: resolve by key or by generator result to prefab, AnimationClip, or audio.
/// For primitives: resolve primitive key to path or asset ref (prompt text, image, video, sound).
/// </summary>
public interface IAssetLoader
{
    /// <summary>Resolve a key to a prefab (GameObject). Returns null if not found.</summary>
    GameObject ResolvePrefab(string key);

    /// <summary>Resolve a key to an AnimationClip. Returns null if not found.</summary>
    AnimationClip ResolveAnimationClip(string key);

    /// <summary>Resolve a key to an AudioClip. Returns null if not found.</summary>
    AudioClip ResolveAudioClip(string key);

    /// <summary>Resolve by generator result (e.g. from history). Returns prefab, clip, or audio depending on asset type.</summary>
    UnityEngine.Object ResolveFromResult(GeneratedResultEntry entry);

    /// <summary>Resolve a primitive key to its entry. Returns null if not found.</summary>
    PrimitiveAssetEntry ResolvePrimitive(string primitiveKey);

    /// <summary>Resolve a key to prompt text. Tries PromptRegistry (NarrativePromptAsset) then PrimitiveAssetStore (Prompt type). Returns null if not found.</summary>
    string ResolvePrompt(string key);

    /// <summary>Ensure generated prefab from entry is registered with SceneObjectRegistry when prebakeAndSave is true.</summary>
    void EnsureRegistered(DynamicGeneratorBase generator, GeneratedResultEntry entry);
}
