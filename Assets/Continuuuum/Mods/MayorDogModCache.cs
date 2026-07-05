using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Continuuuum.Mods
{
    [Serializable]
    public class MayorDogModPackageDto
    {
        public string packageId;
        public string modId;
        public string slug;
        public string displayName;
        public string version;
        public int priority;
    }

    [Serializable]
    public class MayorDogModOverrideDto
    {
        public string packageId;
        public string targetId;
        public string slotKey;
        public string targetKind;
        public string entryId;
        public string draftEpisodeId;
        public int charStart;
        public int charEnd;
        public string overrideText;
        public int priority;
    }

    [Serializable]
    public class MayorDogModManifest
    {
        public int schemaVersion;
        public string cachedAt;
        public string episodeId;
        public string userId;
        public MayorDogModPackageDto[] packages;
        public MayorDogModOverrideDto[] lemmaOverrides;
        public MayorDogModOverrideDto[] episodeOverrides;

        public bool HasData =>
            (lemmaOverrides != null && lemmaOverrides.Length > 0) ||
            (episodeOverrides != null && episodeOverrides.Length > 0) ||
            (packages != null && packages.Length > 0);
    }

    [Serializable]
    public class MayorDogModManifestEnvelope
    {
        public int schemaVersion;
        public string cachedAt;
        public string episodeId;
        public string userId;
        public MayorDogModPackageDto[] packages;
        public MayorDogModOverrideDto[] lemmaOverrides;
        public MayorDogModOverrideDto[] episodeOverrides;
    }

    public static class MayorDogModCache
    {
        static MayorDogModManifest _memory;

        public static string CacheDirectory =>
            Path.Combine(Application.persistentDataPath, "mayor-dog-mods");

        public static string CacheFilePath => Path.Combine(CacheDirectory, "manifest.json");

        public static MayorDogModManifest Current => _memory;

        public static void SetCurrent(MayorDogModManifest manifest)
        {
            _memory = manifest;
        }

        public static void Write(MayorDogModManifest manifest)
        {
            if (manifest == null) return;
            Directory.CreateDirectory(CacheDirectory);
            var env = new MayorDogModManifestEnvelope
            {
                schemaVersion = manifest.schemaVersion,
                cachedAt = manifest.cachedAt,
                episodeId = manifest.episodeId,
                userId = manifest.userId,
                packages = manifest.packages,
                lemmaOverrides = manifest.lemmaOverrides,
                episodeOverrides = manifest.episodeOverrides,
            };
            File.WriteAllText(CacheFilePath, JsonUtility.ToJson(env, true));
            _memory = manifest;
        }

        public static MayorDogModManifest Read()
        {
            try
            {
                if (!File.Exists(CacheFilePath))
                    return null;
                var json = File.ReadAllText(CacheFilePath);
                var env = JsonUtility.FromJson<MayorDogModManifestEnvelope>(json);
                if (env == null) return null;
                var manifest = new MayorDogModManifest
                {
                    schemaVersion = env.schemaVersion,
                    cachedAt = env.cachedAt,
                    episodeId = env.episodeId,
                    userId = env.userId,
                    packages = env.packages ?? Array.Empty<MayorDogModPackageDto>(),
                    lemmaOverrides = env.lemmaOverrides ?? Array.Empty<MayorDogModOverrideDto>(),
                    episodeOverrides = env.episodeOverrides ?? Array.Empty<MayorDogModOverrideDto>(),
                };
                _memory = manifest;
                return manifest;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"MayorDogModCache read failed: {ex.Message}");
                return _memory;
            }
        }
    }

    public static class MayorDogModApplicator
    {
        static readonly Dictionary<string, string> _lemmaBySlot = new Dictionary<string, string>();

        public static void LoadFromManifest(MayorDogModManifest manifest)
        {
            _lemmaBySlot.Clear();
            if (manifest?.lemmaOverrides == null) return;
            Array.Sort(manifest.lemmaOverrides, (a, b) => a.priority.CompareTo(b.priority));
            foreach (var item in manifest.lemmaOverrides)
            {
                if (string.IsNullOrEmpty(item.slotKey)) continue;
                _lemmaBySlot[item.slotKey] = item.overrideText ?? "";
            }
        }

        public static string ResolveModPlaceholders(string text)
        {
            if (string.IsNullOrEmpty(text) || _lemmaBySlot.Count == 0)
                return text ?? "";
            return System.Text.RegularExpressions.Regex.Replace(
                text,
                @"\{\{?M:([^}|]+)(?:\|[^}]+)?\}?\}?|\{M:([^}|]+)(?:\|[^}]+)?\}",
                m =>
                {
                    var key = (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).Trim();
                    return _lemmaBySlot.TryGetValue(key, out var val) && !string.IsNullOrEmpty(val) ? val : m.Value;
                },
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        public static string ApplyEpisodeOverrides(string scriptText, string episodeId)
        {
            var manifest = MayorDogModCache.Current;
            if (manifest?.episodeOverrides == null || string.IsNullOrEmpty(scriptText))
                return scriptText ?? "";
            var sorted = (MayorDogModOverrideDto[])manifest.episodeOverrides.Clone();
            System.Array.Sort(sorted, (a, b) => a.priority.CompareTo(b.priority));
            var text = scriptText;
            for (var i = sorted.Length - 1; i >= 0; i--)
            {
                var item = sorted[i];
                if (!string.IsNullOrEmpty(episodeId) && !string.IsNullOrEmpty(item.draftEpisodeId) &&
                    item.draftEpisodeId != episodeId)
                    continue;
                if (item.charEnd <= item.charStart || string.IsNullOrEmpty(item.overrideText))
                    continue;
                if (item.charStart < 0 || item.charEnd > text.Length)
                    continue;
                text = text.Substring(0, item.charStart) + item.overrideText + text.Substring(item.charEnd);
            }
            return text;
        }
    }
}
