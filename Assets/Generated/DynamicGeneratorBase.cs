using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base for dynamic generator assets (3D, audio, music, animation, texture/UI, shader).
/// Stores prompt, model keywords, prebakeAndSave, history, and ORM output key.
/// </summary>
public abstract class DynamicGeneratorBase : ScriptableObject
{
    [Header("Prompt and model")]
    [Tooltip("Text prompt for generation.")]
    [TextArea(2, 6)]
    public string prompt = "";

    [Tooltip("Keywords used to filter models in 'Search for updates' (e.g. 3d, mesh, audio, texture).")]
    public List<string> modelKeywords = new List<string> { "general" };

    [Tooltip("When true, on generate we bake/save the result and register it with SceneObjectRegistry.")]
    public bool prebakeAndSave = false;

    [Tooltip("Key used when registering the generated object with the scene object registry (ORM). Leave empty to auto-derive from name + prompt hash.")]
    public string ormOutputKey = "";

    [Header("Primitive keys (optional)")]
    [Tooltip("Key of stored image/drawing primitive to use as input (e.g. for image → 3D character).")]
    public string imageKey = "";
    [Tooltip("Key of stored video primitive to use as input (e.g. for video → animation).")]
    public string videoKey = "";
    [Tooltip("Key of stored sound primitive to use as input (e.g. for sound → ML).")]
    public string soundKey = "";

    [Header("History")]
    [Tooltip("History of generated results. Newest last.")]
    public List<GeneratedResultEntry> history = new List<GeneratedResultEntry>();

    [Tooltip("Index of the 'current' result (used when reverting or when prebakeAndSave uses the latest). -1 = none.")]
    public int currentResultIndex = -1;

    /// <summary>Get the current result entry, or null.</summary>
    public GeneratedResultEntry GetCurrentResult()
    {
        if (history == null || currentResultIndex < 0 || currentResultIndex >= history.Count)
            return null;
        return history[currentResultIndex];
    }

    /// <summary>Append a new history entry. Returns the entry.</summary>
    public GeneratedResultEntry AddHistoryEntry(string prompt, string assetPath, UnityEngine.Object asset, string modelUsed = null)
    {
        if (history == null) history = new List<GeneratedResultEntry>();
        var entry = new GeneratedResultEntry(prompt, assetPath, asset, modelUsed);
        history.Add(entry);
        currentResultIndex = history.Count - 1;
        return entry;
    }

    /// <summary>Remove entry at index; optionally delete asset from project. Returns true if removed.</summary>
    public bool RemoveHistoryEntry(int index, bool deleteAssetFromProject)
    {
        if (history == null || index < 0 || index >= history.Count) return false;
        var entry = history[index];
#if UNITY_EDITOR
        if (deleteAssetFromProject && !string.IsNullOrEmpty(entry.generatedAssetPath))
            UnityEditor.AssetDatabase.DeleteAsset(entry.generatedAssetPath);
#endif
        history.RemoveAt(index);
        if (currentResultIndex >= history.Count) currentResultIndex = history.Count - 1;
        if (currentResultIndex < 0) currentResultIndex = -1;
        return true;
    }

    /// <summary>Remove all history entries; optionally delete their assets.</summary>
    public void ClearHistory(bool deleteAssetsFromProject)
    {
        if (history == null) return;
#if UNITY_EDITOR
        if (deleteAssetsFromProject)
        {
            foreach (var entry in history)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.generatedAssetPath))
                    UnityEditor.AssetDatabase.DeleteAsset(entry.generatedAssetPath);
            }
        }
#endif
        history.Clear();
        currentResultIndex = -1;
    }

    /// <summary>Resolve ORM key: use ormOutputKey if set, else derive from name + prompt hash.</summary>
    public string GetOrmKey()
    {
        if (!string.IsNullOrWhiteSpace(ormOutputKey)) return ormOutputKey.Trim();
        var seed = (name ?? "") + "_" + (prompt ?? "");
        return "gen_" + Mathf.Abs(seed.GetHashCode()).ToString();
    }

    /// <summary>Generator type for UI and export (e.g. 3D, Audio, Texture).</summary>
    public abstract string GeneratorTypeName { get; }
}
