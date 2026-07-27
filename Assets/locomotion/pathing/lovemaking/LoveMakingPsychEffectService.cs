using UnityEngine;

/// <summary>Applies romance channel deltas to participants on love-making commit.</summary>
public static class LoveMakingPsychEffectService
{
    public static void Apply(LoveMakingSession session, GameObject a, GameObject b, LoveCard card)
    {
        if (a == null && b == null) return;
        float intensity = card != null ? card.desireIntensity01 : 0.4f;
        float phys = card != null ? card.physicality01 : 0.35f;
        bool requited = true;
        bool poorResponse = card != null && card.kissResponseNegative;
        if (a != null && b != null)
        {
            var pa = a.GetComponent<RomanceProfile>();
            var pb = b.GetComponent<RomanceProfile>();
            if (pa != null && pa.direction == RomanceDirection.Unrequited)
                requited = false;
            if (pb != null && pb.direction == RomanceDirection.Unrequited)
                requited = false;
            if (!poorResponse && pb != null)
                poorResponse = DetectPoorKissResponse(pb);
        }

        bool isKiss = card != null && card.loveMoveKind == LoveMakingMoveKind.Kiss;
        float kissK = isKiss && card != null ? Mathf.Clamp01(card.kissAnimationIntensity) : 0f;

        ApplyTo(a, intensity, phys, requited, giveFull: true, isKiss, kissK, poorResponse);
        ApplyTo(b, intensity, phys, requited, giveFull: requited, isKiss, kissK, poorResponse && !requited);
    }

    static bool DetectPoorKissResponse(RomanceProfile partner)
    {
        if (partner == null) return false;
        if (partner.harshRejectionResponse) return true;
        return partner.direction == RomanceDirection.Unrequited &&
               (partner.severity == RomanceSeverity.FriendZone ||
                partner.severity >= RomanceSeverity.OnTheRocks);
    }

    static void ApplyTo(
        GameObject actor,
        float intensity,
        float phys,
        bool requited,
        bool giveFull,
        bool isKiss,
        float kissIntensityK,
        bool poorKissResponse)
    {
        if (actor == null) return;
        var life = LifeSystemsServices.Instance;
        var sheet = life != null ? life.GetOrCreate(actor) : actor.GetComponent<LifeSystemsSheet>();
        if (sheet == null)
            sheet = actor.AddComponent<LifeSystemsSheet>();
        sheet.EnsureDefaults();

        float k = giveFull ? 1f : 0.35f;
        sheet.Adjust01(LifeSystemsChannelCatalog.Affection, 0.08f * intensity * k);
        sheet.Adjust01(LifeSystemsChannelCatalog.Intimacy, 0.1f * phys * k);
        sheet.Adjust01(LifeSystemsChannelCatalog.Trust, (requited ? 0.06f : -0.03f) * intensity * k);
        sheet.Adjust01(LifeSystemsChannelCatalog.Attachment, 0.05f * intensity * k);
        sheet.Adjust01(LifeSystemsChannelCatalog.Arousal, 0.12f * phys * k);
        if (!requited)
            sheet.Adjust01(LifeSystemsChannelCatalog.Jealousy, 0.04f * intensity);
        sheet.bioRhythm?.ApplyAmplitudeDelta(0.03f * intensity);

        if (isKiss)
        {
            float oxK = requited ? k : k * 0.35f;
            sheet.Adjust01(LifeSystemsChannelCatalog.Serotonin, 0.10f * kissIntensityK * k);
            sheet.Adjust01(LifeSystemsChannelCatalog.Oxytocin, 0.08f * kissIntensityK * oxK);
            sheet.Adjust01(LifeSystemsChannelCatalog.Affection, 0.03f * kissIntensityK * k);
            sheet.Adjust01(LifeSystemsChannelCatalog.Morale, 0.04f * kissIntensityK * k);
            if (kissIntensityK >= 0.7f)
                sheet.Adjust01(LifeSystemsChannelCatalog.Arousal, 0.05f * kissIntensityK * k);

            if (!requited && poorKissResponse)
            {
                // Visceral flinch: blood pressure drop + reflux / acidity spike
                sheet.AdjustClinical(LifeSystemsChannelCatalog.BloodPressureSys, -28f);
                sheet.AdjustClinical(LifeSystemsChannelCatalog.BloodPressureDia, -14f);
                sheet.Adjust01(LifeSystemsChannelCatalog.Acidity, 0.22f * Mathf.Max(0.35f, kissIntensityK));
                sheet.Adjust01(LifeSystemsChannelCatalog.Reflux, 0.28f * Mathf.Max(0.35f, kissIntensityK));
                sheet.Adjust01(LifeSystemsChannelCatalog.Morale, -0.08f);
            }
        }
    }
}
