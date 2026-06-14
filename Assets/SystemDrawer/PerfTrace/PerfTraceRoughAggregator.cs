using System;
using System.Collections.Generic;

/// <summary>Fixed-size hash aggregates for production-safe rough metrics.</summary>
public sealed class PerfTraceRoughAggregator
{
    readonly Dictionary<int, Entry> _entries = new Dictionary<int, Entry>();
    readonly int _maxNotes;

    struct Entry
    {
        public string Note;
        public int Count;
        public long TotalTicks;
        public long MaxTicks;
        public DateTime LastUtc;
    }

    public PerfTraceRoughAggregator(int maxNotes)
    {
        _maxNotes = Math.Max(8, maxNotes);
    }

    public void Record(string note, long ticks)
    {
        if (string.IsNullOrEmpty(note) || ticks < 0)
            return;

        int key = note.GetHashCode();
        if (!_entries.TryGetValue(key, out var entry) || entry.Note != note)
        {
            if (_entries.Count >= _maxNotes && !_entries.ContainsKey(key))
                EvictOldest();
            entry = new Entry { Note = note };
        }

        entry.Count++;
        entry.TotalTicks += ticks;
        if (ticks > entry.MaxTicks)
            entry.MaxTicks = ticks;
        entry.LastUtc = DateTime.UtcNow;
        _entries[key] = entry;
    }

    void EvictOldest()
    {
        int oldestKey = -1;
        DateTime oldest = DateTime.MaxValue;
        foreach (var pair in _entries)
        {
            if (pair.Value.LastUtc < oldest)
            {
                oldest = pair.Value.LastUtc;
                oldestKey = pair.Key;
            }
        }
        if (oldestKey >= 0)
            _entries.Remove(oldestKey);
    }

    public void CopyToNodes(List<PerfTraceNode> output)
    {
        output.Clear();
        foreach (var pair in _entries)
        {
            var e = pair.Value;
            var node = PerfTraceNode.Create(e.Note, e.Note, PerfTraceGrade.Rough);
            node.TotalTicks = e.TotalTicks;
            node.SelfTicks = e.TotalTicks;
            node.CallCount = e.Count;
            node.Label = $"{e.Note} (max {PerfTraceFormat.Ms(e.MaxTicks)})";
            output.Add(node);
        }
        output.Sort((a, b) => b.TotalTicks.CompareTo(a.TotalTicks));
    }

    public int Count => _entries.Count;
}
