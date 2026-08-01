using UnityEngine;

/// <summary>Corrective action card (e.g. shut off heat if not burned too badly).</summary>
[System.Serializable]
public class JusticeCard : GoodSection
{
    [Header("Justice")]
    public JusticeAction justiceAction = JusticeAction.ShutOffHeat;
    public GameObject hazardTarget;
    [Range(0f, 1f)] public float maxBurnInjury01 = 0.55f;
    public bool requireNotBurnedTooBadly = true;

    public JusticeCard()
    {
        isJusticeGoal = true;
        physicalPathingTag = "justice";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "justice";
    }

    public bool MeetsJusticeRequirements(GameObject actor, GameObject target = null)
    {
        GameObject t = target != null ? target : hazardTarget;
        if (actor == null) return false;
        if (justiceAction == JusticeAction.ShutOffHeat && t == null)
            return false;
        if (requireNotBurnedTooBadly && actor != null)
        {
            var limbs = actor.GetComponent<LimbIntegrityState>();
            if (limbs != null)
            {
                // If limb integrity exposes overall damage, gate; otherwise allow
                // Soft gate via LifeSystems fatigue/adrenaline as burn proxy
                var sheet = actor.GetComponent<LifeSystemsSheet>();
                if (sheet != null)
                {
                    float painProxy = sheet.Get01(LifeSystemsChannelCatalog.Fatigue);
                    if (painProxy > maxBurnInjury01)
                        return false;
                }
            }
        }
        return true;
    }

    public static JusticeCard Generate(JusticeAction action, GameObject hazard, RagdollState state = null)
    {
        return new JusticeCard
        {
            justiceAction = action,
            hazardTarget = hazard,
            sectionName = $"justice_{action}",
            description = action.ToString(),
            isJusticeGoal = true,
            physicalPathingTag = $"justice_{action.ToString().ToLowerInvariant()}",
            requiredState = state?.CopyState(),
            targetState = state?.CopyState(),
            limits = new SectionLimits { maxForce = 80f, maxTorque = 20f, maxVelocityChange = 1.5f }
        };
    }
}
