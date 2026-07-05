using System;
using UnityEngine;

/// <summary>
/// USC build/runtime service registration for SystemDrawerService.
/// Supports packed/unpacked resolution behavior using a generated manifest.
/// </summary>
public class UscBuildServiceWizard : MonoBehaviour
{
    public const string ServiceKey = "USCBuildService";

    [Header("Build mode")]
    public UscBuildMode buildMode = UscBuildMode.Packed;

    [Header("Continuuuum / USC settings")]
    public string continuuuumBaseUrl = "http://localhost:5050";
    public string tenantId = "default";
    public string sourceDbPath = "";
    public string languageVersion = "1.0.0";

    [Header("Manifest (for unpacked + packed publish flows)")]
    public TextAsset manifestJson;

    [Header("Packed mode runtime fallback")]
    [Tooltip("When true, packed mode may request unresolved assets from USC runtime service.")]
    public bool allowRuntimeUscRequests = true;

    [Serializable]
    public class ResolutionResult
    {
        public string assetId;
        public string resolvedPath;
        public string source;
        public bool success;
    }

    private UscBuildManifest _manifestCache;

    public bool TryCompleteFromService()
    {
        var service = SystemDrawerService.Instance;
        if (service == null) return false;
        var existing = service.Get<UscBuildServiceWizard>(ServiceKey);
        if (existing == null || existing == this) return false;

        continuuuumBaseUrl = existing.continuuuumBaseUrl;
        tenantId = existing.tenantId;
        sourceDbPath = existing.sourceDbPath;
        languageVersion = existing.languageVersion;
        buildMode = existing.buildMode;
        manifestJson = existing.manifestJson;
        allowRuntimeUscRequests = existing.allowRuntimeUscRequests;
        return true;
    }

    public UscBuildManifest GetManifest()
    {
        if (_manifestCache != null)
            return _manifestCache;

        if (manifestJson == null || string.IsNullOrWhiteSpace(manifestJson.text))
            _manifestCache = UscBuildManifest.CreateDefault();
        else
            _manifestCache = UscBuildManifest.FromJson(manifestJson.text);

        return _manifestCache;
    }

    public void ClearManifestCache()
    {
        _manifestCache = null;
    }

    public ResolutionResult ResolveAsset(string assetId)
    {
        var result = new ResolutionResult
        {
            assetId = assetId,
            success = false,
            source = "none",
            resolvedPath = ""
        };

        var manifest = GetManifest();
        if (manifest.assets != null)
        {
            foreach (var entry in manifest.assets)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.assetId)) continue;
                if (!string.Equals(entry.assetId, assetId, StringComparison.OrdinalIgnoreCase)) continue;

                // Unpacked mode must resolve from local manifest only.
                if (buildMode == UscBuildMode.Unpacked)
                {
                    result.success = !string.IsNullOrWhiteSpace(entry.localPrefabPath);
                    result.source = "manifest_local";
                    result.resolvedPath = entry.localPrefabPath ?? "";
                    return result;
                }

                // Packed publish is an export mode; local reference is preferred.
                if (buildMode == UscBuildMode.PackedPublish)
                {
                    result.success = !string.IsNullOrWhiteSpace(entry.localReconstitutedPath) || !string.IsNullOrWhiteSpace(entry.localPrefabPath);
                    result.source = "packed_publish_manifest";
                    result.resolvedPath = string.IsNullOrWhiteSpace(entry.localReconstitutedPath) ? (entry.localPrefabPath ?? "") : entry.localReconstitutedPath;
                    return result;
                }

                // Packed mode can use local first.
                if (!string.IsNullOrWhiteSpace(entry.localPrefabPath))
                {
                    result.success = true;
                    result.source = "local_prefab";
                    result.resolvedPath = entry.localPrefabPath;
                    return result;
                }
            }
        }

        if (buildMode == UscBuildMode.Packed && allowRuntimeUscRequests)
        {
            result.success = true;
            result.source = "usc_runtime_service";
            result.resolvedPath = $"{continuuuumBaseUrl.TrimEnd('/')}/api/media/reconstitute";
            return result;
        }

        return result;
    }

    public string BuildPackedPublishStubCommand()
    {
        return $"python -m unified_semantic_archiver packed-publish --db \"{sourceDbPath}\" --tenant \"{tenantId}\" --language-version \"{languageVersion}\"";
    }

    private void OnEnable()
    {
        if (SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Register(ServiceKey, this);
    }

    private void OnDisable()
    {
        if (SystemDrawerService.Instance != null)
            SystemDrawerService.Instance.Unregister(ServiceKey);
    }
}
