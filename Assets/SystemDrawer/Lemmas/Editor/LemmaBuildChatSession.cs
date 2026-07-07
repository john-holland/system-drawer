#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>Multi-turn chat session for lemma build model refinement.</summary>
public sealed class LemmaBuildChatSession
{
    readonly List<LemmaBuildChatMessage> _messages = new List<LemmaBuildChatMessage>();
    string _lemmaSlug = "default";

    public string ModelId { get; set; }
    public IReadOnlyList<LemmaBuildChatMessage> Messages => _messages;

    public void SetLemmaSlug(string lemma)
    {
        var slug = LemmaBuildSessionPaths.Slugify(lemma);
        if (slug == _lemmaSlug)
            return;
        Save();
        _lemmaSlug = slug;
        Load();
    }

    public void Clear()
    {
        _messages.Clear();
        Save();
    }

    public void AppendUser(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        _messages.Add(new LemmaBuildChatMessage
        {
            role = "user",
            content = text.Trim(),
            timestampUtc = DateTime.UtcNow.ToString("o")
        });
        Save();
    }

    public void AppendAssistant(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;
        _messages.Add(new LemmaBuildChatMessage
        {
            role = "assistant",
            content = text.Trim(),
            timestampUtc = DateTime.UtcNow.ToString("o")
        });
        Save();
    }

    public LmStudioChatMessage[] ToApiMessages(string systemPreface, LemmaBuildFormSnapshot snapshot, int maxConcurrentBuilds)
    {
        var list = new List<LmStudioChatMessage>();
        var system = new StringBuilder();
        if (!string.IsNullOrEmpty(systemPreface))
            system.AppendLine(systemPreface.Trim());
        system.AppendLine();
        system.AppendLine("Current build form:");
        system.AppendLine(JsonUtility.ToJson(snapshot ?? new LemmaBuildFormSnapshot(), true));
        system.AppendLine();
        system.AppendLine($"Active settings: model={ModelId ?? ""}, maxConcurrentBuilds={maxConcurrentBuilds}");
        system.AppendLine();
        system.AppendLine("If you output a LemmaMechanismDescriptor, wrap it in a fenced JSON block:");
        system.AppendLine("```json " + LemmaBuildDescriptorParser.FenceTag);
        system.AppendLine("{ ... }");
        system.AppendLine("```");

        list.Add(new LmStudioChatMessage { role = "system", content = system.ToString() });

        foreach (var msg in _messages)
        {
            if (msg == null || string.IsNullOrEmpty(msg.content))
                continue;
            var role = msg.role ?? "user";
            if (role == "system")
                continue;
            list.Add(new LmStudioChatMessage { role = role, content = msg.content });
        }

        return list.ToArray();
    }

    public bool TryParseLastDescriptor(out LemmaMechanismDescriptor descriptor)
    {
        descriptor = null;
        for (int i = _messages.Count - 1; i >= 0; i--)
        {
            var msg = _messages[i];
            if (msg == null || !string.Equals(msg.role, "assistant", StringComparison.OrdinalIgnoreCase))
                continue;
            if (LemmaBuildDescriptorParser.TryParseFromAssistantText(msg.content, out descriptor))
                return LemmaBuildDescriptorParser.HasRequiredFields(descriptor);
        }
        return false;
    }

    public void Load()
    {
        _messages.Clear();
        var path = LemmaBuildSessionPaths.ChatSessionPath(_lemmaSlug);
        if (!File.Exists(path))
            return;
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonUtility.FromJson<LemmaBuildChatSessionData>(json);
            if (data?.messages != null)
                _messages.AddRange(data.messages.Where(m => m != null));
            if (!string.IsNullOrEmpty(data?.modelId))
                ModelId = data.modelId;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LemmaBuildChat] Failed to load session: " + e.Message);
        }
    }

    public void Save()
    {
        try
        {
            var path = LemmaBuildSessionPaths.ChatSessionPath(_lemmaSlug);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? LemmaBuildSessionPaths.LibraryRoot);
            var data = new LemmaBuildChatSessionData
            {
                messages = _messages.ToArray(),
                modelId = ModelId ?? "",
                lemmaSlug = _lemmaSlug
            };
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LemmaBuildChat] Failed to save session: " + e.Message);
        }
    }
}

public static class LemmaBuildSessionPaths
{
    public const string LibraryRoot = "Library/LemmaBuild";

    public static string ChatSessionPath(string lemmaSlug)
    {
        var slug = string.IsNullOrEmpty(lemmaSlug) ? "default" : lemmaSlug;
        return Path.Combine(LibraryRoot, $"chat_{slug}.json");
    }

    public static string Slugify(string lemma)
    {
        if (string.IsNullOrWhiteSpace(lemma))
            return "default";
        var lower = lemma.Trim().ToLowerInvariant();
        var chars = lower.Select(c =>
            char.IsLetterOrDigit(c) ? c : (c == ' ' || c == '-') ? '-' : '\0').Where(c => c != '\0').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--"))
            slug = slug.Replace("--", "-");
        return string.IsNullOrEmpty(slug) ? "default" : slug.Trim('-');
    }
}
#endif
