#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

/// <summary>Editor settings for lemma build queue and LM Studio.</summary>
[CreateAssetMenu(fileName = "LemmaBuildSettings", menuName = "System Drawer/Lemma Build Settings")]
public sealed class LemmaBuildSettings : ScriptableObject
{
    public const string DefaultModelId = "mistralai/codestral-22b-v0.1";
    public const int DefaultMaxConcurrentBuilds = 3;

    [Range(0, 16)] public int maxConcurrentBuilds = DefaultMaxConcurrentBuilds;
    public string lmStudioBaseUrl = LmStudioModelService.DefaultLmStudioBaseUrl;
    public string defaultModelId = DefaultModelId;
    public string batchOutputRoot = "Assets/SystemDrawer/Lemmas";
    public string sqlMigrationRoot = "Scripts/continuuuum_migrations/lemma_seed";

    static LemmaBuildSettings _cached;

    public static LemmaBuildSettings LoadOrCreate()
    {
        if (_cached != null)
            return _cached;

        const string assetPath = "Assets/SystemDrawer/Lemmas/LemmaBuildSettings.asset";
        _cached = AssetDatabase.LoadAssetAtPath<LemmaBuildSettings>(assetPath);
        if (_cached == null)
        {
            _cached = CreateInstance<LemmaBuildSettings>();
            var dir = System.IO.Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder("Assets/SystemDrawer/Lemmas"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/SystemDrawer"))
                    AssetDatabase.CreateFolder("Assets", "SystemDrawer");
                if (!AssetDatabase.IsValidFolder("Assets/SystemDrawer/Lemmas"))
                    AssetDatabase.CreateFolder("Assets/SystemDrawer", "Lemmas");
            }
            AssetDatabase.CreateAsset(_cached, assetPath);
            AssetDatabase.SaveAssets();
        }

        _cached.maxConcurrentBuilds = EditorPrefs.GetInt("continuuuum.lemmaBuild.maxConcurrentBuilds", _cached.maxConcurrentBuilds);
        _cached.lmStudioBaseUrl = EditorPrefs.GetString("continuuuum.lemmaBuild.lmStudioBaseUrl", _cached.lmStudioBaseUrl);
        _cached.defaultModelId = EditorPrefs.GetString("continuuuum.lemmaBuild.defaultModelId", _cached.defaultModelId);
        return _cached;
    }

    public void SaveOverrides()
    {
        EditorPrefs.SetInt("continuuuum.lemmaBuild.maxConcurrentBuilds", maxConcurrentBuilds);
        EditorPrefs.SetString("continuuuum.lemmaBuild.lmStudioBaseUrl", lmStudioBaseUrl ?? "");
        EditorPrefs.SetString("continuuuum.lemmaBuild.defaultModelId", defaultModelId ?? "");
        LmStudioModelService.BaseUrl = lmStudioBaseUrl ?? LmStudioModelService.DefaultLmStudioBaseUrl;
    }
}
#endif
