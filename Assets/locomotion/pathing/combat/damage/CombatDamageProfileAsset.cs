using UnityEngine;

/// <summary>Authorable damage family profile for the Damage Type editor.</summary>
[CreateAssetMenu(fileName = "CombatDamageProfile", menuName = "Locomotion/Combat/Damage Profile", order = 210)]
public sealed class CombatDamageProfileAsset : ScriptableObject
{
    public CombatDamageType damageType = CombatDamageType.Bullet;
    public DamageHealthMode healthMode = DamageHealthMode.PerLimb;
    public CombatMaterialKind materialKind = CombatMaterialKind.Human;
    [Range(0f, 1f)] public float defaultAmount01 = 0.3f;
    [Range(0f, 1f)] public float defaultDepth01 = 0.4f;
    public bool throughOrStop;
    public string cutterProfileId;
    public string cutProfileId;
    public float cutInterval = 0.2f;
    public string smellSignature = "blood";
    public bool autoSuture;
    [TextArea] public string notes;

    public CombatDamageEvent ToEvent(GameObject attacker, GameObject target, Vector3 hit, Vector3 dir, string limbId = "Chest")
    {
        return new CombatDamageEvent
        {
            attacker = attacker,
            target = target,
            type = damageType,
            worldHit = hit,
            direction = dir,
            depth01 = defaultDepth01,
            amount01 = defaultAmount01,
            through = throughOrStop,
            cutterProfileId = cutterProfileId,
            cutProfileId = cutProfileId,
            materialKind = materialKind,
            healthMode = healthMode,
            limbId = limbId,
            smellSignature = smellSignature,
            createWound = true,
            autoSuture = autoSuture
        };
    }
}
