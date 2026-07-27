/// <summary>Canonical love-making animation / IK training group tags.</summary>
public static class LoveMakingAnimationGroup
{
    public const string Approach = "lovemaking.approach";
    public const string Embrace = "lovemaking.embrace";
    public const string Kiss = "lovemaking.kiss";
    public const string KissPeck = "lovemaking.kiss.peck";
    public const string KissSmooch = "lovemaking.kiss.smooch";
    public const string KissMakingOut = "lovemaking.kiss.making_out";
    public const string Hold = "lovemaking.hold";
    public const string Caress = "lovemaking.caress";
    public const string Nuzzle = "lovemaking.nuzzle";
    public const string DanceClose = "lovemaking.dance_close";
    public const string Part = "lovemaking.part";

    public const float DefaultKissIntensity = 0.35f;
    public const float PeckIntensity = 0.12f;
    public const float SmoochIntensity = 0.55f;
    public const float MakingOutIntensity = 0.85f;

    public static readonly string[] All =
    {
        Approach, Embrace, Kiss, KissPeck, KissSmooch, KissMakingOut, Hold, Caress, Nuzzle, DanceClose, Part
    };

    public static string ForMove(LoveMakingMoveKind kind, bool intimateStyle = false) =>
        ForMove(kind, intimateStyle, DefaultKissIntensity, null);

    public static string ForMove(
        LoveMakingMoveKind kind,
        bool intimateStyle,
        float kissAnimationIntensity,
        string kissAnimationKey)
    {
        if (kind == LoveMakingMoveKind.Kiss)
            return ForKiss(kissAnimationIntensity, kissAnimationKey, intimateStyle);

        string tag = kind switch
        {
            LoveMakingMoveKind.Approach => Approach,
            LoveMakingMoveKind.Embrace => Embrace,
            LoveMakingMoveKind.Hold => Hold,
            LoveMakingMoveKind.Caress => Caress,
            LoveMakingMoveKind.Nuzzle => Nuzzle,
            LoveMakingMoveKind.DanceClose => DanceClose,
            LoveMakingMoveKind.Part => Part,
            _ => Embrace
        };
        return intimateStyle ? tag + ".intimate" : tag;
    }

    /// <summary>
    /// Precedence: authored kissAnimationKey wins; else intensity band; intimate appends .intimate
    /// unless the authored key already encodes style.
    /// </summary>
    public static string ForKiss(float intensity01, string kissAnimationKey = null, bool intimateStyle = false)
    {
        if (!string.IsNullOrWhiteSpace(kissAnimationKey))
        {
            string key = kissAnimationKey.Trim();
            if (intimateStyle && !key.EndsWith(".intimate", System.StringComparison.OrdinalIgnoreCase))
                return key + ".intimate";
            return key;
        }

        float i = UnityEngine.Mathf.Clamp01(intensity01);
        string band =
            i < 0.2f ? KissPeck :
            i < 0.45f ? Kiss :
            i < 0.7f ? KissSmooch :
            KissMakingOut;
        return intimateStyle ? band + ".intimate" : band;
    }

    public static float DefaultIntensityForLemma(string lemma)
    {
        if (string.IsNullOrEmpty(lemma)) return DefaultKissIntensity;
        string l = lemma.Trim().ToLowerInvariant().Replace('_', '-').Replace(' ', '-');
        if (l.Contains("peck") || l.Contains("cheek"))
            return PeckIntensity;
        if (l.Contains("making-out") || l.Contains("make-out") || l == "makingout")
            return MakingOutIntensity;
        if (l.Contains("smooch"))
            return SmoochIntensity;
        return DefaultKissIntensity;
    }
}
