using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PoseTrackLiveDto
{
    public string modelSpec;
    public float tMs;
    public PoseBoneSample[] samples;

    public static PoseTrack ToTrack(string json)
    {
        var dto = JsonUtility.FromJson<PoseTrackLiveDto>(json);
        var track = new PoseTrack { modelSpec = dto != null ? dto.modelSpec : "" };
        if (dto?.samples == null)
            return track;
        for (int i = 0; i < dto.samples.Length; i++)
        {
            if (dto.samples[i] != null)
                track.samples.Add(dto.samples[i]);
        }
        return track;
    }
}
