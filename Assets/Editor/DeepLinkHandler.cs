#if UNITY_EDITOR

using System;

using System.IO;

using System.Reflection;

using UnityEditor;



/// <summary>

/// Polls for deeplink file written by continuuuum API (POST /api/deeplink) or system-drawer:// URL stub.

/// Parses window and episodeId; opens Continuuuum Explorer or Episodes window.

/// File path: ~/.continuuuum-deeplink.json or CONTINUUUUM_DEEPLINK_PATH.

/// </summary>

[InitializeOnLoad]

public static class DeepLinkHandler

{

    private const string DefaultPath = ".continuuuum-deeplink.json";

    private static double _lastPollTime;



    static DeepLinkHandler()

    {

        EditorApplication.update += OnEditorUpdate;

    }



    private static string GetDeeplinkPath()

    {

        var env = Environment.GetEnvironmentVariable("CONTINUUUUM_DEEPLINK_PATH");

        if (!string.IsNullOrEmpty(env))

            return env;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return Path.Combine(home, DefaultPath);

    }



    private static void OnEditorUpdate()

    {

        if (EditorApplication.isPlaying)

            return;

        var now = EditorApplication.timeSinceStartup;

        if (now - _lastPollTime < 0.5)

            return;

        _lastPollTime = now;

        var path = GetDeeplinkPath();

        if (!File.Exists(path))

            return;

        try

        {

            var json = File.ReadAllText(path);

            File.Delete(path);

            HandleDeeplink(json);

        }

        catch (Exception ex)

        {

            UnityEngine.Debug.LogWarning($"[DeepLinkHandler] Failed to read/delete {path}: {ex.Message}");

        }

    }



    private static void HandleDeeplink(string json)

    {

        try

        {

            var window = ParseJsonString(json, "window");

            var episodeId = ParseJsonString(json, "episodeId");

            if (string.IsNullOrEmpty(window) && string.IsNullOrEmpty(episodeId))

                return;

            if (window.IndexOf("Explorer", StringComparison.OrdinalIgnoreCase) >= 0 || !string.IsNullOrEmpty(episodeId))

            {

                var dbPath = ContinuuuumSettings.GetDbPath();

                var py = ContinuuuumSettings.GetPythonPath();

                var tenant = ContinuuuumSettings.GetTenant();

                var sql = string.IsNullOrEmpty(episodeId)

                    ? "SELECT * FROM episodes LIMIT 100"

                    : $"SELECT * FROM work_orders WHERE episode_id = '{episodeId.Replace("'", "''")}' LIMIT 50";

                ContinuuuumExplorerWindow.ShowAndRunQuery(dbPath, py, tenant, sql);

            }

            else if (window.IndexOf("Episodes", StringComparison.OrdinalIgnoreCase) >= 0)

            {

                ContinuuuumEpisodesWindow.ShowWindow();

            }

            else if (window.IndexOf("Lemma Build", StringComparison.OrdinalIgnoreCase) >= 0)

            {

                var formJson = ExtractJsonObject(json, "form");

                OpenLemmaBuildWindow(formJson);

            }

            else if (window.IndexOf("Lemma", StringComparison.OrdinalIgnoreCase) >= 0)

            {

                var entryId = ParseJsonString(json, "entryId");

                OpenLemmaPropertiesWindow(entryId);

            }

        }

        catch (Exception ex)

        {

            UnityEngine.Debug.LogWarning($"[DeepLinkHandler] Parse error: {ex.Message}");

        }

    }



    static void OpenLemmaPropertiesWindow(string entryId)

    {

        var windowType = Type.GetType("VocabularyLemmaPropertyEditorWindow, Continuuuum.Editor");

        if (windowType == null)

        {

            UnityEngine.Debug.LogWarning("[DeepLinkHandler] Continuuuum.Editor assembly not found; cannot open Lemma Properties.");

            return;

        }



        var open = windowType.GetMethod("OpenWithEntryId", BindingFlags.Public | BindingFlags.Static);

        if (open == null)

        {

            UnityEngine.Debug.LogWarning("[DeepLinkHandler] OpenWithEntryId not found on VocabularyLemmaPropertyEditorWindow.");

            return;

        }



        open.Invoke(null, new object[] { entryId ?? "" });

    }



    static void OpenLemmaBuildWindow(string formJson)

    {

        var windowType = Type.GetType("VocabularyLemmaPropertyEditorWindow, Continuuuum.Editor");

        if (windowType == null)

        {

            UnityEngine.Debug.LogWarning("[DeepLinkHandler] Continuuuum.Editor assembly not found; cannot open Lemma Build.");

            return;

        }



        var open = windowType.GetMethod("OpenOnLemmaBuildTabWithForm", BindingFlags.Public | BindingFlags.Static);

        if (open == null)

        {

            var fallback = windowType.GetMethod("OpenOnLemmaBuildTab", BindingFlags.Public | BindingFlags.Static);

            fallback?.Invoke(null, null);

            return;

        }



        open.Invoke(null, new object[] { formJson ?? "" });

    }



    static string ExtractJsonObject(string json, string key)

    {

        var token = "\"" + key + "\"";

        var start = json.IndexOf(token, StringComparison.Ordinal);

        if (start < 0)

            return "";

        var brace = json.IndexOf('{', start + token.Length);

        if (brace < 0)

            return "";

        var depth = 0;

        for (var i = brace; i < json.Length; i++)

        {

            var c = json[i];

            if (c == '{')

                depth++;

            else if (c == '}')

            {

                depth--;

                if (depth == 0)

                    return json.Substring(brace, i - brace + 1);

            }

        }



        return "";

    }



    static string ParseJsonString(string json, string key)

    {

        var token = "\"" + key + "\"";

        if (!json.Contains(token))

            return "";

        var start = json.IndexOf(token, StringComparison.Ordinal);

        var valStart = json.IndexOf(':', start) + 1;

        while (valStart < json.Length && (json[valStart] == ' ' || json[valStart] == '"'))

            valStart++;

        if (valStart > 0 && valStart < json.Length && json[valStart - 1] == '"')

            valStart--;

        var valEnd = json.IndexOf('"', valStart + 1);

        if (valEnd > valStart)

            return json.Substring(valStart + 1, valEnd - valStart - 1).Trim();

        return "";

    }

}

#endif

