using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Local JSONL session history. Kept indefinitely; truncate oldest after 50MB per product.</summary>
public static class StructuredChatSessionHistory
{
    public const long MaxBytes = 50L * 1024 * 1024;

    public static string RootOverride;

    [Serializable]
    public sealed class Entry
    {
        public string sentAt;
        public string userId;
        public string[] tokens;
        public string text;
        public string direction;
    }

    public static string ProductDir(string productId)
    {
        string root = RootOverride;
        if (string.IsNullOrEmpty(root))
            root = Path.Combine(Application.persistentDataPath, "structured-chat");
        return Path.Combine(root, Sanitize(productId ?? "default"));
    }

    public static string SessionPath(string productId, string sessionId)
    {
        return Path.Combine(ProductDir(productId), Sanitize(sessionId ?? "default") + ".jsonl");
    }

    public static void Append(string productId, string sessionId, Entry entry)
    {
        if (entry == null)
            return;
        string dir = ProductDir(productId);
        Directory.CreateDirectory(dir);
        string path = SessionPath(productId, sessionId);
        if (string.IsNullOrEmpty(entry.sentAt))
            entry.sentAt = DateTime.UtcNow.ToString("o");
        File.AppendAllText(path, JsonUtility.ToJson(entry) + "\n", Encoding.UTF8);
        TruncateIfNeeded(productId);
    }

    public static List<Entry> Load(string productId, string sessionId)
    {
        var list = new List<Entry>();
        string path = SessionPath(productId, sessionId);
        if (!File.Exists(path))
            return list;
        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var e = JsonUtility.FromJson<Entry>(line);
            if (e != null)
                list.Add(e);
        }
        return list;
    }

    public static long ProductBytes(string productId)
    {
        string dir = ProductDir(productId);
        if (!Directory.Exists(dir))
            return 0;
        long total = 0;
        foreach (var file in Directory.GetFiles(dir, "*.jsonl"))
            total += new FileInfo(file).Length;
        return total;
    }

    public static void TruncateIfNeeded(string productId)
    {
        TruncateIfNeeded(productId, MaxBytes);
    }

    public static void TruncateIfNeeded(string productId, long maxBytes)
    {
        string dir = ProductDir(productId);
        if (!Directory.Exists(dir))
            return;
        var files = new List<string>(Directory.GetFiles(dir, "*.jsonl"));
        files.Sort(string.CompareOrdinal);
        while (ProductBytes(productId) > maxBytes && files.Count > 0)
        {
            string oldest = files[0];
            var lines = new List<string>(File.Exists(oldest) ? File.ReadAllLines(oldest) : Array.Empty<string>());
            if (lines.Count == 0)
            {
                File.Delete(oldest);
                files.RemoveAt(0);
                continue;
            }
            lines.RemoveAt(0);
            if (lines.Count == 0)
            {
                File.Delete(oldest);
                files.RemoveAt(0);
            }
            else
            {
                File.WriteAllLines(oldest, lines);
            }
        }
    }

    static string Sanitize(string id)
    {
        if (string.IsNullOrEmpty(id))
            return "default";
        var sb = new StringBuilder(id.Length);
        for (int i = 0; i < id.Length; i++)
        {
            char c = id[i];
            sb.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_');
        }
        return sb.ToString();
    }
}
