#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>LM Studio multi-turn client for lemma build chat.</summary>
public static class LemmaBuildLmStudioClient
{
    const string PrefaceAssetPath = "Assets/SystemDrawer/Lemmas/_Preface/LemmaBuildSystemPreface.md";

    public static string LoadSystemPreface()
    {
        var fullPath = Path.Combine(Application.dataPath, "SystemDrawer/Lemmas/_Preface/LemmaBuildSystemPreface.md");
        if (File.Exists(fullPath))
            return File.ReadAllText(fullPath);
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(PrefaceAssetPath);
        return asset != null ? asset.text : "";
    }

    public static bool SendChat(
        LemmaBuildChatSession session,
        LemmaBuildFormSnapshot snapshot,
        LemmaBuildSettings settings,
        out string assistantResponse,
        out string error)
    {
        assistantResponse = null;
        error = null;
        if (session == null)
        {
            error = "Chat session is null.";
            return false;
        }

        settings ??= LemmaBuildSettings.LoadOrCreate();
        LmStudioModelService.BaseUrl = settings.lmStudioBaseUrl ?? LmStudioModelService.DefaultLmStudioBaseUrl;
        var modelId = string.IsNullOrEmpty(session.ModelId) ? settings.defaultModelId : session.ModelId;
        if (string.IsNullOrEmpty(modelId))
        {
            error = "No LM Studio model id configured.";
            return false;
        }

        var preface = LoadSystemPreface();
        var messages = session.ToApiMessages(preface, snapshot, settings.maxConcurrentBuilds);
        if (!LmStudioModelService.RequestChatCompletionMulti(modelId, messages, out assistantResponse))
        {
            error = "LM Studio request failed — is the server running at " + LmStudioModelService.BaseUrl + "?";
            return false;
        }

        if (string.IsNullOrWhiteSpace(assistantResponse))
        {
            error = "LM Studio returned an empty response.";
            return false;
        }

        return true;
    }

    public static int EstimateTokenCount(LemmaBuildChatSession session, LemmaBuildFormSnapshot snapshot)
    {
        if (session == null)
            return 0;
        int chars = LoadSystemPreface().Length + JsonUtility.ToJson(snapshot ?? new LemmaBuildFormSnapshot()).Length;
        foreach (var msg in session.Messages)
            chars += msg?.content?.Length ?? 0;
        return Mathf.Max(1, chars / 4);
    }
}
#endif
