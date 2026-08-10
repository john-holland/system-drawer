using UnityEngine;

public enum PetInteractionKind
{
    Approach = 0,
    Pet = 1,
    Feed = 2,
    Play = 3,
    Scold = 4,
    Lead = 5
}

public enum PetJudgment
{
    Allow = 0,
    SoftRedirect = 1,
    Deny = 2,
    EscalateThreat = 3
}

/// <summary>Centralizes pet interaction judgments using Ragdoll.OpinionFor.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Civil/Pet Warden")]
public sealed class PetWarden : MonoBehaviour
{
    public ThreatWarden threatWarden;
    [Range(0f, 1f)] public float denyBelowOwnerCoef = 0.2f;
    [Range(0f, 1f)] public float escalateFear01 = 0.75f;

    void Awake()
    {
        if (threatWarden == null)
            threatWarden = GetComponent<ThreatWarden>() ?? FindFirstObjectByType<ThreatWarden>();
    }

    public PetJudgment Judge(RagdollSystem pet, UnityEngine.Object actor, PetInteractionKind kind)
    {
        if (pet == null) return PetJudgment.Deny;
        var opinion = pet.OpinionFor(actor);
        if (opinion.fear01 >= escalateFear01 && kind != PetInteractionKind.Approach)
        {
            NotifyThreat(pet, actor);
            return PetJudgment.EscalateThreat;
        }
        if (opinion.OwnerCoefficient < denyBelowOwnerCoef && kind == PetInteractionKind.Lead)
            return PetJudgment.Deny;
        if (opinion.dislike01 > 0.7f && kind == PetInteractionKind.Pet)
            return PetJudgment.SoftRedirect;
        return PetJudgment.Allow;
    }

    void NotifyThreat(RagdollSystem pet, UnityEngine.Object actor)
    {
        if (threatWarden != null)
            threatWarden.SendMessage("OnPetEscalate", actor, SendMessageOptions.DontRequireReceiver);
        pet.SendMessage("OnPetJudgmentEscalate", actor, SendMessageOptions.DontRequireReceiver);
    }
}
