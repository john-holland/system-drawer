using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scans for partner ragdolls and emits LoveCards into PhysicsCardSolver.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Locomotion/Love Making/Consider Love Making Cards")]
public sealed class ConsiderLoveMakingCards : MonoBehaviour
{
    public PhysicsCardSolver cardSolver;
    public RagdollSystem actorRagdoll;
    public LoveMakingMode mode = LoveMakingMode.Tender;
    public bool intimateStyle;
    public float scanRangeM = 2.5f;
    public LayerMask partnerMask = ~0;
    readonly List<GoodSection> _generated = new List<GoodSection>();

    void Awake()
    {
        if (cardSolver == null)
            cardSolver = GetComponent<PhysicsCardSolver>();
        if (actorRagdoll == null)
            actorRagdoll = GetComponent<RagdollSystem>();
    }

    public List<GoodSection> GenerateCards(GameObject forcedPartner = null)
    {
        _generated.Clear();
        RagdollState state = actorRagdoll != null ? actorRagdoll.GetCurrentState() : null;

        if (forcedPartner != null)
        {
            EmitForPartner(forcedPartner, state);
        }
        else
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, scanRangeM, partnerMask, QueryTriggerInteraction.Ignore);
            var seen = new HashSet<GameObject>();
            for (int i = 0; i < hits.Length; i++)
            {
                var c = hits[i];
                if (c == null) continue;
                var root = c.attachedRigidbody != null ? c.attachedRigidbody.gameObject : c.gameObject;
                var rd = root.GetComponentInParent<RagdollSystem>();
                if (rd == null || rd == actorRagdoll) continue;
                GameObject partner = rd.gameObject;
                if (partner == gameObject || !seen.Add(partner)) continue;
                EmitForPartner(partner, state);
            }
        }

        if (cardSolver != null)
            cardSolver.AddCards(_generated);
        return _generated;
    }

    void EmitForPartner(GameObject partner, RagdollState state)
    {
        LoveMakingMoveKind[] kinds = PreferredMovesForMode(mode);
        for (int i = 0; i < kinds.Length; i++)
        {
            var card = LoveCard.Generate(mode, kinds[i], partner, state, intimateStyle);
            if (!card.MeetsLoveRequirements(gameObject, partner, actorRagdoll))
                continue;
            _generated.Add(card);
        }
    }

    public static LoveMakingMoveKind[] PreferredMovesForMode(LoveMakingMode mode)
    {
        switch (mode)
        {
            case LoveMakingMode.Passionate:
                return new[]
                {
                    LoveMakingMoveKind.Approach, LoveMakingMoveKind.Embrace, LoveMakingMoveKind.Kiss,
                    LoveMakingMoveKind.Caress, LoveMakingMoveKind.Hold
                };
            case LoveMakingMode.Playful:
                return new[]
                {
                    LoveMakingMoveKind.Approach, LoveMakingMoveKind.DanceClose, LoveMakingMoveKind.Nuzzle,
                    LoveMakingMoveKind.Hold, LoveMakingMoveKind.Part
                };
            default:
                return new[]
                {
                    LoveMakingMoveKind.Approach, LoveMakingMoveKind.Embrace, LoveMakingMoveKind.Hold,
                    LoveMakingMoveKind.Kiss, LoveMakingMoveKind.Nuzzle
                };
        }
    }

    public static LoveCard MakeDefaultCard()
    {
        return LoveCard.Generate(LoveMakingMode.Tender, LoveMakingMoveKind.Embrace, null, null);
    }
}
