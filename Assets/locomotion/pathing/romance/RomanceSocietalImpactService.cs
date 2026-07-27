using UnityEngine;

/// <summary>
/// Big-G love impact: default SocietalImpact = 1/population; developer override (e.g. 1).
/// </summary>
[AddComponentMenu("Locomotion/Romance/Societal Impact Service")]
public sealed class RomanceSocietalImpactService : MonoBehaviour
{
    public const string ServiceKey = "romance.societal";

    [Tooltip("Population estimate when no society snapshot is available.")]
    public float populationEstimate = 10000f;

    [Tooltip("When set (≥0), used instead of 1/population. Set to 1 for full big-G impact.")]
    public bool useSocietalImpactOverride;
    [Range(0f, 1f)] public float societalImpactOverride = 1f;

    [Tooltip("Political channel nudged by love events (purple calendar).")]
    public string societyChannelId = LifeSystemsChannelCatalog.Morale;

    static RomanceSocietalImpactService _instance;
    public static RomanceSocietalImpactService Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindFirstObjectByType<RomanceSocietalImpactService>();
            return _instance;
        }
    }

    void Awake() => _instance = this;

    public float ResolveImpact()
    {
        if (useSocietalImpactOverride)
            return Mathf.Clamp01(societalImpactOverride);
        float pop = Mathf.Max(1f, populationEstimate);
        return 1f / pop;
    }

    public static void ApplyLoveEvent(GameObject a, GameObject b, LoveCard card, LoveMakingSession session)
    {
        var svc = Instance;
        if (svc == null)
        {
            // Transient apply without a scene service: use default 1/10000.
            float impact = 1f / 10000f;
            NudgeActor(a, impact, card);
            NudgeActor(b, impact * 0.5f, card);
            return;
        }

        float mag = svc.ResolveImpact();
        float phys = card != null ? card.physicality01 : 0.35f;
        float people = session != null ? Mathf.Max(1, session.ParticipantCount) : 2;
        float scaled = mag * (0.5f + 0.5f * phys) * Mathf.Clamp01(people / 4f);

        NudgeActor(a, scaled, card);
        NudgeActor(b, scaled * 0.5f, card);

        // Broadcast a tiny political/morale bump for purple biorhythm bars.
        var life = LifeSystemsServices.Instance;
        if (life != null && a != null)
        {
            var sheet = life.GetOrCreate(a);
            sheet.Adjust01(svc.societyChannelId, scaled);
            if (!string.IsNullOrEmpty(LifeSystemsChannelCatalog.Liberalism))
                sheet.Adjust01(LifeSystemsChannelCatalog.Liberalism, scaled * 0.25f);
        }
    }

    static void NudgeActor(GameObject actor, float impact, LoveCard card)
    {
        if (actor == null || impact <= 0f) return;
        var life = LifeSystemsServices.Instance;
        var sheet = life != null ? life.GetOrCreate(actor) : actor.GetComponent<LifeSystemsSheet>();
        if (sheet == null) return;
        sheet.EnsureDefaults();
        sheet.Adjust01(LifeSystemsChannelCatalog.Morale, impact);
        sheet.Adjust01(LifeSystemsChannelCatalog.Affection, impact * 0.5f);
    }
}
