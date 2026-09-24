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

    [Header("Violence threshold")]
    [Tooltip("Threat intensity required before physical response; below this civilians flee / de-escalate.")]
    [Range(0f, 1f)] public float violenceThreshold01 = 0.65f;
    [Tooltip("Developer in-painting / persona override added to threshold (negative = easier to snap).")]
    [Range(-0.5f, 0.5f)] public float statusAttributeBias01;
    [Tooltip("Temporary snap depression (Travis-style); lowers effective threshold.")]
    [Range(0f, 1f)] public float snapDepression01;
    [Tooltip("Default civilian graft: flee unless ShouldRespondPhysically.")]
    public bool defaultFleeUnlessTriggered = true;

    public JusticeCard()
    {
        isJusticeGoal = true;
        physicalPathingTag = "justice";
        traversabilityMode = TraversabilityMode.Custom;
        traversabilityTag = "justice";
    }

    public float EffectiveViolenceThreshold01()
    {
        return Mathf.Clamp01(violenceThreshold01 + statusAttributeBias01 - snapDepression01);
    }

    /// <summary>True when threat intensity meets/exceeds the (biased) violence threshold.</summary>
    public bool ShouldRespondPhysically(GameObject actor, float threatIntensity01)
    {
        float thr = EffectiveViolenceThreshold01();
        if (actor != null)
        {
            var sheet = actor.GetComponent<LifeSystemsSheet>();
            if (sheet != null)
            {
                // Adrenaline lowers threshold slightly (easier to respond).
                float adr = sheet.Get01(LifeSystemsChannelCatalog.Adrenaline);
                thr = Mathf.Clamp01(thr - adr * 0.15f);
            }
        }
        return threatIntensity01 >= thr;
    }

    /// <summary>Apply a temporary snap (lowers threshold); decays externally.</summary>
    public void ApplySnap(float amount01)
    {
        snapDepression01 = Mathf.Clamp01(snapDepression01 + Mathf.Max(0f, amount01));
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
            limits = new SectionLimits { maxForce = 80f, maxTorque = 20f, maxVelocityChange = 1.5f },
            violenceThreshold01 = 0.65f,
            defaultFleeUnlessTriggered = true
        };
    }
}
