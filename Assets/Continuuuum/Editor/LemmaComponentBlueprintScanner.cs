#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>Editor prefab hierarchy scanner — posts component blueprints to Continuuuum API.</summary>
public static class LemmaComponentBlueprintScanner
{
    public static ComponentMetadataPayloadDto ScanPrefab(GameObject prefab, string entryId, string prefabRef = null)
    {
        if (prefab == null)
            throw new ArgumentNullException(nameof(prefab));
        prefabRef = prefabRef ?? AssetDatabase.GetAssetPath(prefab);
        var nodes = new List<ComponentMetadataNodeDto>();
        WalkHierarchy(prefab.transform, prefab.name, nodes);
        return new ComponentMetadataPayloadDto
        {
            schemaVersion = 1,
            entryId = entryId ?? "",
            prefabRef = prefabRef ?? "",
            source = "blueprint",
            capturedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            contentHash = ComputeContentHash(prefabRef),
            nodes = nodes.ToArray(),
        };
    }

    public static async Task<bool> PostBlueprintAsync(string entryId, ComponentMetadataPayloadDto payload, CancellationToken ct = default)
    {
        if (payload == null || string.IsNullOrEmpty(entryId))
            return false;
        payload.entryId = entryId;
        payload.source = "blueprint";
        var body = JsonUtility.ToJson(payload);
        var path = $"/api/thesaurus/entries/{Uri.EscapeDataString(entryId)}/component-blueprint";
        var r = await ContinuuuumEditorApiClient.RequestAsync("POST", path, body, ct);
        if (!r.success)
            Debug.LogWarning($"[LemmaComponentBlueprintScanner] POST failed: {r.error}");
        return r.success;
    }

    public static async Task<bool> ScanAndPostEntryAsync(string entryId, CancellationToken ct = default)
    {
        var prefabPath = await ResolvePrefabPathForEntryAsync(entryId, ct);
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogWarning($"[LemmaComponentBlueprintScanner] No prefab-id for entry {entryId}");
            return false;
        }
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[LemmaComponentBlueprintScanner] Prefab not found at {prefabPath}");
            return false;
        }
        var payload = ScanPrefab(prefab, entryId, prefabPath);
        return await PostBlueprintAsync(entryId, payload, ct);
    }

    public static async Task<string> ResolvePrefabPathForEntryAsync(string entryId, CancellationToken ct = default)
    {
        var r = await ContinuuuumEditorApiClient.RequestAsync(
            "GET",
            $"/api/thesaurus/entries?entryId={Uri.EscapeDataString(entryId ?? "")}",
            null,
            ct);
        if (!r.success || string.IsNullOrEmpty(r.json))
            return null;
        var idx = r.json.IndexOf("\"prefab-id\"", StringComparison.Ordinal);
        if (idx < 0)
            idx = r.json.IndexOf("prefab-id", StringComparison.Ordinal);
        if (idx < 0)
            return null;
        var colon = r.json.IndexOf(':', idx);
        if (colon < 0)
            return null;
        var start = r.json.IndexOf('"', colon + 1);
        if (start < 0)
            return null;
        var end = r.json.IndexOf('"', start + 1);
        if (end <= start)
            return null;
        return r.json.Substring(start + 1, end - start - 1);
    }

    static void WalkHierarchy(Transform t, string path, List<ComponentMetadataNodeDto> nodes)
    {
        var comps = t.GetComponents<Component>()
            .Where(c => c != null && !(c is Transform))
            .Select(c => new ComponentMetadataComponentDto
            {
                typeName = c.GetType().Name,
                assembly = c.GetType().Assembly.GetName().Name,
            })
            .ToArray();
        nodes.Add(new ComponentMetadataNodeDto
        {
            path = path,
            gameObjectName = t.name,
            components = comps,
        });
        for (var i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            WalkHierarchy(child, path + "/" + child.name, nodes);
        }
    }

    static string ComputeContentHash(string prefabPath)
    {
        try
        {
            var guid = AssetDatabase.AssetPathToGUID(prefabPath ?? "");
            if (File.Exists(prefabPath))
            {
                using var sha = SHA256.Create();
                var bytes = sha.ComputeHash(File.ReadAllBytes(prefabPath));
                var hex = BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
                return guid + ":" + hex.Substring(0, 16);
            }
            return guid;
        }
        catch
        {
            return prefabPath ?? "";
        }
    }

    [MenuItem("Continuuuum/Scan lemma prefab blueprints…")]
    public static void MenuScanPrompt()
    {
        ScanBlueprintEntryWindow.ShowWindow();
    }
}

public class ScanBlueprintEntryWindow : EditorWindow
{
    string _entryId = "";

    public static void ShowWindow()
    {
        var w = GetWindow<ScanBlueprintEntryWindow>(true, "Scan lemma prefab", true);
        w.minSize = new Vector2(360, 80);
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Scan prefab hierarchy and POST component blueprint");
        _entryId = EditorGUILayout.TextField("Entry ID", _entryId);
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = !string.IsNullOrWhiteSpace(_entryId);
        if (GUILayout.Button("Scan and upload"))
        {
            var id = _entryId.Trim();
            _ = LemmaComponentBlueprintScanner.ScanAndPostEntryAsync(id);
            Close();
        }
        GUI.enabled = true;
        if (GUILayout.Button("Cancel"))
            Close();
        EditorGUILayout.EndHorizontal();
    }
}
#endif
