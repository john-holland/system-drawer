#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;

/// <summary>
/// LM Studio API (and Barracuda scan) for "Search for updates": list models, filter by generator keywords.
/// </summary>
public static class LmStudioModelService
{
    public const string DefaultLmStudioBaseUrl = "http://localhost:1234/v1";

    private static string _baseUrl = DefaultLmStudioBaseUrl;

    public static string BaseUrl { get => _baseUrl; set => _baseUrl = value ?? DefaultLmStudioBaseUrl; }

    /// <summary>List models from LM Studio (GET /v1/models). Returns model ids/names or empty list on error.</summary>
    public static List<string> GetLmStudioModels()
    {
        var list = new List<string>();
        try
        {
            using (var req = UnityWebRequest.Get(_baseUrl.TrimEnd('/') + "/models"))
            {
                req.SendWebRequest();
                while (!req.isDone) { }
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var json = req.downloadHandler?.text;
                    if (!string.IsNullOrEmpty(json))
                        ParseModelsJson(json, list);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LmStudio] Get models failed: " + e.Message);
        }
        return list;
    }

    private static void ParseModelsJson(string json, List<string> outModels)
    {
        try
        {
            var wrap = JsonUtility.FromJson<ModelsResponse>(json);
            if (wrap?.data != null)
            {
                foreach (var m in wrap.data)
                    if (!string.IsNullOrEmpty(m.id)) outModels.Add(m.id);
            }
        }
        catch
        {
            outModels.Clear();
        }
    }

    [System.Serializable]
    private class ModelsResponse
    {
        public ModelEntry[] data;
    }

    [System.Serializable]
    private class ModelEntry
    {
        public string id;
    }

    /// <summary>Filter model list by keywords (any keyword present in model id = match).</summary>
    public static List<string> FilterByKeywords(List<string> modelIds, List<string> keywords)
    {
        if (keywords == null || keywords.Count == 0) return modelIds ?? new List<string>();
        if (modelIds == null) return new List<string>();
        var result = new List<string>();
        foreach (var id in modelIds)
        {
            if (string.IsNullOrEmpty(id)) continue;
            var lower = id.ToLowerInvariant();
            foreach (var kw in keywords)
            {
                if (!string.IsNullOrWhiteSpace(kw) && lower.Contains(kw.Trim().ToLowerInvariant()))
                {
                    result.Add(id);
                    break;
                }
            }
        }
        return result;
    }

    /// <summary>Scan project for ONNX/NNModel (e.g. StreamingAssets/Models). Match by filename to keywords.</summary>
    public static List<string> GetBarracudaModelsInProject(List<string> keywords)
    {
        var list = new List<string>();
        var guids = AssetDatabase.FindAssets("t:NNModel");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (string.IsNullOrEmpty(path)) continue;
            if (keywords != null && keywords.Count > 0)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool match = false;
                foreach (var kw in keywords)
                {
                    if (!string.IsNullOrWhiteSpace(kw) && name.Contains(kw.Trim().ToLowerInvariant())) { match = true; break; }
                }
                if (!match) continue;
            }
            list.Add(path);
        }
        guids = AssetDatabase.FindAssets("onnx");
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".onnx", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (keywords != null && keywords.Count > 0)
            {
                var name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                bool match = false;
                foreach (var kw in keywords)
                {
                    if (!string.IsNullOrWhiteSpace(kw) && name.Contains(kw.Trim().ToLowerInvariant())) { match = true; break; }
                }
                if (!match) continue;
            }
            if (!list.Contains(path)) list.Add(path);
        }
        return list;
    }

    /// <summary>Show "Search for updates" result in window or dialog. Call from EditorWindow.</summary>
    public static void SearchForUpdates(DynamicGeneratorBase generator, EditorWindow window)
    {
        if (generator == null) return;
        var keywords = generator.modelKeywords != null ? generator.modelKeywords : new List<string>();
        var lmModels = GetLmStudioModels();
        var filteredLm = FilterByKeywords(lmModels, keywords);
        var barracuda = GetBarracudaModelsInProject(keywords);
        string msg = $"LM Studio models (filtered): {filteredLm.Count}\n" + (filteredLm.Count > 0 ? string.Join("\n", filteredLm) : "(none or server not running)") +
            "\n\nBarracuda/ONNX in project (filtered): " + barracuda.Count + "\n" + (barracuda.Count > 0 ? string.Join("\n", barracuda) : "(none)");
        EditorUtility.DisplayDialog("Search for updates", msg, "OK");
    }

    /// <summary>Default timeout in seconds for chat completion requests.</summary>
    public const int DefaultCompletionTimeoutSeconds = 120;

    /// <summary>
    /// Call LM Studio's OpenAI-compatible chat completions endpoint.
    /// POST {BaseUrl}/chat/completions with model, system + user messages.
    /// </summary>
    /// <param name="modelId">Model id (e.g. from GetLmStudioModels).</param>
    /// <param name="systemPrompt">System message content (e.g. "You generate Unity ShaderLab shaders. Output only valid shader code.").</param>
    /// <param name="userPrompt">User message content (the shader prompt).</param>
    /// <param name="response">Output: assistant message content, or null on failure.</param>
    /// <param name="maxTokens">Optional max tokens (default 4096).</param>
    /// <param name="temperature">Optional temperature (default 0.2 for code).</param>
    /// <param name="timeoutSeconds">Request timeout in seconds (default DefaultCompletionTimeoutSeconds).</param>
    /// <returns>True if request succeeded and response is non-empty.</returns>
    public static bool RequestChatCompletion(
        string modelId,
        string systemPrompt,
        string userPrompt,
        out string response,
        int maxTokens = 4096,
        float temperature = 0.2f,
        int timeoutSeconds = DefaultCompletionTimeoutSeconds)
    {
        response = null;
        if (string.IsNullOrEmpty(modelId)) { Debug.LogWarning("[LmStudio] RequestChatCompletion: modelId is empty."); return false; }
        var url = _baseUrl.TrimEnd('/') + "/chat/completions";
        var body = new ChatCompletionRequest
        {
            model = modelId,
            messages = new[]
            {
                new ChatMessage { role = "system", content = systemPrompt ?? "" },
                new ChatMessage { role = "user", content = userPrompt ?? "" }
            },
            max_tokens = maxTokens,
            temperature = temperature
        };
        string jsonBody;
        try
        {
            jsonBody = JsonUtility.ToJson(body);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LmStudio] RequestChatCompletion: serialize request failed: " + e.Message);
            return false;
        }
        byte[] raw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(raw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;
            req.SendWebRequest();
            while (!req.isDone) { }
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[LmStudio] RequestChatCompletion failed: " + req.error + " (" + req.responseCode + ")");
                return false;
            }
            var json = req.downloadHandler?.text;
            if (string.IsNullOrEmpty(json)) return false;
            return ParseChatCompletionResponse(json, out response);
        }
    }

    private static bool ParseChatCompletionResponse(string json, out string content)
    {
        content = null;
        try
        {
            var wrap = JsonUtility.FromJson<ChatCompletionResponse>(json);
            if (wrap?.choices != null && wrap.choices.Length > 0)
            {
                var msg = wrap.choices[0].message;
                if (msg != null && msg.content != null)
                {
                    content = msg.content;
                    return true;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LmStudio] ParseChatCompletionResponse failed: " + e.Message);
        }
        return false;
    }

    [System.Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    private class ChatCompletionRequest
    {
        public string model;
        public ChatMessage[] messages;
        public int max_tokens;
        public float temperature;
    }

    [System.Serializable]
    private class ChatCompletionResponse
    {
        public ChatChoice[] choices;
    }

    [System.Serializable]
    private class ChatChoice
    {
        public ChatMessage message;
    }
}
#endif
