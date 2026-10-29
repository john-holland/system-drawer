using System.Collections.Generic;
using Locomotion.Rig;
using UnityEngine;

/// <summary>Applies a PoseTrack to a BoneMap at a playhead.</summary>
public static class PoseTrackPlayer
{
    public static int Apply(PoseTrack track, BoneMap map, float timeMs)
    {
        if (track == null || map == null)
            return 0;
        var ids = new List<string>();
        track.CollectTraitIds(ids);
        var seen = new HashSet<string>();
        int applied = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            if (!seen.Add(ids[i])) continue;
            if (!track.TrySample(ids[i], timeMs, out var pos, out var rot))
                continue;
            if (!map.TryGet(ids[i], out var t) || t == null)
                continue;
            t.localPosition = pos;
            t.localRotation = rot;
            applied++;
        }
        return applied;
    }
}
