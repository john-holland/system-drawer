#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor orchestration surface for USC packed/unpacked/packed-publish flows.
/// First pass generates a manifest and stub publish commands.
/// </summary>
public class ContinuumBuildManagerWindow : EditorWindow
{
    [SerializeField] private UscBuildMode mode = UscBuildMode.PackedPublish;
    [SerializeField] private string tenantId = "default";
    [SerializeField] private string continuumBaseUrl = "http://localhost:5050";
    [SerializeField] private string sourceDbPath = "";
    [SerializeField] private string languageVersion = "1.0.0";
    [SerializeField] private string promptLanguageAssetsCsv = "";
    [SerializeField] private string manifestOutPath = "Assets/Generated/USC/usc_build_manifest.json";
    [SerializeField] private string lastStatus = "";
    [SerializeField] private Vector2 scroll;

    private List<string> _scenePaths = new List<string>();
    private UscBuildManifest _draftManifest;

    [MenuItem("Window/Continuum/Build Manager")]
    public static void ShowWindow()
    {
        var window = GetWindow<ContinuumBuildManagerWindow>("Continuum Build Manager");
        window.minSize = new Vector2(580f, 480f);
    }

    private void OnEnable()
    {
        RefreshScenes();
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("Continuum Build Manager", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Build modes:\n" +
            "- packed: runtime USC fallback allowed\n" +
            "- unpacked: resolve from manifest-local assets only\n" +
            "- packed publish: generate reduced USC package + manifest (stub orchestration in v1)",
            MessageType.Info);

        EditorGUILayout.Space();
        mode = (UscBuildMode)EditorGUILayout.EnumPopup("Build mode", mode);
        tenantId = EditorGUILayout.TextField("Tenant", tenantId);
        continuumBaseUrl = EditorGUILayout.TextField("Continuum URL", continuumBaseUrl);
        sourceDbPath = EditorGUILayout.TextField("Source DB path", sourceDbPath);
        languageVersion = EditorGUILayout.TextField("Language version", languageVersion);
        promptLanguageAssetsCsv = EditorGUILayout.TextField("Prompt language assets (csv)", promptLanguageAssetsCsv);
        manifestOutPath = EditorGUILayout.TextField("Manifest output path", manifestOutPath);

        EditorGUILayout.Space();
        if (GUILayout.Button("Refresh scene references", GUILayout.Height(24)))
            RefreshScenes();

        DrawSceneList();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Generate draft manifest", GUILayout.Height(24)))
            GenerateDraftManifest();

        if (GUILayout.Button("Save manifest JSON", GUILayout.Height(24)))
            SaveDraftManifest();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preview packed-publish CLI stub", GUILayout.Height(24)))
            PreviewPackedPublishCommand();

        if (GUILayout.Button("Run packed-publish stub", GUILayout.Height(24)))
            RunPackedPublishStub();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        if (!string.IsNullOrWhiteSpace(lastStatus))
            EditorGUILayout.HelpBox(lastStatus, MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    private void DrawSceneList()
    {
        EditorGUILayout.LabelField($"Scenes in build ({_scenePaths.Count})", EditorStyles.boldLabel);
        using (new EditorGUILayout.VerticalScope("box"))
        {
            if (_scenePaths.Count == 0)
            {
                EditorGUILayout.LabelField("No build scenes configured.");
                return;
            }

            foreach (var scenePath in _scenePaths)
                EditorGUILayout.LabelField("- " + scenePath);
        }
    }

    private void RefreshScenes()
    {
        _scenePaths.Clear();
        var scenes = EditorBuildSettings.scenes;
        if (scenes == null) return;

        foreach (var scene in scenes)
        {
            if (scene == null || !scene.enabled || string.IsNullOrWhiteSpace(scene.path))
                continue;
            _scenePaths.Add(scene.path);
        }
    }

    private string[] ParsePromptLanguageAssets()
    {
        if (string.IsNullOrWhiteSpace(promptLanguageAssetsCsv))
            return Array.Empty<string>();

        var bits = promptLanguageAssetsCsv.Split(',');
        var parsed = new List<string>();
        foreach (var raw in bits)
        {
            var trimmed = raw.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
                parsed.Add(trimmed);
        }
        return parsed.ToArray();
    }

    private void GenerateDraftManifest()
    {
        var manifest = UscBuildManifest.CreateDefault();
        manifest.mode = mode;
        manifest.tenantId = string.IsNullOrWhiteSpace(tenantId) ? "default" : tenantId.Trim();
        manifest.languageVersion = string.IsNullOrWhiteSpace(languageVersion) ? "1.0.0" : languageVersion.Trim();
        manifest.sourceDbPath = sourceDbPath ?? "";
        manifest.scenePaths = _scenePaths.ToArray();
        manifest.promptLanguageAssets = ParsePromptLanguageAssets();
        manifest.generatedAtUtc = DateTime.UtcNow.ToString("o");
        manifest.notes = "Generated by ContinuumBuildManagerWindow (v1 manifest+stub flow).";

        var entries = new List<UscBuildManifestEntry>();
        foreach (var promptAsset in manifest.promptLanguageAssets)
        {
            entries.Add(new UscBuildManifestEntry
            {
                assetId = $"prompt::{promptAsset}",
                assetLabel = promptAsset,
                languageTag = "prompt",
                languageVersion = manifest.languageVersion,
                includeInPackedPublish = true,
                generatedAsset = true,
                sourceScenes = manifest.scenePaths
            });
        }
        manifest.assets = entries.ToArray();

        _draftManifest = manifest;
        lastStatus = $"Draft manifest created with {_draftManifest.assets.Length} assets, mode={_draftManifest.mode}.";
    }

    private void SaveDraftManifest()
    {
        if (_draftManifest == null)
            GenerateDraftManifest();
        if (_draftManifest == null)
        {
            lastStatus = "Failed to create draft manifest.";
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", manifestOutPath));
        var json = UscBuildManifest.ToJson(_draftManifest, true);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..")));
        File.WriteAllText(fullPath, json);
        AssetDatabase.Refresh();
        lastStatus = $"Manifest saved: {fullPath}";
    }

    private string BuildPackedPublishCommand()
    {
        var lang = string.IsNullOrWhiteSpace(languageVersion) ? "1.0.0" : languageVersion.Trim();
        return $"python -m unified_semantic_archiver packed-publish --db \"{sourceDbPath}\" --tenant \"{tenantId}\" --language-version \"{lang}\" --manifest-out \"{manifestOutPath}\"";
    }

    private void PreviewPackedPublishCommand()
    {
        EditorUtility.DisplayDialog("Packed publish CLI stub", BuildPackedPublishCommand(), "OK");
    }

    private void RunPackedPublishStub()
    {
        // Stub execution only: keep behavior explicit and infrastructure-safe for now.
        var cmd = BuildPackedPublishCommand();
        Debug.Log("[ContinuumBuildManager] Stub packed-publish command:\n" + cmd);
        lastStatus = "Stub packed-publish command logged to Console.";
    }
}
#endif
