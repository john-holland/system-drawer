using UnityEngine;

/// <summary>
/// Power-diamond helpers for relationship travel: Affection, Consent, Doctrine, Safety.
/// Missing optional wardens use 0.5 (neutral).
/// </summary>
public static class RelationshipPowerDiamond
{
    public const float NeutralMissing = 0.5f;
    public static readonly string[] Axes = { "Affection", "Consent", "Doctrine", "Safety" };

    public static float[] GreenExpected01(
        RelationshipStep step,
        LoveWarden love,
        RomanceWarden romance)
    {
        if (step != null && step.expected01 != null && step.expected01.Length >= 4)
            return CivilianPaperDoll.Pad4(step.expected01, NeutralMissing);
        float aff = Affection01(love, romance);
        return new[] { aff, NeutralMissing, NeutralMissing, NeutralMissing };
    }

    public static float[] RedLimit01(
        RelationshipStep step,
        ConsentWarden consent,
        TheocraticWarden theo,
        JusticeWarden justice,
        RightsWarden rights,
        ThreatWarden threat)
    {
        float[] fire = step != null ? step.FireLimit01() : new[] { 0.9f, 0.9f, 0.9f, 0.85f };
        float aff = fire[0];
        float con = consent != null ? Mathf.Min(fire[1], Mathf.Max(0.15f, consent.maxPhysicality01)) : fire[1];
        float doc = theo != null ? Mathf.Min(fire[2], Mathf.Max(0.15f, theo.Allow01())) : fire[2];
        float saf = Safety01(threat, justice, rights);
        if (threat != null || justice != null)
            saf = Mathf.Min(fire[3], Mathf.Max(0.15f, saf));
        else
            saf = fire[3];
        return new[] { aff, con, doc, saf };
    }

    public static float[] WhiteActual01(
        RelationshipStep step,
        LoveWarden love,
        RomanceWarden romance,
        ConsentWarden consent,
        TheocraticWarden theo,
        JusticeWarden justice,
        RightsWarden rights,
        ThreatWarden threat,
        RelationshipBioRhythm bio)
    {
        float aff = Affection01(love, romance);
        if (bio != null)
            aff = Mathf.Clamp01(aff * 0.7f + bio.affection01 * 0.3f);
        float con = consent != null ? consent.lastScore01 : NeutralMissing;
        float doc = theo != null ? theo.Allow01() : NeutralMissing;
        float saf = Safety01(threat, justice, rights);
        if (step != null && step.hasInpaint)
        {
            float[] exp = step.Expected01();
            aff = Mathf.Clamp01(aff * 0.5f + exp[0] * 0.5f);
            con = Mathf.Clamp01(con * 0.5f + exp[1] * 0.5f);
            doc = Mathf.Clamp01(doc * 0.5f + exp[2] * 0.5f);
            saf = Mathf.Clamp01(saf * 0.5f + exp[3] * 0.5f);
        }
        return new[] { aff, con, doc, saf };
    }

    public static float Affection01(LoveWarden love, RomanceWarden romance)
    {
        if (love == null && romance == null) return NeutralMissing;
        if (love != null && romance != null)
            return Mathf.Clamp01(0.5f * love.Allow01() + 0.5f * romance.Allow01());
        return love != null ? love.Allow01() : romance.Allow01();
    }

    public static float Safety01(ThreatWarden threat, JusticeWarden justice, RightsWarden rights)
    {
        bool any = threat != null || justice != null || rights != null;
        if (!any) return NeutralMissing;
        float s = 0f;
        int n = 0;
        if (threat != null)
        {
            s += 1f - threat.MaxThreat01();
            n++;
        }
        if (justice != null)
        {
            s += justice.Allow01();
            n++;
        }
        else if (rights != null)
        {
            s += rights.Allow01();
            n++;
        }
        return n > 0 ? Mathf.Clamp01(s / n) : NeutralMissing;
    }
}
