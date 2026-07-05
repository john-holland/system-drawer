#if UNITY_EDITOR
using System;
using System.Collections.Generic;

/// <summary>Append-only Continuuuum editor console feed (notifications + local events).</summary>
public static class ContinuuuumNotificationConsoleSink
{
    public struct ConsoleEntry
    {
        public DateTime at;
        public string type;
        public string message;
        public string draftId;
        public string reviewId;
    }

    static readonly List<ConsoleEntry> _entries = new List<ConsoleEntry>();
    const int MaxEntries = 500;

    public static IReadOnlyList<ConsoleEntry> Entries => _entries;

    public static event Action Changed;

    public static void Log(string type, string message, string draftId = null, string reviewId = null)
    {
        _entries.Add(new ConsoleEntry
        {
            at = DateTime.Now,
            type = type ?? "info",
            message = message ?? "",
            draftId = draftId,
            reviewId = reviewId
        });
        if (_entries.Count > MaxEntries)
            _entries.RemoveAt(0);
        UnityEngine.Debug.Log($"[Continuuuum] {type}: {message}");
        Changed?.Invoke();
    }

    public static void AddNotification(NotificationItem item)
    {
        if (item == null) return;
        Log(item.type ?? "notification", item.message, item.draftId, item.reviewId);
    }

    public static void Clear() => _entries.Clear();
}

#endif
