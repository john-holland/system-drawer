using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum UscBuildMode
{
    Packed,
    Unpacked,
    PackedPublish
}

[Serializable]
public class UscLanguageVersion
{
    public string major = "1";
    public string minor = "0";
    public string revision = "0";

    public override string ToString()
    {
        return $"{major}.{minor}.{revision}";
    }

    public static UscLanguageVersion Parse(string text)
    {
        var output = new UscLanguageVersion();
        if (string.IsNullOrWhiteSpace(text))
            return output;

        var parts = text.Trim().Split('.');
        if (parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0])) output.major = parts[0];
        if (parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1])) output.minor = parts[1];
        if (parts.Length > 2 && !string.IsNullOrWhiteSpace(parts[2])) output.revision = parts[2];
        return output;
    }
}

[Serializable]
public class UscBuildManifestEntry
{
    public string assetId;
    public string assetLabel;
    public string languageTag;
    public string languageVersion;
    public string localPrefabPath;
    public string localReconstitutedPath;
    public bool includeInPackedPublish = true;
    public bool generatedAsset;
    public string[] sourceScenes = Array.Empty<string>();
}

[Serializable]
public class UscBuildManifest
{
    public string manifestVersion = "1.0";
    public string generatedAtUtc;
    public string tenantId = "default";
    public string languageVersion = "1.0.0";
    public string sourceDbPath = "";
    public UscBuildMode mode = UscBuildMode.PackedPublish;
    public UscBuildManifestEntry[] assets = Array.Empty<UscBuildManifestEntry>();
    public string[] promptLanguageAssets = Array.Empty<string>();
    public string[] scenePaths = Array.Empty<string>();
    public string notes = "";

    public static UscBuildManifest CreateDefault()
    {
        return new UscBuildManifest
        {
            generatedAtUtc = DateTime.UtcNow.ToString("o")
        };
    }

    public static string ToJson(UscBuildManifest manifest, bool pretty = true)
    {
        return JsonUtility.ToJson(manifest, pretty);
    }

    public static UscBuildManifest FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return CreateDefault();

        var parsed = JsonUtility.FromJson<UscBuildManifest>(json);
        return parsed ?? CreateDefault();
    }

    public List<UscBuildManifestEntry> AssetsAsList()
    {
        return assets == null ? new List<UscBuildManifestEntry>() : new List<UscBuildManifestEntry>(assets);
    }
}
