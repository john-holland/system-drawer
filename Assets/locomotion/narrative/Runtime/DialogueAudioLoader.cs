using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Locomotion.Narrative
{
    /// <summary>
    /// Loads dialogue audio clips from local paths or Continuuuum USC audioRef URLs with caching.
    /// </summary>
    public static class DialogueAudioLoader
    {
        static readonly Dictionary<string, AudioClip> Cache = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);

        public static bool TryGetCached(string audioRef, out AudioClip clip) =>
            Cache.TryGetValue(audioRef ?? "", out clip);

        public static AudioClip LoadSync(string audioRef, string continuuuumBaseUrl = "http://127.0.0.1:5050")
        {
            if (string.IsNullOrWhiteSpace(audioRef))
                return null;
            if (Cache.TryGetValue(audioRef, out var cached))
                return cached;

#if UNITY_EDITOR
            if (audioRef.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioRef);
                if (clip != null)
                    Cache[audioRef] = clip;
                return clip;
            }
#endif

            string url = ResolveUrl(audioRef, continuuuumBaseUrl);
            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) { }
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogWarning("[DialogueAudioLoader] Failed to load: " + url + " — " + req.error);
                    return null;
                }
                var loaded = DownloadHandlerAudioClip.GetContent(req);
                if (loaded != null)
                    Cache[audioRef] = loaded;
                return loaded;
            }
        }

        public static string ResolveUrl(string audioRef, string continuuuumBaseUrl)
        {
            if (audioRef.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                audioRef.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return audioRef;

            if (audioRef.StartsWith("usc://", StringComparison.OrdinalIgnoreCase))
            {
                string path = audioRef.Substring("usc://".Length).TrimStart('/');
                return continuuuumBaseUrl.TrimEnd('/') + "/api/library/documents/" + path;
            }

            return continuuuumBaseUrl.TrimEnd('/') + "/" + audioRef.TrimStart('/');
        }
    }
}
