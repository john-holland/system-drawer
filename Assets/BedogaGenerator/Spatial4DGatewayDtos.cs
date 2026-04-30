using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// One gateway terminus: geometry-derived causality leaf id (e.g. S3.O2.1.7) for supplementary
/// Back / Pause / Forward interpretation along a Bounds4 time window. Leaf ids are sampled at
/// volume center and narrative times tMin, centerT, tMax (clamped to generator bounds)—see SpatialGenerator4D.
/// </summary>
[Serializable]
public class Spatial4DTerminus
{
    public string causalityLeafId;
    public string label;
}

/// <summary>
/// Three-leaf spatial gateway for a single 4D volume: temporal logical anchors, not a replacement for oct-tree octants.
/// </summary>
[Serializable]
public class Spatial4DTerminusTriplet
{
    public Spatial4DTerminus back;
    public Spatial4DTerminus pause;
    public Spatial4DTerminus forward;

    public static Spatial4DTerminusTriplet FromLeafIds(string backId, string pauseId, string forwardId)
    {
        return new Spatial4DTerminusTriplet
        {
            back = string.IsNullOrEmpty(backId) ? null : new Spatial4DTerminus { causalityLeafId = backId },
            pause = string.IsNullOrEmpty(pauseId) ? null : new Spatial4DTerminus { causalityLeafId = pauseId },
            forward = string.IsNullOrEmpty(forwardId) ? null : new Spatial4DTerminus { causalityLeafId = forwardId }
        };
    }
}

/// <summary>Optional extensible named flag slot per causality history row (XML-serializable; prefer over Dictionary).</summary>
[Serializable]
public class CausalityNamedFlagEntryDto
{
    public string key;
    public int value;
}

/// <summary>One append-only row: gateway triplet at observation time + parallel flags channel.</summary>
[Serializable]
public class CausalityHistoryRowSnapshotDto
{
    public string leafBack;
    public string leafPause;
    public string leafForward;
    public long flags;
    public List<CausalityNamedFlagEntryDto> namedFlags = new List<CausalityNamedFlagEntryDto>();
    public float narrativeT;
    public float px, py, pz;
    public string eventType;
}

/// <summary>
/// Append-only causality history: rows are observations (e.g. volume enter); do not mutate prior rows when retracing.
/// </summary>
[Serializable]
public class CausalityHistory2D
{
    public List<CausalityHistoryRowSnapshotDto> rows = new List<CausalityHistoryRowSnapshotDto>();

    public void AppendRow(string leafBack, string leafPause, string leafForward, long flags, float narrativeT,
        Vector3 position, string eventType, IList<CausalityNamedFlagEntryDto> namedFlags = null)
    {
        var row = new CausalityHistoryRowSnapshotDto
        {
            leafBack = leafBack,
            leafPause = leafPause,
            leafForward = leafForward,
            flags = flags,
            narrativeT = narrativeT,
            px = position.x,
            py = position.y,
            pz = position.z,
            eventType = eventType ?? ""
        };
        if (namedFlags != null)
        {
            foreach (var n in namedFlags)
            {
                if (n == null) continue;
                row.namedFlags.Add(new CausalityNamedFlagEntryDto { key = n.key, value = n.value });
            }
        }
        rows.Add(row);
    }

    public CausalityHistory2D CloneForExport()
    {
        var c = new CausalityHistory2D();
        if (rows == null) return c;
        foreach (var r in rows)
        {
            if (r == null) continue;
            var copy = new CausalityHistoryRowSnapshotDto
            {
                leafBack = r.leafBack,
                leafPause = r.leafPause,
                leafForward = r.leafForward,
                flags = r.flags,
                narrativeT = r.narrativeT,
                px = r.px,
                py = r.py,
                pz = r.pz,
                eventType = r.eventType
            };
            if (r.namedFlags != null)
                foreach (var n in r.namedFlags)
                    if (n != null)
                        copy.namedFlags.Add(new CausalityNamedFlagEntryDto { key = n.key, value = n.value });
            c.rows.Add(copy);
        }
        return c;
    }

    public static void MergeAppend(CausalityHistory2D into, CausalityHistory2D from)
    {
        if (into == null || from?.rows == null) return;
        if (into.rows == null) into.rows = new List<CausalityHistoryRowSnapshotDto>();
        foreach (var r in from.rows)
        {
            if (r == null) continue;
            var copy = new CausalityHistoryRowSnapshotDto
            {
                leafBack = r.leafBack,
                leafPause = r.leafPause,
                leafForward = r.leafForward,
                flags = r.flags,
                narrativeT = r.narrativeT,
                px = r.px,
                py = r.py,
                pz = r.pz,
                eventType = r.eventType
            };
            if (r.namedFlags != null)
                foreach (var n in r.namedFlags)
                    if (n != null)
                        copy.namedFlags.Add(new CausalityNamedFlagEntryDto { key = n.key, value = n.value });
            into.rows.Add(copy);
        }
    }
}
