using System.Collections.Generic;
using UnityEngine;
using Locomotion.Narrative;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Unified asset loader: resolve by key or by generator result to prefab, AnimationClip, or audio.
/// Wires into SceneObjectRegistry for prefabs; holds optional clip/audio key-to-path mapping; uses PrimitiveAssetStore for primitives.
/// Implements IAnimationClipResolver so narrative can play generated animations by key.
/// </summary>
public class AssetLoader : MonoBehaviour, IAssetLoader, IAnimationClipResolver
{
    [Header("References")]
    [Tooltip("Scene object registry for prefab resolution.")]
    public SceneObjectRegistry sceneObjectRegistry;

    [Tooltip("Primitive asset store for resolving prompt/image/video/sound by key.")]
    public PrimitiveAssetStore primitiveAssetStore;

    [Tooltip("Optional. When set, ResolvePrompt tries this first (NarrativePromptAsset by key/synonym) before PrimitiveAssetStore.")]
    public PromptRegistry promptRegistry;

    [Header("Clip / audio key mapping (optional)")]
    [Tooltip("Keys for animation clips (e.g. ORM key + '_clip').")]
    public List<string> animationClipKeys = new List<string>();

    [Tooltip("Paths for animation clips (same order as animationClipKeys).")]
    public List<string> animationClipPaths = new List<string>();

    [Tooltip("Keys for audio clips.")]
    public List<string> audioClipKeys = new List<string>();

    [Tooltip("Paths for audio clips (same order as audioClipKeys).")]
    public List<string> audioClipPaths = new List<string>();

    private void Awake()
    {
        if (sceneObjectRegistry == null)
            sceneObjectRegistry = FindAnyObjectByType<SceneObjectRegistry>();
        if (primitiveAssetStore == null)
            primitiveAssetStore = Resources.Load<PrimitiveAssetStore>("PrimitiveAssetStore");
    }

    /// <summary>Resolve a key to a prefab (GameObject). Uses SceneObjectRegistry.</summary>
    public GameObject ResolvePrefab(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var registry = sceneObjectRegistry;
        if (registry == null) registry = FindAnyObjectByType<SceneObjectRegistry>();
        if (registry == null) return null;
        var entry = registry.GetCloneable(key);
        if (entry != null && entry.prefabForClone != null) return entry.prefabForClone;
        var go = registry.Resolve(key);
        return go;
    }

    /// <summary>Resolve a key to an AnimationClip. Uses animationClipKeys/Paths or generator result.</summary>
    public AnimationClip ResolveAnimationClip(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var path = GetPathForKey(animationClipKeys, animationClipPaths, key);
        if (!string.IsNullOrEmpty(path))
        {
            var clip = LoadAssetAtPath<AnimationClip>(path);
            if (clip != null) return clip;
        }
        return null;
    }

    /// <summary>Resolve a key to an AudioClip. Uses audioClipKeys/Paths.</summary>
    public AudioClip ResolveAudioClip(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var path = GetPathForKey(audioClipKeys, audioClipPaths, key);
        if (!string.IsNullOrEmpty(path))
        {
            var clip = LoadAssetAtPath<AudioClip>(path);
            if (clip != null) return clip;
        }
        return null;
    }

    /// <summary>Resolve by generator result. Returns the generated asset (prefab, AnimationClip, or AudioClip).</summary>
    public UnityEngine.Object ResolveFromResult(GeneratedResultEntry entry)
    {
        if (entry == null) return null;
        if (entry.generatedAsset != null) return entry.generatedAsset;
        if (string.IsNullOrEmpty(entry.generatedAssetPath)) return null;
        return LoadAssetAtPath<UnityEngine.Object>(entry.generatedAssetPath);
    }

    /// <summary>Resolve a primitive key to its entry.</summary>
    public PrimitiveAssetEntry ResolvePrimitive(string primitiveKey)
    {
        if (string.IsNullOrWhiteSpace(primitiveKey) || primitiveAssetStore == null) return null;
        return primitiveAssetStore.GetByKey(primitiveKey);
    }

    /// <summary>Resolve a key to prompt text. Tries PromptRegistry (NarrativePromptAsset.GetActivePromptText()) then PrimitiveAssetStore (Prompt type).</summary>
    public string ResolvePrompt(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        var registry = promptRegistry;
        if (registry == null) registry = Resources.Load<PromptRegistry>("PromptRegistry");
        if (registry != null)
        {
            var asset = registry.Resolve(key);
            if (asset != null)
            {
                var text = asset.GetActivePromptText();
                if (!string.IsNullOrWhiteSpace(text)) return text;
            }
        }
        var entry = ResolvePrimitive(key);
        if (entry != null && entry.type == PrimitiveAssetType.Prompt && !string.IsNullOrWhiteSpace(entry.promptText))
            return entry.promptText;
        return null;
    }

    /// <summary>Ensure generated prefab from entry is registered with SceneObjectRegistry when prebakeAndSave is true.</summary>
    public void EnsureRegistered(DynamicGeneratorBase generator, GeneratedResultEntry entry)
    {
        if (generator == null || entry == null || !generator.prebakeAndSave) return;
        var registry = sceneObjectRegistry;
        if (registry == null) registry = FindAnyObjectByType<SceneObjectRegistry>();
        if (registry == null) return;
        var go = entry.generatedAsset as GameObject;
        if (go == null && !string.IsNullOrEmpty(entry.generatedAssetPath))
            go = LoadAssetAtPath<GameObject>(entry.generatedAssetPath);
        if (go != null)
            registry.Register(generator.GetOrmKey(), go, true);
    }

    /// <summary>Register an animation clip key and path (e.g. from generator).</summary>
    public void RegisterAnimationClipKey(string key, string assetPath)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        int i = animationClipKeys.IndexOf(key);
        if (i >= 0)
        {
            if (i < animationClipPaths.Count) animationClipPaths[i] = assetPath ?? "";
        }
        else
        {
            animationClipKeys.Add(key);
            animationClipPaths.Add(assetPath ?? "");
        }
    }

    /// <summary>IAnimationClipResolver: resolve a clip key to an AnimationClip.</summary>
    public AnimationClip ResolveClip(string key)
    {
        return ResolveAnimationClip(key);
    }

    /// <summary>Register an audio clip key and path.</summary>
    public void RegisterAudioClipKey(string key, string assetPath)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        int i = audioClipKeys.IndexOf(key);
        if (i >= 0)
        {
            if (i < audioClipPaths.Count) audioClipPaths[i] = assetPath ?? "";
        }
        else
        {
            audioClipKeys.Add(key);
            audioClipPaths.Add(assetPath ?? "");
        }
    }

    private static string GetPathForKey(List<string> keys, List<string> paths, string key)
    {
        if (keys == null || paths == null) return null;
        for (int i = 0; i < keys.Count && i < paths.Count; i++)
            if (string.Equals(keys[i], key, System.StringComparison.OrdinalIgnoreCase))
                return paths[i];
        return null;
    }

    private static T LoadAssetAtPath<T>(string path) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(path)) return null;
#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<T>(path);
#else
        if (path.Contains("Resources/"))
        {
            var resourcePath = path;
            var idx = resourcePath.IndexOf("Resources/");
            if (idx >= 0)
            {
                resourcePath = resourcePath.Substring(idx + "Resources/".Length);
                var ext = System.IO.Path.GetExtension(resourcePath);
                if (!string.IsNullOrEmpty(ext)) resourcePath = resourcePath.Replace(ext, "");
                return Resources.Load<T>(resourcePath);
            }
        }
        return null;
#endif
    }
}
