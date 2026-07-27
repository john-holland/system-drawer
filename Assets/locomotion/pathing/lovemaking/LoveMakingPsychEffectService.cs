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
        if (a != null && b != null)
        {
            var pa = a.GetComponent<RomanceProfile>();
            var pb = b.GetComponent<RomanceProfile>();
            if (pa != null && pa.direction == RomanceDirection.Unrequited)
                requited = false;
            if (pb != null && pb.direction == RomanceDirection.Unrequited)
                requited = false;
        }

        ApplyTo(a, intensity, phys, requited, giveFull: true);
        ApplyTo(b, intensity, phys, requited, giveFull: requited);
    }

    static void ApplyTo(GameObject actor, float intensity, float phys, bool requited, bool giveFull)
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
    }
}
