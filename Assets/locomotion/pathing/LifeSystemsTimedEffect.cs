using System;
using System.Collections.Generic;

public enum LifeSystemsEffectSource
{
    Lemma,
    Dev,
    Supplement,
    Illness
}

[Serializable]
public sealed class LifeSystemsChannelDelta
{
    public string channelId;
    public float delta01;
}

[Serializable]
public sealed class LifeSystemsOrganDelta
{
    public string organId;
    public float rawDelta;
}

[Serializable]
public sealed class LifeSystemsEffectSpec
{
    public string id;
    public LifeSystemsEffectSource source = LifeSystemsEffectSource.Dev;
    public string promptLabel;
    /// <summary>UTC ticks; 0 means apply immediately.</summary>
    public long startUtcTicks;
    /// <summary>0 = until cleared.</summary>
    public float durationSeconds;
    public List<LifeSystemsChannelDelta> channelDeltas = new List<LifeSystemsChannelDelta>();
    public List<LifeSystemsOrganDelta> organDeltas = new List<LifeSystemsOrganDelta>();
    public float lifeForceDelta;
    public float bioRhythmAmplitudeDelta;
    public string targetActorKey;
}

[Serializable]
public sealed class LifeSystemsActiveEffect
{
    public LifeSystemsEffectSpec spec;
    public double appliedUnscaledTime;
    public bool channelApplied;
    public bool organApplied;
}
