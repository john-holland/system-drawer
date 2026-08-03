using System;
using System.Collections.Generic;
using UnityEngine;

public enum MusicAmbianceTag
{
    Neutral = 0,
    Hushed = 1,
    Classical = 2,
    Club = 3,
    Bar = 4,
    Lobby = 5
}

[Serializable]
public sealed class MusicAmbianceSlot
{
    public string slotId;
    public string hoursCron = "* 20-2 * * *";
    public string musicCompositionId;
    public MusicAmbianceTag ambiance = MusicAmbianceTag.Neutral;
    public string celebrityPersonaKey;
    public string appearanceNote;
    [Range(0f, 1f)] public float ambianceScoreBias01 = 0.5f;
}

/// <summary>Cron slots → music composition + ambiance + optional celebrity appearance.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Music Ambiance Schedule")]
public sealed class MusicAmbianceSchedule : MonoBehaviour
{
    public List<MusicAmbianceSlot> slots = new List<MusicAmbianceSlot>();
    public MusicAmbianceSlot Current { get; private set; }
    [Range(0f, 1f)] public float ambianceScore01 = 0.5f;

    public void Tick(DateTime utcNow, float occupancy01 = 0.5f)
    {
        MusicAmbianceSlot best = null;
        for (int i = 0; i < slots.Count; i++)
        {
            var s = slots[i];
            if (s == null) continue;
            if (!CronDue.IsActiveSchedule(s.hoursCron, utcNow)) continue;
            best = s;
            break;
        }
        Current = best;
        float bias = best != null ? best.ambianceScoreBias01 : 0.35f;
        ambianceScore01 = Mathf.Clamp01(bias * 0.65f + occupancy01 * 0.35f);
    }

    public string CelebrityNow => Current?.celebrityPersonaKey;
    public MusicAmbianceTag AmbianceNow => Current?.ambiance ?? MusicAmbianceTag.Neutral;
}
