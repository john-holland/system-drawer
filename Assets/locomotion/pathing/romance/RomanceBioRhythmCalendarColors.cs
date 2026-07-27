using UnityEngine;

/// <summary>
/// Kind-based calendar tint mapping for romance biorhythm overlay rows.
/// Blue = health, pink→red = love physicality, purple = political/society.
/// </summary>
public static class RomanceBioRhythmCalendarColors
{
    public enum Kind { Health, Love, Political }

    public static readonly Color HealthBlue = new Color(0.3f, 0.6f, 1f, 0.9f);
    public static readonly Color LovePink = new Color(0.95f, 0.45f, 0.65f, 0.9f);
    public static readonly Color LoveRed = new Color(0.9f, 0.2f, 0.25f, 0.9f);
    public static readonly Color PoliticalPurple = new Color(0.6f, 0.4f, 0.9f, 0.9f);

    public static Color ForKind(Kind kind, float physicality01 = 0.35f)
    {
        switch (kind)
        {
            case Kind.Love:
                return Color.Lerp(LovePink, LoveRed, Mathf.Clamp01(physicality01));
            case Kind.Political:
                return PoliticalPurple;
            default:
                return HealthBlue;
        }
    }

    public static Kind InferKindFromTags(System.Collections.Generic.IList<string> tags)
    {
        if (tags == null) return Kind.Health;
        for (int i = 0; i < tags.Count; i++)
        {
            var t = tags[i];
            if (string.IsNullOrEmpty(t)) continue;
            if (t.IndexOf("politic", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("society", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("liberal", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("conserv", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return Kind.Political;
            if (t.IndexOf("love", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("romance", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("intimacy", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("lovemaking", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return Kind.Love;
            if (t.IndexOf("health", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("biorhythm", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                t.IndexOf("clinical", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return Kind.Health;
        }
        return Kind.Health;
    }

    /// <summary>Love tint scaled by physicality × participant count (clamped).</summary>
    public static Color LoveTint(float physicality01, int participantCount)
    {
        float p = Mathf.Clamp01(physicality01) * Mathf.Clamp(participantCount, 1, 4) / 2f;
        return ForKind(Kind.Love, Mathf.Clamp01(p));
    }
}
