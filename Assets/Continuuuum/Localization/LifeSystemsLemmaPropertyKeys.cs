using System;

/// <summary>Property keys and DTOs for {P:life|...} lemma painting.</summary>
public static class LifeSystemsLemmaPropertyKeys
{
    public const string PlaceholderName = "life";
    // Prompt params use short names (op, channel, ...); catalog keys are life-* for discovery.
    public const string Op = "op";
    public const string Channel = "channel";
    public const string Value = "value";
    public const string Delta = "delta";
    public const string Duration = "duration";
    public const string Query = "q";
    public const string OrganId = "id";
    public const string LifeForce = "lifeForce";
    public const string Label = "label";
    public const string Difficulty = "difficulty";
    public const string Raw = "raw";
    public const string BioRhythm = "bioRhythm";

    public const string SpecOp = "life-op";
    public const string SpecChannel = "life-channel";
    public const string SpecValue = "life-value";
    public const string SpecDelta = "life-delta";
    public const string SpecDuration = "life-duration";
    public const string SpecQuery = "life-q";
    public const string SpecOrganId = "life-id";
    public const string SpecLifeForce = "life-lifeForce";
    public const string SpecBioRhythm = "life-bioRhythm";
    public const string SpecLabel = "life-label";
    public const string SpecDifficulty = "life-difficulty";
    public const string SpecRaw = "life-raw";

    public static readonly string[] AllKeys =
    {
        Op, Channel, Value, Delta, Duration, Query, OrganId, LifeForce, Label, Difficulty, Raw, BioRhythm
    };
}

public enum LifeSystemsLemmaOp
{
    None,
    Set,
    Adjust,
    Query,
    Buff,
    Illness,
    Organ
}

[Serializable]
public struct LifeSystemsLemmaProperties
{
    public LifeSystemsLemmaOp op;
    public string channel;
    public float value;
    public float delta;
    public float durationSeconds;
    public string query;
    public string organId;
    public float lifeForce;
    public float bioRhythm;
    public string label;
    public string difficulty;
    public bool useRaw;

    public static LifeSystemsLemmaProperties Defaults => new LifeSystemsLemmaProperties
    {
        op = LifeSystemsLemmaOp.None,
        durationSeconds = 0f,
        value = 0f,
        delta = 0f
    };
}
