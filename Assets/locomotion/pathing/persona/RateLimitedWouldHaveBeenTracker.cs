using System;
using UnityEngine;

/// <summary>
/// Silly always-on counters: what full-rate civil sim would have done when rate-limited.
/// </summary>
[Serializable]
public sealed class RateLimitedWouldHaveBeenTracker
{
    public long wouldHaveWakes;
    public long wouldHaveBtTicks;
    public long wouldHaveReplans;
    public long wouldHaveBioTicks;
    public long actualWakes;
    public long actualBtTicks;
    public long actualReplans;
    public long actualBioTicks;

    public void NoteWake(bool actuallyRan)
    {
        wouldHaveWakes++;
        if (actuallyRan) actualWakes++;
    }

    public void NoteBtTick(bool actuallyRan)
    {
        wouldHaveBtTicks++;
        if (actuallyRan) actualBtTicks++;
    }

    public void NoteReplan(bool actuallyRan)
    {
        wouldHaveReplans++;
        if (actuallyRan) actualReplans++;
    }

    public void NoteBioTick(bool actuallyRan)
    {
        wouldHaveBioTicks++;
        if (actuallyRan) actualBioTicks++;
    }

    public void Reset()
    {
        wouldHaveWakes = wouldHaveBtTicks = wouldHaveReplans = wouldHaveBioTicks = 0;
        actualWakes = actualBtTicks = actualReplans = actualBioTicks = 0;
    }

    public string DebugSummary() =>
        $"wakes {actualWakes}/{wouldHaveWakes}  bt {actualBtTicks}/{wouldHaveBtTicks}  " +
        $"replan {actualReplans}/{wouldHaveReplans}  bio {actualBioTicks}/{wouldHaveBioTicks}";
}
