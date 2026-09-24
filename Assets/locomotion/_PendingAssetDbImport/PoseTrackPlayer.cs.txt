using Locomotion.Rig;
using UnityEngine;

/// <summary>Applies a PoseTrack to a BoneMap at a playhead (editor preview or bake).</summary>
public static class PoseTrackPlayer
{
    public static int Apply(PoseTrack track, BoneMap map, float timeMs)
    {
        if (track == null || map == null)
            return 0;
        var ids = new System.Collections.Generic.List<string>();
        track.CollectTraitIds(ids);
        int applied = 0;
        for (int i = 0; i < ids.Count; i++)
        {
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
