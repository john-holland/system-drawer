using System;
using System.Collections.Generic;

/// <summary>Fixed-size hash aggregates for production-safe rough metrics.</summary>
public sealed class PerfTraceRoughAggregator
{
    readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
    readonly int _maxNotes;

    struct Entry
    {
        public int Count;
        public long TotalTicks;
        public long MaxTicks;
        public DateTime LastUtc;
        public int Sequence;
    }

    int _nextSequence;

    public PerfTraceRoughAggregator(int maxNotes)
    {
        _maxNotes = Math.Max(1, maxNotes);
    }

    public void Record(string note, long ticks)
    {
        if (string.IsNullOrEmpty(note) || ticks < 0)
            return;

        if (!_entries.TryGetValue(note, out var entry))
        {
            if (_entries.Count >= _maxNotes)
                EvictOldest();
            entry = new Entry { Sequence = ++_nextSequence };
        }
        else if (entry.Sequence == 0)
        {
            entry.Sequence = ++_nextSequence;
        }

        entry.Count++;
        entry.TotalTicks += ticks;
        if (ticks > entry.MaxTicks)
            entry.MaxTicks = ticks;
        entry.LastUtc = DateTime.UtcNow;
        _entries[note] = entry;
    }

    void EvictOldest()
    {
        string oldestKey = null;
        int oldestSeq = int.MaxValue;
        foreach (var pair in _entries)
        {
            if (pair.Value.Sequence < oldestSeq)
            {
                oldestSeq = pair.Value.Sequence;
                oldestKey = pair.Key;
            }
        }
        if (oldestKey != null)
            _entries.Remove(oldestKey);
    }

    public void CopyToNodes(List<PerfTraceNode> output)
    {
        output.Clear();
        foreach (var pair in _entries)
        {
            var e = pair.Value;
            var node = PerfTraceNode.Create(pair.Key, pair.Key, PerfTraceGrade.Rough);
            node.TotalTicks = e.TotalTicks;
            node.SelfTicks = e.TotalTicks;
            node.CallCount = e.Count;
            node.Label = $"{pair.Key} (max {PerfTraceFormat.Ms(e.MaxTicks)})";
            output.Add(node);
        }
        output.Sort((a, b) => b.TotalTicks.CompareTo(a.TotalTicks));
    }

    public int Count => _entries.Count;
}
