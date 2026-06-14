using System.Collections.Generic;
using Unity.Profiling;

/// <summary>Lazy ProfilerMarker instances keyed by note.</summary>
public static class PerfTraceMarkerCache
{
    static readonly Dictionary<string, ProfilerMarker> Markers = new Dictionary<string, ProfilerMarker>();

    public static ProfilerMarker Get(string note)
    {
        if (string.IsNullOrEmpty(note))
            note = "PerfTrace";
        if (!Markers.TryGetValue(note, out var marker))
        {
            marker = new ProfilerMarker(note);
            Markers[note] = marker;
        }
        return marker;
    }
}
